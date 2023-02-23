﻿namespace Interlude.Features.Play

open OpenTK
open Percyqaz.Flux.Audio
open Percyqaz.Flux.Input
open Percyqaz.Flux.UI
open Prelude.Common
open Prelude.Charts.Formats.Interlude
open Prelude.Scoring
open Prelude.Gameplay.Mods
open Prelude.Data.Themes
open Interlude.Options
open Interlude.Content
open Interlude.UI
open Interlude.Features
open Interlude.Features.Play.GameplayWidgets

[<AutoOpen>]
module Utils =

    let inline add_widget (screen: Screen, playfield: NoteRenderer, state: PlayState) (constructor: 'T * PlayState -> #Widget) = 
        let config: ^T = getGameplayConfig<'T>()
        let pos: WidgetConfig = (^T: (member Position: WidgetConfig) config)
        if pos.Enabled then
            let w = constructor(config, state)
            w.Position <- { Left = pos.LeftA %+ pos.Left; Top = pos.TopA %+ pos.Top; Right = pos.RightA %+ pos.Right; Bottom = pos.BottomA %+ pos.Bottom }
            if pos.Float then screen.Add w else playfield.Add w

[<AbstractClass>]
type IPlayScreen(chart: ModChart, pacemakerInfo: PacemakerInfo, ruleset: Ruleset, scoring: IScoreMetric) as this =
    inherit Screen()
    
    let firstNote = offsetOf chart.Notes.First.Value

    let onHit = new Event<HitEvent<HitEventGuts>>()

    let state: PlayState =
        {
            Ruleset = ruleset
            Scoring = scoring
            HP = scoring.HP
            OnHit = onHit.Publish
            CurrentChartTime = fun () -> Song.timeWithOffset() - firstNote
            Pacemaker = pacemakerInfo
        }

    let noteRenderer = NoteRenderer scoring

    do
        this.Add noteRenderer

        if noteskinConfig().EnableColumnLight then
            noteRenderer.Add(new ColumnLighting(chart.Keys, noteskinConfig(), state))

        if noteskinConfig().Explosions.FadeTime >= 0.0f then
            noteRenderer.Add(new Explosions(chart.Keys, noteskinConfig(), state))

        noteRenderer.Add(LaneCover())

        this.AddWidgets()

        scoring.SetHitCallback onHit.Trigger

    abstract member AddWidgets : unit -> unit

    member this.Playfield = noteRenderer
    member this.State = state
    member this.Chart = chart

    override this.OnEnter(prev) =
        Dialog.close()
        Background.dim (float32 options.BackgroundDim.Value)
        Screen.Toolbar.hide()
        Song.changeRate Gameplay.rate.Value
        Song.changeGlobalOffset (toTime options.AudioOffset.Value)
        Song.onFinish <- SongFinishAction.Wait
        Song.playLeadIn()
        Input.finish_frame_events()

    override this.OnExit next =
        Background.dim 0.7f
        if next <> Screen.Type.Score then Screen.Toolbar.show()

    override this.OnBack() = Some Screen.Type.LevelSelect