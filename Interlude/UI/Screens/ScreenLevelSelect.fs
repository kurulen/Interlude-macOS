﻿namespace Interlude.UI

open System
open System.Linq
open Prelude.Common
open Prelude.Data.ScoreManager
open Prelude.Data.ChartManager
open Prelude.Gameplay.Score
open Interlude.Themes
open Interlude.Utils
open Interlude.Render
open Interlude.Options
open Interlude.Input
open Interlude.UI.Animation
open Interlude.UI.Components
open Interlude.Gameplay
open OpenTK

module private ScreenLevelSelectVars =

    //TODO LIST
    //  MAKE IT LOOK NICE
    //    SHOW PBS (LAMP, ACCURACY, GRADE)
    let mutable selectedGroup = ""
    let mutable selectedChart = "" //hash
    let mutable scrollTo = false
    let mutable expandedGroup = ""
    let mutable scrollBy = fun amt -> ()
    let mutable colorVersionGlobal = 0
    let mutable colorFunc = fun (_, _, _) -> Color.Transparent

    //todo: have these update when score system is changed, could be done remotely, exactly when settings are changed
    let mutable scoreSystem = "SC+ (J4)"
    let mutable hpSystem = "VG"

    type Navigation = 
    | Nothing
    | Backward of string * CachedChart
    | Forward of bool

    let mutable navigation = Nothing

    let switchCurrentChart(cc, groupName) =
        match cache.LoadChart(cc) with
        | Some c ->
            changeChart(cc, c)
            selectedChart <- cc.Hash
            expandedGroup <- groupName
            selectedGroup <- groupName
            scrollTo <- true
        | None -> Logging.Error("Couldn't load cached file: " + cc.FilePath) ""

    let playCurrentChart() =
        Screens.addScreen(ScreenPlay >> (fun s -> s :> Screen), ScreenTransitionFlag.Default)

