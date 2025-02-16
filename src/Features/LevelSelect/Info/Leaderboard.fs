﻿namespace Interlude.Features.LevelSelect

open System
open Percyqaz.Common
open Percyqaz.Flux.Input
open Percyqaz.Flux.UI
open Prelude.Common
open Prelude.Data.Scores
open Prelude.Data.Charts.Caching
open Prelude.Gameplay
open Prelude.Charts.Formats.Interlude
open Interlude
open Interlude.UI
open Interlude.Utils
open Interlude.Options
open Interlude.UI.Components
open Interlude.UI.Menu
open Interlude.Features.Gameplay
open Interlude.Features.Score
open Interlude.Features.Online
open Interlude.Web.Shared

module Leaderboard =

    [<RequireQualifiedAccess>]
    type State =
        | Offline = -1
        | Loading = 0
        | NoLeaderboard = 1
        | EmptyLeaderboard = 2
        | Loaded = 3

    [<RequireQualifiedAccess>]
    type Sort =
        | Time = 0
        | Performance = 1
        | Accuracy = 2
    
    [<RequireQualifiedAccess>]
    type Filter =
        | None = 0
        | CurrentRate = 1
        | CurrentMods = 2

    type LeaderboardScore = Requests.Charts.Scores.Leaderboard.Score

    type LeaderboardCard(score: LeaderboardScore, data: ScoreInfoProvider) as this =
        inherit Frame(NodeType.Button((fun () -> 
            Screen.changeNew 
                (fun () -> new ScoreScreen(data, ImprovementFlags.Default) :> Screen)
                Screen.Type.Score
                Transitions.Flags.Default)))

        let fade = Animation.Fade(0.0f, Target = 1.0f)
        let animation = Animation.seq [Animation.Delay 150; fade]

        do
            this.Fill <- fun () -> if this.Focused then Colors.yellow_accent.O1a fade.Alpha else (!*Palette.DARK).O2a fade.Alpha
            this.Border <- fun () -> if this.Focused then Colors.yellow_accent.O4a fade.Alpha else (!*Palette.LIGHT).O2a fade.Alpha
            ignore data.Physical
            ignore data.Lamp

            let text_color = fun () -> let a = fade.Alpha in (Colors.white.O4a a, Colors.shadow_1.O4a a)
            let text_subcolor = fun () -> let a = fade.Alpha in (Colors.grey_1.O4a a, Colors.shadow_2.O4a a)

            this
            |+ Text(
                K (sprintf "#%i %s  •  %s" score.Rank score.Username (data.Scoring.FormatAccuracy()))
                ,
                Color = text_color,
                Align = Alignment.LEFT,
                Position = { Left = 0.0f %+ 5.0f; Top = 0.0f %+ 0.0f; Right = 0.5f %+ 0.0f; Bottom = 0.6f %+ 0.0f })

            |+ Text(
                K (sprintf "%s  •  %ix  •  %.2f" (data.Ruleset.LampName data.Lamp) data.Scoring.State.BestCombo data.Physical)
                ,
                Color = text_subcolor,
                Align = Alignment.LEFT,
                Position = { Left = 0.0f %+ 5.0f; Top = 0.6f %- 5.0f; Right = 0.5f %+ 0.0f; Bottom = 1.0f %- 2.0f })

            |+ Text(
                K (formatTimeOffset(DateTime.UtcNow - data.ScoreInfo.time.ToUniversalTime())),
                Color = text_subcolor,
                Align = Alignment.RIGHT,
                Position = { Left = 0.5f %+ 0.0f; Top = 0.6f %- 5.0f; Right = 1.0f %- 5.0f; Bottom = 1.0f %- 2.0f })

            |+ Text(
                K data.Mods,
                Color = text_color,
                Align = Alignment.RIGHT,
                Position = { Left = 0.5f %+ 0.0f; Top = 0.0f %+ 0.0f; Right = 1.0f %- 5.0f; Bottom = 0.6f %+ 0.0f })

            |* Clickable(this.Select,
                OnRightClick = fun () -> ScoreContextMenu(data).Show())

        member this.Data = data

        override this.OnFocus() = Style.hover.Play(); base.OnFocus()

        override this.Update(elapsedTime, bounds) =
            base.Update(elapsedTime, bounds)
            animation.Update elapsedTime
            if Mouse.hover this.Bounds && (!|"delete").Tapped() then ScoreContextMenu.ConfirmDeleteScore(data, false)
            elif this.Focused && (!|"context_menu").Tapped() then ScoreContextMenu(data).Show()

    module Loader =

        type private Request =
            {
                Scores: LeaderboardScore array
                RulesetId: string
                Ruleset: Ruleset
                Hash: string
                Chart: Chart
            }
            override this.ToString() = "<leaderboard calculation>"

        let handle (container: FlowContainer.Vertical<LeaderboardCard>) =
            let worker =
                { new Async.SwitchServiceSeq<Request, LeaderboardScore * ScoreInfoProvider>() with
                    member this.Handle(req: Request) =
                        sync container.Clear
                        seq { 
                            for score in req.Scores do
                                let data = ScoreInfoProvider(
                                    ({ 
                                        time = score.Timestamp
                                        replay = score.Replay
                                        rate = score.Rate
                                        selectedMods = score.Mods
                                        layout = options.Playstyles.[req.Chart.Keys - 3]
                                        keycount = req.Chart.Keys
                                    } : Score), req.Chart, req.Ruleset, Player = Some score.Username)
                                yield score, data
                        }
                    member this.Callback((score: LeaderboardScore, data: ScoreInfoProvider)) =
                        let sc = LeaderboardCard(score, data)
                        sync(fun () -> container.Add sc)
                    member this.JobCompleted(req: Request) = ()
                }
            fun (state: Setting<State>) ->
                if Network.status <> Network.Status.LoggedIn then state.Set State.Offline else
                state.Set State.Loading
                let hash, ruleset_id = Chart.cacheInfo.Value.Hash, Content.Rulesets.current_hash
                API.Client.get<Requests.Charts.Scores.Leaderboard.Response>(sprintf "charts/scores?chart=%s&ruleset=%s" hash ruleset_id,
                    function
                    | Some reply ->
                        if (hash, ruleset_id) <> (Chart.cacheInfo.Value.Hash, Content.Rulesets.current_hash) then () else

                        worker.Request
                            {
                                Scores = reply.Scores
                                RulesetId = Content.Rulesets.current_hash
                                Ruleset = Content.Rulesets.current
                                Chart = Chart.current.Value
                                Hash = Chart.cacheInfo.Value.Hash
                            }
                        state.Set (if reply.Scores.Length > 0 then State.Loaded else State.EmptyLeaderboard)
                    | None -> 
                        // worker is requested anyway because it ensures any loading scores get swallowed and the scoreboard is cleared
                        worker.Request
                            {
                                Scores = [||]
                                RulesetId = Content.Rulesets.current_hash
                                Ruleset = Content.Rulesets.current
                                Chart = Chart.current.Value
                                Hash = Chart.cacheInfo.Value.Hash
                            }
                        state.Set State.NoLeaderboard
                )

