﻿namespace Interlude.UI.Components.Selection.Menu

open System
open OpenTK
open Percyqaz.Common
open Percyqaz.Flux.Input
open Percyqaz.Flux.Graphics
open Prelude.Common
open Interlude
open Interlude.Utils
open Interlude.UI
open Interlude.UI.Components
open Interlude.UI.Components.Selection
open Interlude.UI.Components.Selection.Containers
open Interlude.UI.Components.Selection.Controls
open Interlude.UI.Components.Selection.Buttons

type SelectionPage =
    {
        Content: (string * SelectionPage -> unit) -> Selectable
        Callback: unit -> unit
    }

[<AutoOpen>]
module Helpers =
    let row xs =
        let r = ListSelectable(true)
        List.iter r.Add xs; r

    let column xs =
        let c = ListSelectable(false)
        List.iter c.Add xs; c

    let refreshRow number cons =
        let r = ListSelectable(true)
        let refresh() =
            r.Clear()
            let n = number()
            for i in 0 .. (n - 1) do
                r.Add(cons i n)
        refresh()
        r, refresh

    let N (s: string) = L ("options." + s + ".name")
    let T (s: string) = L ("options." + s + ".tooltip")
    let E (name: string) = Localisation.localiseWith [name] "misc.edit"

    let refreshChoice (options: string array) (widgets: Widget1 array array) (setting: Setting<int>) =
        let rec newSetting =
            {
                Set =
                    fun x ->
                        for w in widgets.[setting.Value] do if w.Parent.IsSome then selector.SParent.Value.SParent.Value.Remove w
                        for w in widgets.[x] do selector.SParent.Value.SParent.Value.Add w
                        setting.Value <- x
                Get = setting.Get
                Config = setting.Config
            }
        and selector : Selector<int> = Selector(Array.indexed options, newSetting)
        selector.Synchronized(fun () -> newSetting.Value <- newSetting.Value)
        selector

    let PRETTYTEXTWIDTH = 500.0f
    let PRETTYHEIGHT = 80.0f
    let PRETTYWIDTH = 1200.0f

type Divider() =
    inherit Widget1()

    member this.Position(y) =
        this.Position( Position.Box(0.0f, 0.0f, 100.0f, y - 5.0f, PRETTYWIDTH, 10.0f) )

    override this.Draw() =
        base.Draw()
        Draw.quad (Quad.ofRect this.Bounds) (struct(Color.White, Color.FromArgb(0, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), Color.White)) Sprite.DefaultQuad

type PrettySetting(name, widget: Selectable) as this =
    inherit Selectable()

    let mutable widget = widget

    do
        this
        |-+ widget.Position { Position.Default with Left = 0.0f %+ PRETTYTEXTWIDTH }
        |-+ TextBox(K (N name + ":"), (fun () -> ((if this.Selected then Style.accentShade(255, 1.0f, 0.2f) else Color.White), Color.Black)), 0.0f)
            .Position (Position.Box (0.0f, 0.0f, PRETTYTEXTWIDTH, PRETTYHEIGHT))
        |=+ TooltipRegion(T name)
    
    member this.Position(y, width, height) =
        this.Position( Position.Box(0.0f, 0.0f, 100.0f, y, width, height) )
    
    member this.Position(y, width) = this.Position(y, width, PRETTYHEIGHT)
    member this.Position(y) = this.Position(y, PRETTYWIDTH)

    override this.Draw() =
        if this.Selected then Draw.rect this.Bounds (Style.accentShade(120, 0.4f, 0.0f))
        elif this.Hover then Draw.rect this.Bounds (Style.accentShade(100, 0.4f, 0.0f))
        base.Draw()
    
    override this.Update(elapsedTime, bounds) =
        base.Update(elapsedTime, bounds)
        if widget.Hover && not widget.Selected && this.Selected then this.HoverChild <- None; this.Hover <- true
        
    override this.OnSelect() = if not widget.Hover then widget.Selected <- true
    override this.OnDehover() = base.OnDehover(); widget.OnDehover()

    member this.Refresh(w: Selectable) =
        widget.Destroy()
        widget <- w
        this |=+ widget.Position { Position.Default with Left = 0.0f %+ PRETTYTEXTWIDTH }

