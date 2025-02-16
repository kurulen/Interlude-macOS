﻿namespace Interlude.UI.Components

open Percyqaz.Flux.Input
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI

module private Dropdown =

    let ITEMSIZE = 60.0f
    
    type Item(label: string, onclick: unit -> unit) as this =
        inherit StaticContainer(NodeType.Button (fun () -> Style.click.Play(); onclick()))

        do
            this
            |+ Clickable(this.Select, OnHover = (fun b -> if b && not this.Focused then this.Focus()), Floating = true)
            |* Text(label,
                Align = Alignment.LEFT,
                Position = Position.Margin(10.0f, 5.0f))

        override this.OnFocus() = Style.hover.Play(); base.OnFocus()

        override this.Draw() =
            if this.Focused then Draw.rect this.Bounds (!*Palette.HOVER)
            base.Draw()

type Dropdown(items: (string * (unit -> unit)) seq, onclose: unit -> unit) as this =
    inherit Frame(NodeType.Switch (fun _ -> this.Items),
        Fill = !%Palette.DARK, Border = !%Palette.LIGHT)

    let flow = FlowContainer.Vertical(Dropdown.ITEMSIZE, Floating = true)

    do
        for (label, action) in items do
            flow |* Dropdown.Item(label, fun () -> action(); this.Close())
        this.Add flow

    override this.Update(elapsedTime, moved) =
        base.Update(elapsedTime, moved)
        if (!|"exit").Tapped() || not this.Focused || Mouse.leftClick() || Mouse.rightClick() then this.Close()
        if Mouse.hover this.Bounds then Input.finish_frame_events()

    override this.Init(parent: Widget) =
        base.Init parent
        this.Focus()

    member this.Close() = onclose()
    member private this.Items = flow

    member this.Place (x, y, width) =
        this.Position <- Position.Box(0.0f, 0.0f, x, y, width, this.Height)

    member this.Height = float32 (Seq.length items) * Dropdown.ITEMSIZE

    static member Selector (items: 'T seq) (labelFunc: 'T -> string) (selectFunc: 'T -> unit) (onclose: unit -> unit) =
        Dropdown(Seq.map (fun item -> (labelFunc item, fun () -> selectFunc item)) items, onclose)