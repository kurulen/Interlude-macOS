﻿namespace Interlude.Features.OptionsMenu.Themes

open Percyqaz.Common
open Prelude.Common
open Prelude.Data.Content
open Interlude.Content
open Interlude.UI.Menu
open Interlude.UI.Components

type EditThemePage() as this =
    inherit Page()

    let data = Themes.Current.config
        
    let name = Setting.simple data.Name

    do
        this.Content(
            column()
            |+ PageSetting("themes.edittheme.themename", TextEntry(name, "none"))
                .Pos(200.0f)
        )

    override this.Title = data.Name
    override this.OnClose() =
        Themes.Current.changeConfig
            { data with
                Name = name.Value
            }