module ScreenLevelSelect =

    //publicly accessible so that other importing can request that the level select is refreshed
    let mutable refresh = false

    open ScreenLevelSelectVars

    type ScoreCard(data: ScoreInfoProvider) as this =
        inherit Widget()

        do
            this.Add(
                new TextBox(sprintf "%s / %i" (data.Accuracy.Format()) (let (_, _, _, _, _, cbs) = data.Accuracy.State in cbs) |> K, K Color.White, 0.0f)
                |> positionWidget(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.5f, 0.0f, 0.6f))
            this.Add(
                new TextBox(sprintf "%s / %ix" (data.Lamp.ToString()) (let (_, _, _, _, combo, _) = data.Accuracy.State in combo) |> K, K Color.White, 0.0f)
                |> positionWidget(0.0f, 0.0f, 0.0f, 0.6f, 0.0f, 0.5f, 0.0f, 1.0f))
            this.Add(
                new TextBox(K data.Mods, K Color.White, 1.0f)
                |> positionWidget(0.0f, 0.5f, 0.0f, 0.6f, 0.0f, 1.0f, 0.0f, 1.0f))
            this.Add(
                new Clickable(
                    (fun () ->
                        Screens.addScreen(
                            (fun () ->
                                new ScreenScore(data, (PersonalBestType.None, PersonalBestType.None, PersonalBestType.None)) :> Screen),
                            ScreenTransitionFlag.Default)),
                    ignore))
            this.Reposition(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 75.0f, 0.0f)

        override this.Draw() =
            Draw.rect this.Bounds (Screens.accentShade(127, 0.8f, 0.0f)) Sprite.Default
            base.Draw()

    type Scoreboard() as this =
        inherit Widget()

        let flowContainer = new FlowContainer()
        let mutable empty = false

        do
            this.Add(flowContainer)

        member this.Refresh() =
            flowContainer.Clear()
            empty <- true
            match chartSaveData with
            | None -> ()
            | Some d ->
                for score in d.Scores do
                    empty <- false
                    flowContainer.Add(new ScoreCard(new ScoreInfoProvider(score, currentChart.Value)))

    type SelectableItem(content: Choice<string * CachedChart, string * SelectableItem list>) =
        
        let hover = new AnimationFade(0.0f)
        //todo: colors
        let mutable colorVersion = -1
        let mutable color = Color.Transparent
        let mutable chartData = None
        let mutable pbData = (None, None, None)
        let animation = new AnimationGroup()

        do
            animation.Add(hover)

        member this.Draw(top: float32): float32 =
            match content with
            | Choice1Of2 (groupName, cc) ->
                if (top > 70.0f && top < Render.vheight) then
                    let bounds = Rect.create (Render.vwidth * 0.4f) top Render.vwidth (top + 85.0f)
                    let struct (left, _, right, bottom) = bounds
                    Draw.rect(bounds)(Screens.accentShade(127, 0.8f, 0.0f))Sprite.Default
                    let twidth = Math.Max(Text.measure(font(), cc.Artist + " - " + cc.Title) * 23.0f, Text.measure(font(), cc.DiffName + " // " + cc.Creator) * 20.0f + 40.0f) + 20.0f
                    let stripeLength = twidth + (right - left) * 0.3f * hover.Value
                    Draw.quad
                        (Quad.create <| new Vector2(left, top) <| new Vector2(left + stripeLength, top) <| new Vector2(left + stripeLength - 40.0f, bottom - 25.0f) <| new Vector2(left, bottom - 25.0f))
                        (Quad.colorOf <| Screens.accentShade(127, 1.0f, 0.2f))
                        (Sprite.gridUV(0, 0)Sprite.Default)
                    Draw.rect(Rect.sliceBottom(25.0f)bounds)(Screens.accentShade(60, 0.3f, 0.0f))Sprite.Default
                    Text.draw(font(), cc.Artist + " - " + cc.Title, 23.0f, left, top, Color.White)
                    Text.draw(font(), cc.DiffName + " // " + cc.Creator, 18.0f, left, top + 30.0f, Color.White)

                    let f p m =
                        match p with
                        | None -> "--"
                        | Some ((p1, r1), (p2, r2)) ->
                            if r1 < rate then
                                if r2 < rate then
                                    "--"
                                else
                                    sprintf "%s (%.2fx)" (m p2) r2
                            else
                                sprintf "%s (%.2fx)" (m p1) r1
                    let (acc, lamp, clear) = pbData
                    Text.draw(font(), f acc (fun x -> sprintf "%.2f%%" (100.0 * x)), 15.0f, left, top + 60.0f, Color.White)
                    Text.draw(font(), f lamp (fun x -> x.ToString()), 15.0f, left + 200.0f, top + 60.0f, Color.White)
                    Text.draw(font(), f clear (fun x -> if x then "CLEAR" else "FAILED"), 15.0f, left + 400.0f, top + 60.0f, Color.White)

                    let border = Rect.expand(5.0f, 5.0f)bounds
                    let borderColor = if selectedChart = cc.Hash then Color.White else color
                    if borderColor.A > 0uy then
                        Draw.rect(Rect.sliceLeft(5.0f)(border))(borderColor)Sprite.Default
                        Draw.rect(Rect.sliceTop(5.0f)(border))(borderColor)Sprite.Default
                        Draw.rect(Rect.sliceRight(5.0f)(border))(borderColor)Sprite.Default
                        Draw.rect(Rect.sliceBottom(5.0f)(border))(borderColor)Sprite.Default
                top + 95.0f
            | Choice2Of2 (name, items) ->
                if (top > 90.0f && top < Render.vheight) then
                    let bounds = Rect.create (Render.vwidth * 0.4f) top (Render.vwidth * 0.6f) (top + 65.0f)
                    let struct (left, _, right, bottom) = bounds
                    Draw.rect(bounds)(if selectedGroup = name then Screens.accentShade(127, 1.0f, 0.2f) else Screens.accentShade(127, 0.5f, 0.0f))Sprite.Default
                    Text.drawFill(font(), name, bounds, Color.White, 0.5f)
                if expandedGroup = name then
                    List.fold (fun t (i: SelectableItem) -> i.Draw(t)) (top + 80.0f) items
                else
                    top + 80.0f

        member this.Update(top: float32, elapsedTime): float32 =
            this.Navigate()
            match content with
            | Choice1Of2 (groupName, cc) ->
                if scrollTo && groupName = selectedGroup && cc.Hash = selectedChart then
                    scrollBy(-top + 500.0f)
                    scrollTo <- false
                if (top > 150.0f) then
                    if colorVersion < colorVersionGlobal then
                        let f key (d: Collections.Generic.Dictionary<string, PersonalBests<_>>) =
                            if d.ContainsKey(key) then Some d.[key] else None
                        colorVersion <- colorVersionGlobal
                        if chartData.IsNone then chartData <- scores.GetScoreData(cc.Hash)
                        match chartData with
                        | Some d ->
                            pbData <- (f scoreSystem d.Accuracy, f scoreSystem d.Lamp, f (scoreSystem + "|" + hpSystem) d.Clear)
                        | None -> ()
                        color <- colorFunc(pbData)

                    let bounds = Rect.create (Render.vwidth * 0.4f) top (Render.vwidth * 0.8f) (top + 85.0f)
                    if Mouse.Hover(bounds) then 
                        hover.SetTarget(1.0f)
                        if Mouse.Click(Input.MouseButton.Left) then
                            if selectedChart = cc.Hash then
                                playCurrentChart()
                            else
                                switchCurrentChart(cc, groupName)
                        elif Mouse.Click(Input.MouseButton.Right) then
                            expandedGroup <- ""
                            scrollTo <- true
                    else
                        hover.SetTarget(0.0f)
                    animation.Update(elapsedTime)
                top + 95.0f
            | Choice2Of2 (name, items) ->
                if scrollTo && name = selectedGroup && name <> expandedGroup then
                    scrollBy(-top + 500.0f)
                    scrollTo <- false
                if (top > 170.0f) then                       
                    let bounds = Rect.create (Render.vwidth * 0.4f) top (Render.vwidth * 0.9f) (top + 65.0f)
                    if Mouse.Hover(bounds) then 
                        hover.SetTarget(1.0f)
                        if Mouse.Click(Input.MouseButton.Left) then
                            if expandedGroup = name then expandedGroup <- "" else expandedGroup <- name
                    else
                        hover.SetTarget(0.0f)
                    animation.Update(elapsedTime)
                if expandedGroup = name then
                    List.fold (fun t (i: SelectableItem) -> i.Update(t, elapsedTime)) (top + 80.0f) items
                else
                    List.iter (fun (i: SelectableItem) -> i.Navigate()) items
                    top + 80.0f

            member this.Navigate() =
                match content with
                | Choice1Of2 (groupName, cc) ->
                    match navigation with
                    | Nothing -> ()
                    | Forward b ->
                        if b then
                            switchCurrentChart(cc, groupName); navigation <- Nothing
                        elif groupName = selectedGroup && cc.Hash = selectedChart then
                            navigation <- Forward true
                    | Backward (groupName2, cc2) ->
                        if groupName = selectedGroup && cc.Hash = selectedChart then
                            switchCurrentChart(cc2, groupName2); navigation <- Nothing
                        else navigation <- Backward(groupName, cc)
                | _ -> () //nyi

    let private firstCharacter(s : string) =
        if (s.Length = 0) then "?"
        else
            if Char.IsLetterOrDigit(s.[0]) then s.[0].ToString().ToUpper() else "?"

    //todo: maybe move to ChartManagement in Prelude
    let groupBy = dict[
            "Physical", fun c -> let i = int (c.Physical / 2.0) * 2 in i.ToString().PadLeft(2, '0') + " - " + (i + 2).ToString().PadLeft(2, '0')
            "Technical", fun c -> let i = int (c.Technical / 2.0) * 2 in i.ToString().PadLeft(2, '0') + " - " + (i + 2).ToString().PadLeft(2, '0')
            "Pack", fun c -> c.Pack
            "Title", fun c -> firstCharacter(c.Title)
            "Artist", fun c -> firstCharacter(c.Artist)
            "Creator", fun c -> firstCharacter(c.Creator)
            "Keymode", fun c -> c.Keys.ToString() + "k"
        ]

    let sortBy = dict[
            "Physical", Comparison(fun a b -> a.Physical.CompareTo(b.Physical))
            "Technical", Comparison(fun a b -> a.Technical.CompareTo(b.Technical))
            "Title", Comparison(fun a b -> a.Title.CompareTo(b.Title))
            "Artist", Comparison(fun a b -> a.Artist.CompareTo(b.Artist))
            "Creator", Comparison(fun a b -> a.Creator.CompareTo(b.Creator))
        ]

    let colorBy = dict[
            "Grade",
            fun (acc, lamp, clear) ->
                match acc with
                | None -> Color.Transparent
                | Some ((p1, r1), (p2, r2)) ->
                    if r1 < rate then
                        if r2 < rate then
                            Color.Transparent
                        else
                            themeConfig.GradeColors.[grade p2 themeConfig.GradeThresholds] |> otkColor
                    else
                        themeConfig.GradeColors.[grade p1 themeConfig.GradeThresholds] |> otkColor
        ]