type PrettyButton(name, action) as this =
    inherit Selectable()

    do
        this
        |-+ TextBox(
            K (N name + "  >"),
            ( 
                fun () -> 
                    if this.Enabled then
                        ( (if this.Hover then Style.accentShade(255, 1.0f, 0.5f) else Color.White), Color.Black )
                    else (Color.Gray, Color.Black)
            ),
            0.0f )
        |-+ Clickable((fun () -> this.Selected <- true), (fun b -> if b then this.Hover <- true))
        |=+ TooltipRegion(T name)

    override this.OnSelect() =
        if this.Enabled then action()
        this.Selected <- false
    override this.Draw() =
        if this.Hover then Draw.rect this.Bounds (Style.accentShade(120, 0.4f, 0.0f))
        base.Draw()
    member this.Position(y) = 
        this.Position( Position.Box(0.0f, 0.0f, 100.0f, y, PRETTYWIDTH, PRETTYHEIGHT) )

    member val Enabled = true with get, set

    static member Once(name, action, notifText, notifType) =
        { new PrettyButton(name, action) with
            override this.OnSelect() =
                base.OnSelect()
                if base.Enabled then Notification.add (notifText, notifType)
                base.Enabled <- false
        }

type SelectionMenu(title: string, topLevel: SelectionPage) as this =
    inherit Dialog()
    
    let stack: (Selectable * (unit -> unit)) option array = Array.create 12 None
    let mutable namestack = []
    let mutable name = ""
    let body = Widget1()

    let wrapper main =
        let mutable disposed = false
        let w = 
            { new Selectable() with

                override this.Update(elapsedTime, bounds) =
                    if disposed then this.HoverChild <- None
                    base.Update(elapsedTime, bounds)
                    if not disposed then
                        Input.finish_frame_events()

                override this.VisibleBounds = this.Bounds
                override this.Dispose() = base.Dispose(); disposed <- true
            }
        w.Add main
        w.SelectedChild <- Some main
        w
    
    let rec add (label, page) =
        let n = List.length namestack
        namestack <- label :: namestack
        name <- String.Join(" > ", List.rev namestack)
        let w = wrapper (page.Content add)
        match stack.[n] with
        | None -> ()
        | Some (x, _) -> x.Destroy()
        stack.[n] <- Some (w, page.Callback)
        body.Add w
        let n = float32 n + 1.0f
        w.Reposition(0.0f, Viewport.vheight * n, 0.0f, Viewport.vheight * n)
        body.Move(0.0f, -Viewport.vheight * n, 0.0f, -Viewport.vheight * n)
    
    let back() =
        namestack <- List.tail namestack
        name <- String.Join(" > ", List.rev namestack)
        let n = List.length namestack
        let (w, callback) = stack.[n].Value in w.Dispose(); callback()
        let n = float32 n
        body.Move(0.0f, -Viewport.vheight * n, 0.0f, -Viewport.vheight * n)
    
    do
        this.Add body
        TextBox((fun () -> name), K (Color.White, Color.Black), 0.0f)
            .Position { Left = 0.0f %+ 20.0f; Top = 0.0f %+ 20.0f; Right = 1.0f %+ 0.0f; Bottom = 0.0f %+ 100.0f }
        |> this.Add
        add (title, topLevel)
    
    override this.Update(elapsedTime, bounds) =
        base.Update(elapsedTime, bounds)
        match List.length namestack with
        | 0 -> this.BeginClose()
        | n -> if (fst stack.[n - 1].Value).SelectedChild.IsNone then back()
    
    override this.OnClose() = ()

type ConfirmDialog(prompt, callback: unit -> unit) as this =
    inherit Dialog()

    let mutable confirm = false

    let options =
        row [ 
            LittleButton(
                K "Yes",
                fun () ->  this.BeginClose(); confirm <- true
            ).Position(Position.SliceLeft 200.0f)
            LittleButton(
                K "No", 
                this.BeginClose
            ).Position(Position.SliceRight 200.0f)
        ]

    do
        TextBox(K prompt, K (Color.White, Color.Black), 0.5f)
            .Position { Left = 0.0f %+ 200.0f; Top = 0.5f %- 200.0f; Right = 1.0f %- 200.0f; Bottom = 0.5f %- 50.0f }
        |> this.Add
        options.OnSelect()
        options.Position { Left = 0.5f %- 300.0f; Top = 0.5f %+ 0.0f; Right = 0.5f %+ 300.0f; Bottom = 0.5f %+ 100.0f }
        |> this.Add

    override this.Update(elapsedTime, bounds) =
        base.Update(elapsedTime, bounds)
        if not options.Selected then this.BeginClose()

    override this.OnClose() = if confirm then callback()