﻿namespace Interlude.Features.Multiplayer

open Percyqaz.Common
open Percyqaz.Flux.UI
open Prelude.Common
open Interlude.Utils
open Interlude.UI
open Interlude.UI.Menu
open Interlude.UI.Components
open Interlude.Web.Shared
open Interlude.Features.Online

type CreateLobbyPage() as this =
    inherit Page()
    

    let value = Setting.simple (Network.credentials.Username + "'s Lobby")
    let submit() = Lobby.create value.Value
    let submit_button = PageButton("confirm.yes", (fun () -> submit(); Menu.Back()))
    
    do
        this.Content(
            column()
            |+ PageSetting("create_lobby.name", TextEntry(value |> Setting.trigger (fun s -> submit_button.Enabled <- s.Length > 0), "none")).Pos(200.0f)
            |+ submit_button.Pos(300.0f)
        )
    
    override this.Title = L"create_lobby.name"
    override this.OnClose() = ()

type InviteCard(sender: string, lobby: System.Guid) =
    inherit Frame(NodeType.None)

    override this.Init(parent) =
        this
        |+ Text(Icons.invite + " " + sender, Position = Position.Margin(5.0f), Align = Alignment.LEFT)
        |+ Button(
            Icons.ready,
            (fun () -> sync(fun () -> (this.Parent :?> FlowContainer.Vertical<InviteCard>).Remove this); Lobby.join lobby),
            Position = Position.TrimRight(50.0f).SliceRight(50.0f)
        )
        |* Button(
            Icons.not_ready,
            (fun () -> sync(fun () -> (this.Parent :?> FlowContainer.Vertical<InviteCard>).Remove this)),
            Position = Position.SliceRight(50.0f)
        )
        base.Init parent

type InviteList() =
    inherit StaticContainer(NodeType.None)

    let container = FlowContainer.Vertical<InviteCard>(50.0f, Spacing = 10.0f, Position = Position.Margin (0.0f, 80.0f))
    
    do 
        Network.Events.receive_invite.Add (fun (name, id) -> 
            container.Add(InviteCard(name, id))
            //if 
            //    match Screen.currentType with
            //    | Screen.Type.LevelSelect
            //    | Screen.Type.Import
            //    | Screen.Type.MainMenu -> true
            //    | _ -> false
            //then
            //    Notifications.add(Localisation.localiseWith [name] "notification.invited_to_lobby.title", NotificationType.Info)
        )

    override this.Init(parent) =
        this |* container
        base.Init parent
        
type LobbyInfoCard(info: LobbyInfo) =
    inherit Frame(NodeType.None)
        
    override this.Init(parent) =
        this
        |+ Text(info.Name, Position = Position.SliceTop(50.0f).Margin(5.0f), Align = Alignment.LEFT)
        |+ Text((match info.CurrentlyPlaying with None -> L"lobby.no_song_selected" | Some s -> s), Color = K Colors.text_subheading, Position = Position.SliceBottom(40.0f).Margin(5.0f), Align = Alignment.LEFT)
        |+ Clickable(fun () -> Lobby.join info.Id)
        |* Text(info.Players.ToString() + " " + Icons.multiplayer, Color = K Colors.text_subheading, Position = Position.SliceTop(50.0f).Margin(5.0f), Align = Alignment.RIGHT)
        base.Init parent
        
    member this.Name = info.Name

type LobbyList() =
    inherit StaticContainer(NodeType.None)

    let searchtext = Setting.simple ""

    let container = FlowContainer.Vertical<LobbyInfoCard>(80.0f, Spacing = 10.0f, Position = Position.Margin (0.0f, 80.0f))
    let mutable no_lobbies = false

    let refresh() =
        container.Clear()
        no_lobbies <- Network.lobby_list.Length = 0
        for l in Network.lobby_list do
            container.Add(LobbyInfoCard l)

    let mutable lobby_creating = false
    let create_lobby() =
        if lobby_creating then () else
        lobby_creating <- true
        Menu.ShowPage CreateLobbyPage

    override this.Init(parent) =
        this
        |+ container
        |+ Conditional((fun () -> no_lobbies), EmptyState(Icons.multiplayer, L"lobby_list.none", Subtitle = L"lobby_list.none.subtitle"))
        |+ IconButton(L"lobby_list.create", Icons.add, 60.0f, create_lobby, Position = Position.SliceBottom(60.0f).TrimRight(250.0f))
        |+ IconButton(L"lobby_list.refresh", Icons.reset, 60.0f, Lobby.refresh_list, Position = Position.SliceBottom(60.0f).SliceRight(250.0f))
        |* SearchBox(searchtext, (fun () -> container.Filter <- fun l -> l.Name.ToLower().Contains searchtext.Value), Position = Position.SliceTop 60.0f)
        
        base.Init parent

        refresh()
        Network.Events.receive_lobby_list.Add refresh
        Network.Events.join_lobby.Add (fun () -> lobby_creating <- false)