﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interlude.Gameplay.Charts.YAVSRG;
using Interlude.Gameplay.Watchers;

namespace Interlude.Gameplay
{
    public class ScoreInfoProvider
    {
        Score _score;
        Chart _chart;
        DifficultyRating.RatingReport _rating;
        string _mods;
        IScoreSystem _scoring;
        ScoreTracker.HitData[] _hitdata;
        float? _physical, _technical;

        public ScoreInfoProvider(Score Score, Chart Chart)
        {
            _score = Score;
            _chart = Chart;
            _hitdata = ScoreTracker.StringToHitData(_score.hitdata, _score.keycount);
        }

        public ChartHeader Data
        {
            get { return _chart.Data; }
        }

        public ScoreTracker.HitData[] HitData
        {
            get { return _hitdata; }
        }

        public string Mods
        {
            get { if (_mods == null) { _mods = Game.Gameplay.GetModString(Game.Gameplay.GetModifiedChart(_score.mods, _chart), _score.rate, _score.layout); } return _mods; }
        }

        public IScoreSystem ScoreSystem
        {
            get
            {
                if (_scoring == null) { _scoring = IScoreSystem.GetScoreSystem(Game.Options.Profile.ScoreSystem); _scoring.ProcessScore(HitData); }
                return _scoring;
            }
        }

        public string Accuracy
        {
            get { return ScoreSystem.FormatAcc(); }
        }

        public int BestCombo
        {
            get { return ScoreSystem.BestCombo; }
        }

        public DifficultyRating.RatingReport RatingData
        {
            get { if (_rating == null) { _rating = new DifficultyRating.RatingReport(Game.Gameplay.GetModifiedChart(_score.mods, _chart), _score.rate, _score.layout); } return _rating; }
        }

        public float PhysicalPerformance
        {
            get { if (_physical == null) { _physical = DifficultyRating.PlayerRating.GetRating(RatingData, HitData); } return (float)_physical; }
        }

        public float TechnicalPerformance
        {
            get { if (_technical == null) { _technical = 0f; } return (float)_technical; }
        }

        public DateTime Time
        {
            get { return _score.time; }
        }

        public string Player
        {
            get { return _score.player; }
        }
    }
}
