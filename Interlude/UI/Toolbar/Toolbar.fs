﻿namespace Interlude.UI.Toolbar

open System.Drawing
open Interlude
open Interlude.Options
open Interlude.Graphics
open Interlude.UI
open Interlude.UI.Animation
open Interlude.UI.Components
open Interlude.UI.OptionsMenu
open Interlude.Utils

type Toolbar() as this =
    inherit Widget()

    let HEIGHT = 70.0f

    let barSlider = new AnimationFade 1.0f
    let notifSlider = new AnimationFade 0.0f

    let shown() = not Screen.hideToolbar

    let mutable userCollapse = false
    
    do
        this
        |-* barSlider
        |-* notifSlider
        |-+ TextBox(K version, K (Color.White, Color.Black), 1.0f)
            .Position (Position.Box (1.0f, 1.0f, -305.0f, 0.0f, 300.0f, HEIGHT * 0.5f))
        |-+ TextBox((fun () -> System.DateTime.Now.ToString()), K (Color.White, Color.Black), 1.0f)
            .Position( Position.Box (1.0f, 1.0f, -305.0f, HEIGHT * 0.5f, 300.0f, HEIGHT * 0.5f) )
        |-+ Button(
                (fun () -> Screen.back Screen.TransitionFlag.UnderLogo),
                sprintf "%s %s  " Icons.back (L"menu.back"),
                Hotkey.Exit )
            .Position( Position.Box (0.0f, 1.0f, 200.0f, HEIGHT) )
        |-+ Button(
                ( fun () -> if shown() && Screen.currentType <> Screen.Type.Play && Screen.currentType <> Screen.Type.Replay then OptionsMenuRoot.show() ),
                L"menu.options",
                Hotkey.Options )
            .Position( Position.Box(0.0f, 0.0f, 0.0f, -HEIGHT, 200.0f, HEIGHT) )
        |-+ Button(
                ( fun () -> if shown() then Screen.change Screen.Type.Import Screen.TransitionFlag.Default ),
                L"menu.import",
                Hotkey.Import )
            .Position( Position.Box(0.0f, 0.0f, 200.0f, -HEIGHT, 200.0f, HEIGHT) )
        |-+ Button(
                ( fun () -> if shown() then MarkdownReader.help() ),
                L"menu.help",
                Hotkey.Help )
            .Position( Position.Box(0.0f, 0.0f, 400.0f, -HEIGHT, 200.0f, HEIGHT) )
        |-+ Button(
                ( fun () -> if shown() then TaskDisplay.Dialog().Show() ),
                L"menu.tasks",
                Hotkey.Tasks )
            .Position( Position.Box(0.0f, 0.0f, 600.0f, -HEIGHT, 200.0f, HEIGHT) )
        |=+ Jukebox()

    override this.VisibleBounds = this.Parent.Value.VisibleBounds

    override this.Draw() = 
        let { Rect.Left = l; Top = t; Right = r; Bottom = b } = this.Bounds
        Draw.rect (Rect.Create(l, t - HEIGHT, r, t)) (Style.main 100 ()) Sprite.Default
        Draw.rect (Rect.Create(l, b, r, b + HEIGHT)) (Style.main 100 ()) Sprite.Default
        if barSlider.Value > 0.01f then
            let s = this.Bounds.Width / 48.0f
            for i in 0 .. 47 do
                let level = System.Math.Min((Audio.waveForm.[i] + 0.01f) * barSlider.Value * 0.4f, HEIGHT)
                Draw.rect (Rect.Create(l + float32 i * s + 2.0f, t - HEIGHT, l + (float32 i + 1.0f) * s - 2.0f, t - HEIGHT + level)) (Style.accentShade(int level, 1.0f, 0.5f)) Sprite.Default
                Draw.rect (Rect.Create(r - (float32 i + 1.0f) * s + 2.0f, b + HEIGHT - level, r - float32 i * s - 2.0f, b + HEIGHT)) (Style.accentShade(int level, 1.0f, 0.5f)) Sprite.Default
        base.Draw()
        Terminal.draw()

    override this.Update(elapsedTime, bounds) =
        if shown() && (!|Hotkey.Toolbar).Tapped() then
            userCollapse <- not userCollapse
            barSlider.Target <- if userCollapse then 0.0f else 1.0f
        Terminal.update()
        base.Update(elapsedTime, bounds.Expand (0.0f, -HEIGHT * if Screen.hideToolbar then 0.0f else barSlider.Value))