open ScreenLevelSelect
open ScreenLevelSelectVars

type ScreenLevelSelect() as this =
    inherit Screen()

    let mutable selection: SelectableItem list = []
    let mutable lastItem: (string * CachedChart) option = None
    let scrollPos = new AnimationFade(300.0f)
    let searchText = new Setting<string>("")
    let searchTimer = System.Diagnostics.Stopwatch()
    let scoreboard = new Scoreboard()

    let refresh() =
        let groups = cache.GetGroups groupBy.[Options.options.ChartGroupMode.Get()] sortBy.[Options.options.ChartSortMode.Get()] <| searchText.Get()
        if groups.Count = 1 then
            let g = groups.Keys.First()
            if groups.[g].Count = 1 then
                let cc = groups.[g].[0]
                match cache.LoadChart(cc) with
                | Some c ->
                    changeChart(cc, c)
                | None -> Logging.Error("Couldn't load cached file: " + cc.FilePath) ""
        lastItem <- None
        colorVersionGlobal <- 0
        colorFunc <- colorBy.["Grade"]
        selection <- 
            groups.Keys
            |> Seq.sort
            |> Seq.map
                (fun k ->
                    groups.[k]
                    |> Seq.map (fun cc ->
                        match currentCachedChart with
                        | None -> ()
                        | Some c -> if c.Hash = cc.Hash then selectedChart <- c.Hash; selectedGroup <- k
                        lastItem <- Some (k, cc)
                        SelectableItem(Choice1Of2 (k, cc)))
                    |> List.ofSeq
                    |> fun l -> SelectableItem(Choice2Of2 (k, l)))
            |> List.ofSeq
        scrollTo <- true
        expandedGroup <- selectedGroup

    do
        if not <| sortBy.ContainsKey(Options.options.ChartSortMode.Get()) then Options.options.ChartSortMode.Set("Title")
        if not <| groupBy.ContainsKey(Options.options.ChartGroupMode.Get()) then Options.options.ChartGroupMode.Set("Pack")
        this.Animation.Add(scrollPos)
        scrollBy <- fun amt -> scrollPos.SetTarget(scrollPos.Target + amt)
        this.Add(
            let sorts = sortBy.Keys |> Array.ofSeq
            new Dropdown(sorts, Array.IndexOf(sorts, Options.options.ChartSortMode.Get()),
                (fun i -> Options.options.ChartSortMode.Set(sorts.[i]); refresh()), "Sort by", 50.0f)
            |> positionWidget(-400.0f, 1.0f, 100.0f, 0.0f, -250.0f, 1.0f, 400.0f, 0.0f))
        this.Add(
            let groups = groupBy.Keys |> Array.ofSeq
            new Dropdown(groups, Array.IndexOf(groups, Options.options.ChartGroupMode.Get()),
                (fun i -> Options.options.ChartGroupMode.Set(groups.[i]); refresh()), "Group by", 50.0f)
            |> positionWidget(-200.0f, 1.0f, 100.0f, 0.0f, -50.0f, 1.0f, 400.0f, 0.0f))
        this.Add(
            new TextEntry(new WrappedSetting<string, string>(searchText, (fun s -> searchTimer.Restart(); s), id), Some (Options.options.Hotkeys.Search :> ISettable<Bind>), "search")
            |> positionWidget(-600.0f, 1.0f, 20.0f, 0.0f, -50.0f, 1.0f, 80.0f, 0.0f))
        this.Add(scoreboard |> positionWidget(50.0f, 0.0f, 220.0f, 0.0f, -50.0f, 0.4f, -50.0f, 1.0f))
        onChartChange <- scoreboard.Refresh
        this.Add(
            new TextBox((fun () -> match currentCachedChart with None -> "" | Some c -> c.Title), K Color.White, 0.5f)
            |> positionWidget(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.4f, 100.0f, 0.0f))
        this.Add(
            new TextBox((fun () -> match currentCachedChart with None -> "" | Some c -> c.DiffName), K Color.White, 0.5f)
            |> positionWidget(0.0f, 0.0f, 100.0f, 0.0f, 0.0f, 0.4f, 160.0f, 0.0f))
        this.Add(
            new TextBox((fun () -> match difficultyRating with None -> "0.00" | Some d -> sprintf "%.2f" d.Physical), K Color.White, 0.5f)
            |> positionWidget(50.0f, 0.0f, -240.0f, 1.0f, 0.0f, 0.2f, -140.0f, 1.0f))
        this.Add(
            new TextBox((fun () -> match difficultyRating with None -> "0.00" | Some d -> sprintf "%.2f" d.Technical), K Color.White, 0.5f)
            |> positionWidget(0.0f, 0.2f, -240.0f, 1.0f, -50.0f, 0.4f, -140.0f, 1.0f))
        this.Add(
            new TextBox(getModString, K Color.White, 0.5f)
            |> positionWidget(0.0f, 0.0f, -140.0f, 1.0f, -50.0f, 0.4f, -70.0f, 1.0f))
        refresh()

    override this.Update(elapsedTime, bounds) =
        base.Update(elapsedTime, bounds)
        if Options.options.Hotkeys.Select.Get().Tapped(false) then
            playCurrentChart()
        elif Options.options.Hotkeys.UpRateSmall.Get().Tapped(false) then changeRate(0.01f); colorVersionGlobal <- colorVersionGlobal + 1
        elif Options.options.Hotkeys.UpRateHalf.Get().Tapped(false) then changeRate(0.05f); colorVersionGlobal <- colorVersionGlobal + 1
        elif Options.options.Hotkeys.UpRate.Get().Tapped(false) then changeRate(0.1f); colorVersionGlobal <- colorVersionGlobal + 1
        elif Options.options.Hotkeys.DownRateSmall.Get().Tapped(false) then changeRate(-0.01f); colorVersionGlobal <- colorVersionGlobal + 1
        elif Options.options.Hotkeys.DownRateHalf.Get().Tapped(false) then changeRate(-0.05f); colorVersionGlobal <- colorVersionGlobal + 1
        elif Options.options.Hotkeys.DownRate.Get().Tapped(false) then changeRate(-0.1f); colorVersionGlobal <- colorVersionGlobal + 1
        elif Options.options.Hotkeys.Next.Get().Tapped(false) then
            if lastItem.IsSome then
                let h = (lastItem.Value |> snd).Hash
                navigation <- Navigation.Forward(selectedGroup = fst lastItem.Value && selectedChart = h)
        elif Options.options.Hotkeys.Previous.Get().Tapped(false) then
            if lastItem.IsSome then
                navigation <- Navigation.Backward(lastItem.Value)
        let struct (left, top, right, bottom)  = this.Bounds
        let bottomEdge =
            selection
            |> List.fold (fun t (i: SelectableItem) -> i.Update(t, elapsedTime)) scrollPos.Value
        let height = bottomEdge - scrollPos.Value - 320.0f
        if Mouse.pressed(Input.MouseButton.Right) then
            scrollPos.SetTarget(-(Mouse.Y() - (top + 250.0f))/(bottom - top - 250.0f) * height)
        scrollPos.SetTarget(Math.Min(Math.Max(scrollPos.Target + float32 (Mouse.Scroll()) * 100.0f, -height + 600.0f), 300.0f))
        if searchTimer.ElapsedMilliseconds > 400L then searchTimer.Reset(); refresh()
            

    override this.Draw() =
        let struct (left, top, right, bottom) = this.Bounds
        //level select stuff
        Stencil.create(false)
        Draw.rect(Rect.create 0.0f (top + 170.0f) Render.vwidth bottom)(Color.Transparent)(Sprite.Default)
        Stencil.draw()
        let bottomEdge =
            selection
            |> List.fold (fun t (i: SelectableItem) -> i.Draw(t)) scrollPos.Value
        Stencil.finish()
        //todo: make this render right, is currently bugged
        let scrollPos = (scrollPos.Value / (scrollPos.Value - bottomEdge)) * (bottom - top - 100.0f)
        Draw.rect(Rect.create (Render.vwidth - 10.0f) (top + 225.0f + scrollPos) (Render.vwidth - 5.0f) (top + 245.0f + scrollPos))(Color.White)(Sprite.Default)

        Draw.rect(Rect.create left top right (top + 170.0f))(Screens.accentShade(100, 0.6f, 0.0f))(Sprite.Default)
        Draw.rect(Rect.create left (top + 170.0f) right (top + 175.0f))(Screens.accentShade(255, 0.8f, 0.0f))(Sprite.Default)
        base.Draw()

    override this.OnEnter(prev) =
        base.OnEnter(prev)
        scoreboard.Refresh()
        colorVersionGlobal <- colorVersionGlobal + 1