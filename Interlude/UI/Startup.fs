﻿namespace Interlude.UI

open Interlude
open Interlude.UI.Screens

module Startup =

    let init() =
        Screen.init [|MainMenu.LoadingScreen(); MainMenu.Screen(); Import.Screen(); LevelSelect.Screen()|]
        
        Score.Helpers.watchReplay <- fun (rate, data) -> Screen.changeNew (fun () -> Play.ReplayScreen(Play.ReplayMode.Replay (rate, data)) :> Screen.T) Screen.Type.Play Screen.TransitionFlag.Default
        Toolbar.TaskDisplay.init()
        Utils.AutoUpdate.checkForUpdates()
        Import.Mounts.handleStartupImports()

        Screen.Container(Toolbar.Toolbar())