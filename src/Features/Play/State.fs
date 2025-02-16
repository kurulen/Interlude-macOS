﻿namespace Interlude.Features.Play

open System
open Prelude
open Prelude.Gameplay

[<RequireQualifiedAccess>]
type PacemakerInfo =
    | None
    | Accuracy of float
    | Replay of IScoreMetric
    | Judgement of target: JudgementId * max_count: int

type PlayState =
    {
        Ruleset: Ruleset
        mutable Scoring: IScoreMetric
        ScoringChanged: Event<unit>
        CurrentChartTime: unit -> ChartTime
        Pacemaker: PacemakerInfo
    }
    static member Dummy(chart) =
        let s = Metrics.createDummyMetric chart
        {
            Ruleset = s.Ruleset
            Scoring = s
            ScoringChanged = Event<unit>()
            CurrentChartTime = fun () -> 0.0f<ms>
            Pacemaker = PacemakerInfo.None
        }
    member this.SubscribeToHits(handler: HitEvent<HitEventGuts> -> unit) =
        let mutable obj : IDisposable = this.Scoring.OnHit.Subscribe handler
        this.ScoringChanged.Publish.Add(
            fun () ->
                obj.Dispose()
                obj <- this.Scoring.OnHit.Subscribe handler
        )
    member this.ChangeScoring(scoring) =
        this.Scoring <- scoring
        this.ScoringChanged.Trigger()