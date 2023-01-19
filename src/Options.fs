﻿namespace Interlude

open System.IO
open System.Collections.Generic
open OpenTK.Windowing.GraphicsLibraryFramework
open Percyqaz.Common
open Percyqaz.Json
open Percyqaz.Flux.Input
open Percyqaz.Flux.Input.Bind
open Prelude.Common
open Prelude.Gameplay.Layout
open Prelude.Data.Charts.Library
open Prelude.Data.Charts.Library.Imports
open Interlude

module Options =

    (*
        User settings
    *)

    [<Json.AutoCodec>]
    [<RequireQualifiedAccess>]
    type ActiveCollection =
        | Collection of string
        | Level of string
        | None
        override this.ToString() =
            match this with
            | Collection c -> c
            | Level l -> l
            | None -> "" // todo: localised placeholder for nothing

    type Keymode =
        | ``3K`` = 3
        | ``4K`` = 4
        | ``5K`` = 5
        | ``6K`` = 6
        | ``7K`` = 7
        | ``8K`` = 8
        | ``9K`` = 9
        | ``10K`` = 10

    [<Json.AutoCodec>]
    [<RequireQualifiedAccess>]
    type Pacemaker =
        | Accuracy of float
        | Lamp of int
        static member Default = Accuracy 0.95

    type FailType =
        | Instant = 0
        | EndOfSong = 1

    [<Json.AutoCodec>]
    type LaneCoverOptions =
        {
            Enabled: Setting<bool>
            Sudden: Setting.Bounded<float>
            Hidden: Setting.Bounded<float>
            FadeLength: Setting.Bounded<int>
            Color: Setting<Color>
        }

    [<Json.AutoCodec(false)>]
    type GameOptions =
        {
            VisualOffset: Setting.Bounded<float>
            AudioOffset: Setting.Bounded<float>
            AudioVolume: Setting.Bounded<float>
            CurrentChart: Setting<string>
            Theme: Setting<string>

            ScrollSpeed: Setting.Bounded<float>
            HitPosition: Setting.Bounded<int>
            HitLighting: Setting<bool>
            Upscroll: Setting<bool>
            BackgroundDim: Setting.Bounded<float>
            PerspectiveTilt: Setting.Bounded<float>
            LaneCover: LaneCoverOptions
            KeymodePreference: Setting<Keymode>
            UseKeymodePreference: Setting<bool>
            Noteskin: Setting<string>

            Playstyles: Layout array
            SelectedRuleset: Setting<string>
            FavouriteRulesets: Setting<string list>
            FailCondition: Setting<FailType>
            Pacemakers: Dictionary<string, Pacemaker>
            ScaveScoreIfUnderPace: Setting<bool>

            OsuMount: Setting<MountedChartSource option>
            StepmaniaMount: Setting<MountedChartSource option>
            EtternaMount: Setting<MountedChartSource option>

            ChartSortMode: Setting<string>
            ChartSortReverse: Setting<bool>
            ChartGroupMode: Setting<string>
            LibraryMode: Setting<LibraryMode>
            ChartGroupReverse: Setting<bool>
            ScoreSortMode: Setting<int>

            Collection: Setting<ActiveCollection>
            Table: Setting<string option>
            GameplayBinds: (Bind array) array

            EnableConsole: Setting<bool>
            EnableTableEdit: Setting<bool>
            Hotkeys: Dictionary<Hotkey, Bind>
        }
        static member Default = {
            VisualOffset = Setting.bounded 0.0 -500.0 500.0 |> Setting.round 0
            AudioOffset = Setting.bounded 0.0 -500.0 500.0 |> Setting.round 0
            AudioVolume = Setting.percent 0.05
            CurrentChart = Setting.simple ""
            Theme = Setting.simple "*default"

            ScrollSpeed = Setting.bounded 2.05 1.0 5.0 |> Setting.round 2
            HitPosition = Setting.bounded 0 -300 600
            HitLighting = Setting.simple false
            Upscroll = Setting.simple false
            BackgroundDim = Setting.percent 0.5
            PerspectiveTilt = Setting.bounded 0.0 -1.0 1.0 |> Setting.round 2
            LaneCover = 
                { 
                    Enabled = Setting.simple false
                    Sudden = Setting.percent 0.0
                    Hidden = Setting.percent 0.45
                    FadeLength = Setting.bounded 200 0 500
                    Color = Setting.simple Color.Black
                }
            Noteskin = Setting.simple "*defaultBar.isk"
            KeymodePreference = Setting.simple Keymode.``4K``
            UseKeymodePreference = Setting.simple false

            Playstyles = [|Layout.OneHand; Layout.Spread; Layout.LeftOne; Layout.Spread; Layout.LeftOne; Layout.Spread; Layout.LeftOne; Layout.Spread|]
            SelectedRuleset = 
                Setting.simple Content.Rulesets.DEFAULT
                |> Setting.trigger (fun t -> Content.Rulesets.switch t false)
            FavouriteRulesets = Setting.simple [Content.Rulesets.DEFAULT]
            FailCondition = Setting.simple FailType.EndOfSong
            Pacemakers = Dictionary<string, Pacemaker>()
            ScaveScoreIfUnderPace = Setting.simple true

            OsuMount = Setting.simple None
            StepmaniaMount = Setting.simple None
            EtternaMount = Setting.simple None

            ChartSortMode = Setting.simple "Title"
            ChartSortReverse = Setting.simple false
            ChartGroupMode = Setting.simple "Pack"
            LibraryMode = Setting.simple LibraryMode.All
            ChartGroupReverse = Setting.simple false
            ScoreSortMode = Setting.simple 0

            Collection = Setting.simple ActiveCollection.None
            Table = Setting.simple None

            EnableConsole = Setting.simple false
            EnableTableEdit = Setting.simple false
            Hotkeys = Dictionary<Hotkey, Bind>()
            GameplayBinds = [|
                [|mk Keys.Left; mk Keys.Down; mk Keys.Right|]
                [|mk Keys.Z; mk Keys.X; mk Keys.Period; mk Keys.Slash|]
                [|mk Keys.Z; mk Keys.X; mk Keys.Space; mk Keys.Period; mk Keys.Slash|]
                [|mk Keys.Z; mk Keys.X; mk Keys.C; mk Keys.Comma; mk Keys.Period; mk Keys.Slash|]
                [|mk Keys.Z; mk Keys.X; mk Keys.C; mk Keys.Space; mk Keys.Comma; mk Keys.Period; mk Keys.Slash|]
                [|mk Keys.Z; mk Keys.X; mk Keys.C; mk Keys.V; mk Keys.Comma; mk Keys.Period; mk Keys.Slash; mk Keys.RightShift|]
                [|mk Keys.Z; mk Keys.X; mk Keys.C; mk Keys.V; mk Keys.Space; mk Keys.Comma; mk Keys.Period; mk Keys.Slash; mk Keys.RightShift|]
                [|mk Keys.CapsLock; mk Keys.Q; mk Keys.W; mk Keys.E; mk Keys.V; mk Keys.Space; mk Keys.K; mk Keys.L; mk Keys.Semicolon; mk Keys.Apostrophe|]
            |]
        }

    let mutable internal config : Percyqaz.Flux.Windowing.Config = Unchecked.defaultof<_>

    do 
        // Register decoding rules for Percyqaz.Flux config
        JSON.WithAutoCodec<Percyqaz.Flux.Windowing.Config>(false)
            .WithAutoCodec<Percyqaz.Flux.Windowing.WindowResolution>()
            .WithAutoCodec<Percyqaz.Flux.Input.Bind>() |> ignore

    let mutable options : GameOptions = Unchecked.defaultof<_>

    module Hotkeys =

        let init(d: Dictionary<Hotkey, Bind>) =
            Hotkeys.register "search" (mk Keys.Tab)
            Hotkeys.register "toolbar" (ctrl Keys.T)
            Hotkeys.register "tooltip" (mk Keys.Slash)
            Hotkeys.register "delete" (mk Keys.Delete)
            Hotkeys.register "screenshot" (mk Keys.F12)
            Hotkeys.register "volume" (mk Keys.LeftAlt)
            Hotkeys.register "previous" (mk Keys.Left)
            Hotkeys.register "next" (mk Keys.Right)
            Hotkeys.register "previous_group" (mk Keys.PageUp)
            Hotkeys.register "next_group" (mk Keys.PageDown)
            Hotkeys.register "start" (mk Keys.Home)
            Hotkeys.register "end" (mk Keys.End)
            
            Hotkeys.register "import" (ctrl Keys.I)
            Hotkeys.register "options" (ctrl Keys.O)
            Hotkeys.register "wiki" (ctrl Keys.H)
            Hotkeys.register "console" (mk Keys.GraveAccent)
            
            Hotkeys.register "library_mode" (mk Keys.D1)
            Hotkeys.register "add_to_collection" (mk Keys.RightBracket)
            Hotkeys.register "remove_from_collection" (mk Keys.LeftBracket)
            Hotkeys.register "move_down_in_collection" (ctrl Keys.RightBracket)
            Hotkeys.register "move_up_in_collection" (ctrl Keys.LeftBracket)
            Hotkeys.register "sort_mode" (mk Keys.D2)
            Hotkeys.register "reverse_sort_mode" (shift Keys.D2)
            Hotkeys.register "group_mode" (mk Keys.D3)
            Hotkeys.register "reverse_group_mode" (shift Keys.D3)
            Hotkeys.register "comment" (mk Keys.F)
            Hotkeys.register "context_menu" (mk Keys.Period)

            Hotkeys.register "uprate" (mk Keys.Equal)
            Hotkeys.register "downrate" (mk Keys.Minus)
            Hotkeys.register "uprate_half" (ctrl Keys.Equal)
            Hotkeys.register "downrate_half" (ctrl Keys.Minus)
            Hotkeys.register "uprate_small" (shift Keys.Equal)
            Hotkeys.register "downrate_small" (shift Keys.Minus)

            Hotkeys.register "scoreboard_storage" (mk Keys.Q)
            Hotkeys.register "scoreboard_sort" (mk Keys.W)
            Hotkeys.register "scoreboard_filter" (mk Keys.E)

            Hotkeys.register "scoreboard" (mk Keys.Z)
            Hotkeys.register "table" (mk Keys.X)
            Hotkeys.register "collections" (mk Keys.C)

            Hotkeys.register "preview" (mk Keys.A)
            Hotkeys.register "mods" (mk Keys.S)
            Hotkeys.register "ruleset" (mk Keys.D)
            Hotkeys.register "random_chart" (mk Keys.R)
            Hotkeys.register "autoplay" (ctrl Keys.A)
            Hotkeys.register "reload_themes" (Key (Keys.S, (true, true, true)))

            Hotkeys.register "skip" (mk Keys.Space)
            Hotkeys.register "retry" (ctrl Keys.R)

            options <- { options with Hotkeys = Hotkeys.import d }

    let private configPath = Path.GetFullPath "config.json"
    let firstLaunch = not (File.Exists configPath)

    let load() =
        config <- loadImportantJsonFile "Config" configPath true
        Localisation.loadFile config.Locale
        if config.WorkingDirectory <> "" then Directory.SetCurrentDirectory config.WorkingDirectory
        options <- loadImportantJsonFile "Options" (Path.Combine(getDataPath "Data", "options.json")) true

    let save() =
        try
            JSON.ToFile(configPath, true) config
            JSON.ToFile(Path.Combine(getDataPath "Data", "options.json"), true) options
        with err -> Logging.Critical("Failed to write options/config to file.", err)