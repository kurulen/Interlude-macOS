﻿using System;
using Prelude.Utilities;

namespace Prelude.Gameplay.Watchers.Scoring
{
    public class StandardScoringPlus : DancePoints
    {
        float CurveEnd = 180f;
        float scale = 1f;

        public StandardScoringPlus(DataGroup Settings) : base(Settings)
        {
            int judge = Settings.GetValue("Judge", 4);
            float m = (10 - judge) / 6f;
            scale *= m;
            CurveEnd *= m;
            Name = Name.Replace("DP", "SC+");
        }

        public override float GetPointsForNote(float Delta)
        {
            if (Delta >= CurveEnd) { return 0; };
            return (float)Math.Max(-1, (1 - Math.Pow(Delta / scale, 2.8) * 0.0000056d) * 2f);
        }
    }
}
