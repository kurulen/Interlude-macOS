﻿namespace Interlude.UI.OptionsMenu

open Prelude.Common
open Prelude.Data.Charts
open Interlude.Utils
open Interlude.Options
open Interlude.UI
open Interlude.UI.Components.Selection.Menu

module Debug =

    type DebugPage(m) as this =
        inherit Page(m)

        do
            this |*
                page_content m [
                    PrettyButton.Once(
                        "debug.rebuildcache",
                        (fun () -> BackgroundTask.Create TaskFlags.LONGRUNNING "Rebuilding Cache" Library.rebuildTask |> ignore),
                        Localisation.localiseWith ["Rebuilding Cache"] "notification.taskstarted", NotificationType.Task
                    ).Pos(200.0f)
                    PrettyButton.Once(
                        "debug.downloadupdate",
                        ( fun () ->
                            if AutoUpdate.updateAvailable then
                                AutoUpdate.applyUpdate(fun () -> Notification.add (L"notification.update.installed", NotificationType.System))
                        ),
                        L"notification.update.installing", NotificationType.System,
                        Enabled = AutoUpdate.updateAvailable
                    ).Pos(300.0f)
                    PrettySetting("debug.enableconsole", Percyqaz.Flux.UI.Selector<_>.FromBool options.EnableConsole).Pos(400.0f)
                ]

        override this.Title = N"debug"
        override this.OnClose() = ()