open Leaderboard

type Leaderboard(display: Setting<Display>) as this =
    inherit StaticContainer(NodeType.None)

    let state = Setting.simple State.NoLeaderboard

    let mutable chart = ""
    let mutable scoring = ""

    let filter = Setting.simple Filter.None
    let sort = Setting.map enum int options.ScoreSortMode

    let flowContainer =  FlowContainer.Vertical(75.0f, Spacing = Style.PADDING * 3.0f)
    let scrollContainer = ScrollContainer.Flow(flowContainer, Margin = Style.PADDING, Position = Position.TrimTop(55.0f).TrimBottom(50.0f))

    let loader = Loader.handle flowContainer
    let load_leaderboard_async = fun () -> loader state

    do
        this
        |+ StylishButton(
            (fun () -> display.Set Display.Details),
            K <| Localisation.localise "levelselect.info.leaderboard.name",
            !%Palette.MAIN_100,
            Hotkey = "scoreboard_storage",
            TiltLeft = false,
            Position = { Left = 0.0f %+ 0.0f; Top = 0.0f %+ 0.0f; Right = 0.33f %- 25.0f; Bottom = 0.0f %+ 50.0f })
            .Tooltip(Tooltip.Info("levelselect.info.mode", "scoreboard_storage"))
        |+ StylishButton.Selector(
            Icons.sort,
            [|
                Sort.Accuracy, L"levelselect.info.scoreboard.sort.accuracy"
                Sort.Performance, L"levelselect.info.scoreboard.sort.performance"
                Sort.Time, L"levelselect.info.scoreboard.sort.time"
            |],
            sort,
            !%Palette.DARK_100,
            Hotkey = "scoreboard_sort",
            Position = { Left = 0.33f %+ 0.0f; Top = 0.0f %+ 0.0f; Right = 0.66f %- 25.0f; Bottom = 0.0f %+ 50.0f })
            .Tooltip(Tooltip.Info("levelselect.info.scoreboard.sort", "scoreboard_sort"))
        |+ StylishButton.Selector(
            Icons.filter,
            [|
                Filter.None, L"levelselect.info.scoreboard.filter.none"
                Filter.CurrentRate, L"levelselect.info.scoreboard.filter.currentrate"
                Filter.CurrentMods, L"levelselect.info.scoreboard.filter.currentmods"
            |],
            filter,
            !%Palette.MAIN_100,
            Hotkey = "scoreboard_filter",
            TiltRight = false,
            Position = { Left = 0.66f %+ 0.0f; Top = 0.0f %+ 0.0f; Right = 1.0f %- 0.0f; Bottom = 0.0f %+ 50.0f })
            .Tooltip(Tooltip.Info("levelselect.info.scoreboard.filter", "scoreboard_filter"))
        |+ scrollContainer
        |+ HotkeyAction("scoreboard", fun () -> if flowContainer.Focused then Selection.clear() else flowContainer.Focus())
        |+ Conditional((fun () -> state.Value = State.EmptyLeaderboard), EmptyState(Icons.leaderboard, L"levelselect.info.leaderboard.empty", Subtitle = L"levelselect.info.leaderboard.empty.subtitle"))
        |+ Conditional((fun () -> state.Value = State.NoLeaderboard), EmptyState(Icons.no_leaderboard, L"levelselect.info.leaderboard.unavailable"))
        |* Conditional((fun () -> state.Value = State.Offline), EmptyState(Icons.connected, L"misc.offline"))

    member this.Refresh() =
        let h = match Chart.cacheInfo with Some c -> c.Hash | None -> ""
        if h <> chart || scoring <> Content.Rulesets.current_hash then
            chart <- h
            scoring <- Content.Rulesets.current_hash
            load_leaderboard_async()