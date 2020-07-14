﻿namespace Interlude.UI

open Prelude.Common
open Prelude.Data.Profiles
open Interlude.Options
open Interlude.UI.Components
open FSharp.Reflection

type ConfigEditor<'T>(data: 'T) as this =
    inherit FlowContainer()

    do
        let typeName = data.GetType().Name
        let fields = FSharpType.GetRecordFields(data.GetType())
        Array.choose (
            fun p ->
                let value = FSharpValue.GetRecordField(data, p)
                match value with
                | :? FloatSetting as s -> new Slider<float>(s, Localisation.localise(typeName + ".name." + p.Name)) |> Components.positionWidget(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 80.0f, 0.0f) |> Some
                | :? IntSetting as s -> new Slider<int>(s, Localisation.localise(typeName + ".name." + p.Name)) |> Components.positionWidget(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 80.0f, 0.0f) |> Some
                | :? Setting<bool> as s -> Selector.FromBool(s, Localisation.localise(typeName + ".name." + p.Name)) |> Components.positionWidget(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 80.0f, 0.0f) |> Some
                //| :? Setting<'S> as s -> Selector.FromEnum(s, localise(typeName + ".name." + p.Name)) |> Components.positionWidget(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 80.0f, 0.0f) |> Some
                | _ -> None
            ) fields
        |> Array.iter this.Add
        this.Reposition(100.0f, 40.0f, -100.0f, -40.0f)

type ScreenOptions() as this =
    inherit Screen()

    do  
        this.Add(new ConfigEditor<Profile>(Options.profile))