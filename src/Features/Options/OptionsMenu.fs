﻿namespace Interlude.Features.OptionsMenu

open Percyqaz.Flux.UI
open Percyqaz.Flux.Graphics
open Interlude.Utils
open Interlude.UI
open Interlude.UI.Menu
open Interlude.Features.LevelSelect

module OptionsMenuRoot =

    type private TileButton(body: Callout, onclick: unit -> unit) =
        inherit StaticContainer(NodeType.Button (onclick))
    
        let body_width, body_height = Callout.measure body
    
        member val Disabled = false with get, set
        member val Margin = (0.0f, 20.0f) with get, set
        member this.Height = body_height + snd this.Margin * 2.0f
    
        override this.Init(parent) =
            this |* Clickable.Focus(this)
            base.Init(parent)
    
        override this.Draw() =
            let color = 
                if this.Disabled then Colors.shadow_1
                elif this.Focused then Colors.pink_accent
                else Colors.shadow_1
            Draw.rect this.Bounds color.O3
            Draw.rect (this.Bounds.Expand(0.0f, 5.0f).SliceBottom(5.0f)) color
            Callout.draw (this.Bounds.Left + fst this.Margin, this.Bounds.Top + snd this.Margin, body_width, body_height, Colors.text, body)
    
    type OptionsPage() as this =
        inherit Page()

        let tooltip_hint = 
            Callout.Normal
                .Icon(Icons.info)
                .Title(L"options.ingame_help.name")
                .Body(L"options.ingame_help.hint")
                .Hotkey("tooltip")

        let system =
            TileButton(
                Callout.Normal
                    .Icon(Icons.system)
                    .Title(L"system.name"),
                fun () -> Menu.ShowPage System.SystemPage)

        let gameplay =
            TileButton(
                Callout.Normal
                    .Icon(Icons.gameplay)
                    .Title(L"gameplay.name"),
                fun () -> Menu.ShowPage Gameplay.GameplayPage)
                
        let debug =
            TileButton(
                Callout.Normal
                    .Icon(Icons.debug)
                    .Title(L"debug.name"),
                fun () -> Menu.ShowPage Debug.DebugPage)

        do
            this.Content(
                GridContainer(system.Height, 3,
                    Spacing = (50.0f, 0.0f),
                    Position = Position.SliceTop(400.0f).SliceBottom(system.Height).Margin(200.0f, 0.0f))
                |+ system
                |+ gameplay
                |+ debug
            )
            this |* Callout.frame (tooltip_hint) 
                ( fun (w, h) -> Position.SliceBottom(600.0f).SliceTop(h + 40.0f).Margin(200.0f, 0.0f) )

        override this.Title = L"options.name"
        override this.OnClose() = LevelSelect.refresh_all()

    let show() = Menu.ShowPage OptionsPage