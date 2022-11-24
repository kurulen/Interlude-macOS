﻿namespace Interlude.Features

open System
open Percyqaz.Common
open Percyqaz.Flux.Audio
open Prelude.Common
open Prelude.Charts.Formats.Interlude
open Prelude.Gameplay.Mods
open Prelude.Scoring
open Prelude.Gameplay.Difficulty
open Prelude.Gameplay.NoteColors
open Prelude.Data.Charts
open Prelude.Data.Charts.Tables
open Prelude.Data.Charts.Caching
open Prelude.Data.Charts.Collections
open Prelude.Data.Scores
open Interlude
open Interlude.Options
open Interlude.UI
open Interlude.Utils

module Gameplay =

    let mutable autoplay = false
    let mutable enablePacemaker = false

    let mutable ruleset : Ruleset = getCurrentRuleset()
    let mutable rulesetId = Ruleset.hash ruleset
    
    module Chart =
    
        let mutable cacheInfo : CachedChart option = None
        let mutable current : Chart option = None
        let mutable context : LibraryContext = LibraryContext.None
        let mutable withMods : ModChart option = None
        let mutable withColors : ColorizedChart option = None
    
        let mutable rating : RatingReport option = None
        let mutable saveData : ChartSaveData option = None

        let  _rate = Setting.rate 1.0f
        let _selectedMods = Setting.simple Map.empty

        let update() =
            match current with
            | None -> ()
            | Some c ->
                let modChart = getModChart _selectedMods.Value c
                withMods <- Some modChart
                rating <- 
                    RatingReport(modChart.Notes, _rate.Value, options.Playstyles.[modChart.Keys - 3], modChart.Keys)
                    |> Some
                withColors <- None
        
        let chartChangeEvent = Event<unit>()
        let onChange = chartChangeEvent.Publish

        let change(cache, ctx, c) =
            cacheInfo <- Some cache
            current <- Some c
            context <- ctx
            saveData <- Some (Scores.getOrCreateData c)
            Background.load c.BackgroundPath
            if Song.change (c.AudioPath, saveData.Value.Offset - c.FirstNote, _rate.Value) then
                Song.playFrom c.Header.PreviewTime
            options.CurrentChart.Value <- cache.FilePath
            update()
            chartChangeEvent.Trigger()

        let colored() =
            match withMods with
            | None -> failwith "Tried to get coloredChart when no modifiedChart exists"
            | Some mc ->
                withColors <- Option.defaultWith (fun () -> getColoredChart (Content.noteskinConfig().NoteColors) mc) withColors |> Some
                withColors.Value

        let recolor() = withColors <- None
    
    module Collections =
        
        open Prelude.Data.Charts.Library
    
        let mutable current =
            let selection = options.SelectedCollection.Value
            match collections.Get selection with
            | Some c -> c
            | None -> collections.CreateFolder(selection, Icons.heart).Value |> Folder
    
        let notifyChangeRate v =
            match Chart.context with
            | LibraryContext.Playlist (_, _, d) -> d.Rate.Value <- v
            | _ -> ()
    
        let notifyChangeMods mods =
            match Chart.context with
            | LibraryContext.Playlist (_, _, d) -> d.Mods.Value <- mods
            | _ -> ()
    
        let notifyChangeChart (rate: Setting.Bounded<float32>) (mods: Setting<ModState>) =
            match Chart.context with
            | LibraryContext.Playlist (_, _, d) -> 
                rate.Value <- d.Rate.Value
                mods.Value <- d.Mods.Value
            | _ -> ()
    
        let select(name: string) =
            match collections.Get name with
            | Some c -> 
                options.SelectedCollection.Set name
                current <- c
            | None -> Logging.Error (sprintf "No such collection with name '%s'" name)

    let rate =
        Chart._rate
        |> Setting.trigger ( fun v ->
                Collections.notifyChangeRate v
                Song.changeRate v
                Chart.update() )
    let selectedMods =
        Chart._selectedMods
        |> Setting.trigger ( fun mods ->
                Collections.notifyChangeMods mods
                Chart.update() )

    do Chart.onChange.Add ( fun () -> Collections.notifyChangeChart rate selectedMods)

    let makeScore (replayData, keys) : Score =
        {
            time = DateTime.Now
            replay = Replay.compress replayData
            rate = rate.Value
            selectedMods = selectedMods.Value |> ModChart.filter Chart.withMods.Value
            layout = options.Playstyles.[keys - 3]
            keycount = keys
        }

    let setScore (pacemakerMet: bool) (data: ScoreInfoProvider) : BestFlags =
        if data.ModStatus < ModStatus.Unstored &&
           (options.ScaveScoreIfUnderPace.Value || pacemakerMet)
        then
            if data.ModStatus = ModStatus.Ranked then
                // todo: score uploading goes here when online added
                Scores.saveScoreWithPbs Chart.saveData.Value rulesetId data
            else
                Scores.saveScore Chart.saveData.Value data
                BestFlags.Default
        else BestFlags.Default

    // todo: this is a temporary measure until the table module has a better interface
    let selectTable(id: string) =
        try 
            Table.load id
            options.SelectedTable.Set id
        with err -> Logging.Error (sprintf "Error loading table '%s'" id, err)

    let save() =
        Table.save()
        Scores.save()
        Library.save()

    let init() =
        match options.SelectedCollection.Value with "" -> () | v -> Collections.select v
        match options.SelectedTable.Value with "" -> () | v -> selectTable v
        try
            let c, ch =
                match Library.lookup options.CurrentChart.Value with
                | Some cc ->
                    match Library.load cc with
                    | Some c -> cc, c
                    | None ->
                        Logging.Error("Could not load chart file: " + cc.FilePath)
                        Library.getGroups Unchecked.defaultof<_> (K (0, "All")) (Comparison(fun _ _ -> 0)) []
                        |> fun d -> fst d.[(0, "All")].[0]
                        |> fun c -> c, Library.load(c).Value
                | None ->
                    Logging.Info("Could not find cached chart: " + options.CurrentChart.Value)
                    Library.getGroups Unchecked.defaultof<_> (K (0, "All")) (Comparison(fun _ _ -> 0)) []
                    |> fun d -> fst d.[(0, "All")].[0]
                    |> fun c -> c, Library.load(c).Value
            Chart.change(c, LibraryContext.None, ch)
        with err ->
            Logging.Debug("No charts installed")
            Background.load ""