// Main Launcher Window
namespace TSMPModdedSIILauncher.Views

open System

open Elmish
open Avalonia
open Avalonia.Input
open Avalonia.Controls
open Avalonia.Controls.Presenters
open Avalonia.Threading
open Avalonia.Media
open Avalonia.Media.Immutable

open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Components.Hosts

open TSMPModdedSIILauncher
open TSMPModdedSIILauncher.Views
open TSMPModdedSIILauncher.Core

// Other Types of Windows
type WindowKind =
    | Output
    | GameOutput
    | Shell

type IWindowService =
    abstract Open : WindowKind -> Async<unit>

module MainWindow =       
    open Avalonia.Layout
    open Avalonia.Media.Imaging
    open Avalonia.Styling
    open Avalonia.Platform
    open CmlLib.Core.Auth

    // Update Messages
    type Msg =
        // Subview messages
        | LoginMsg of Login.Msg
        | SettingsMsg of Settings.Msg
        | Logout
        | OpenSettings
        | CloseSettings
        
        // Window open messages
        | OpenOutput
        | OpenGameOutput
        | OpenShell
        | WindowOpen of WindowKind // Window opened callback

        | OpenURL of string
        | SetStatusBar of string * Type * StatusType
        | StartGameEnabled of bool
        | UpdateAvatar of Bitmap
        | ConfirmUpdate of string * string
        | ConfirmUpdateResponse of bool  
        | StartGame
        | Initialize

    // Model 
    type State =
      { loginState: Login.State
        settingsState: Settings.State
        initialized: bool
        statusBarText: string
        statusBarColor: string
        loggedIn: bool
        launchEnabled: bool
        mcSkin: Bitmap
        session: MSession option
        updateMessage: (string * string) option
        updateMessage2: string
        settingsOpen: bool }

    let init () =
        // Subview states
        let loginState, loginCmd = Login.init ()
        let settingsState, settingsCmd = Settings.init ()

        { loginState = loginState
          settingsState = settingsState
          initialized = false
          statusBarText = "Loading"
          statusBarColor = Utils.statusToColor StatusType.Initializing
          loggedIn = false
          launchEnabled = false 
          mcSkin = new Bitmap ( AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri ("avares://TSMPModdedSIILauncher/Assets/defaultProfile.png") ) )
          session =  None
          updateMessage = None
          updateMessage2 = "No updates available"
          settingsOpen = false}, 
        // Initial Commands
        Cmd.batch [ 
            Cmd.map LoginMsg loginCmd;
            Cmd.map SettingsMsg settingsCmd;
            Cmd.ofMsg Initialize
        ] 

    let update (msg: Msg) (state: State) (windowService: IWindowService) (mLauncher: ModdedLauncher) : State * Cmd<Msg> =
        let mLauncher' = mLauncher :> IModdedLauncher

        match msg with
        // Subview msgs
        | LoginMsg lMsg ->
            let loginState, cmd = 
               Login.update lMsg state.loginState
            match lMsg with 
            | Login.Msg.LoginSuccess session -> 
                {state with session = Some session; loginState = loginState}, 
                    if session.UUID <> "user_uuid" then Cmd.OfAsync.perform Utils.mcSkinStreamAsync session.UUID (fun s -> s.Position <- 0L ; new Bitmap (s) |> UpdateAvatar) else Cmd.none
            | _ -> 
                { state with loginState = loginState}, Cmd.map LoginMsg cmd

        | SettingsMsg sMsg -> 
            let settingsState', cmd = Settings.update sMsg state.settingsState mLauncher'.Configuration
            {state with settingsState = settingsState' }, Cmd.map SettingsMsg cmd; 
        // Open Windows
        | OpenOutput ->     state, Cmd.OfAsync.perform windowService.Open Output     (fun _ -> WindowOpen Output)
        | OpenGameOutput -> state, Cmd.OfAsync.perform windowService.Open GameOutput (fun _ -> WindowOpen GameOutput)
        | OpenShell ->      state, Cmd.OfAsync.perform windowService.Open Shell      (fun _ -> WindowOpen Shell)
        | OpenURL url -> Utils.openURL url; state, Cmd.none
        | WindowOpen window ->
            let type' = 
                match window with 
                | Output -> typeof<OutputWindow>
                | GameOutput -> typeof<GameOutputWindow>
                | Shell -> typeof<ShellWindow>

            mLauncher'.WriteLine ("Window opened",type')
            state, Cmd.none

        | SetStatusBar (text, sender, statusType) ->
            let color = Utils.statusToColor statusType
            let statusName = Enum.GetName(typeof<StatusType>, statusType)
            {state with statusBarColor = color; statusBarText = $"%-20s{statusName} %-25s{sender.Name} %s{text}" }, Cmd.none

        | StartGameEnabled enableButton -> {state with launchEnabled = enableButton}, Cmd.none
        
        | UpdateAvatar bitmap -> {state with mcSkin = bitmap}, Cmd.none

        | Logout -> 
            mLauncher'.Shell.Execute("session clear") |> ignore
            {state with session = None; mcSkin = new Bitmap ( AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri ("avares://TSMPModdedSIILauncher/Assets/defaultProfile.png") ) )}, Cmd.none

        | ConfirmUpdate (oldVersion,newVersion) -> { state with updateMessage = Some (oldVersion, newVersion)}, Cmd.none

        | ConfirmUpdateResponse update -> 
            mLauncher.PublishConfirmUpdateResultEvent update
            {state with updateMessage = None; updateMessage2 = (if update then "No updates available " else "Update skipped ") }, Cmd.ofMsg (SetStatusBar ((if update then "Preparing to update" else "Update skipped"), typeof<ModdedLauncher>, StatusType.Ready))

        | OpenSettings -> {state with settingsOpen = true}, Cmd.map SettingsMsg (Cmd.ofMsg Settings.LoadConfig)

        | CloseSettings -> 
            {state with settingsOpen = false; settingsState = {state.settingsState with requiresReinitialization = false} }, 
                if state.settingsState.requiresReinitialization then Cmd.ofMsg Initialize else Cmd.none

        | StartGame -> 
            match state.initialized with 
            | true -> 
                async {
                    mLauncher'.Launcher.LaunchGame state.session.Value
                } |> Async.Start
                let workflow (dispatch: Msg -> unit): unit = 
                    async {
                        do! Async.Sleep 30000
                        dispatch (StartGameEnabled true)
                    } 
                    |> Async.Start
                {state with launchEnabled = false}, Cmd.batch [ Cmd.ofSub workflow; Cmd.ofMsg OpenGameOutput ] 
            | false -> state, Cmd.none

        | Initialize -> 
            mLauncher.PublishStatusBarEvent "Initializing" typeof<ModdedLauncher> StatusType.Initializing
            let workflow () = 
                async {
                    do! Async.Sleep 2000
                    let! requiresDownload = mLauncher'.ModpackService.NeedsDownloadAsync() |> Async.AwaitTask
                    if requiresDownload then
                        do! mLauncher'.ModpackService.DownloadAsync() |> Async.AwaitTask 
                        do! mLauncher'.ModpackService.InstallAsync() |> Async.AwaitTask
                    do! Async.Sleep 2000
                    if (not <| mLauncher'.OptifineService.OptifineInstalled() && mLauncher'.Configuration.Optifine) then
                        mLauncher'.WriteLine("Installing Optifine",typeof<ModdedLauncher>)
                        mLauncher'.OptifineService.InstallOptifine();
                    else if (mLauncher'.OptifineService.OptifineInstalled() && not <|mLauncher'.Configuration.Optifine) then
                        mLauncher'.WriteLine("Removing Optifine",typeof<ModdedLauncher>)
                        mLauncher'.OptifineService.RemoveOptifine();
                    return StartGameEnabled true
                }
            
            {state with initialized = true}, Cmd.batch[ Cmd.OfAsync.perform workflow () id; Cmd.ofMsg (StartGameEnabled false)]
            
    [<AutoOpen>]
    module private Subviews =
        // creates a semitransparent canvas with a callback
        let darkenOverlay (onTapped: unit -> unit) =
            Canvas.create [
                Canvas.background (ImmutableSolidColorBrush("#000000"|> Color.Parse , 0.4))
                Canvas.horizontalAlignment HorizontalAlignment.Stretch
                Canvas.verticalAlignment VerticalAlignment.Stretch
                Canvas.onTapped (fun _ -> onTapped ())
            ]

        // contains the title 'Titanic SMP Modded Season II'
        let topView = 
            StackPanel.create [
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.dock Dock.Top
                StackPanel.children [                    
                    TextBlock.create [
                        TextBlock.text "Titanic SMP Modded"
                        TextBlock.fontSize 35.
                        TextBlock.foreground "#1d2342"
                        TextBlock.fontWeight FontWeight.DemiBold
                    ]
                    TextBlock.create [
                        TextBlock.text "                                     Season II"
                        TextBlock.fontSize 25.
                        TextBlock.fontStyle FontStyle.Italic
                        TextBlock.foreground "mediumslateblue"
                    ]
    
                ]
            ]
        // The bottom status bar
        let statusBar (state: State) dispatch =
            DockPanel.create [
                DockPanel.maxHeight 40.
                DockPanel.minHeight 20.
                DockPanel.verticalAlignment VerticalAlignment.Bottom
                DockPanel.horizontalAlignment HorizontalAlignment.Stretch
                DockPanel.dock Dock.Bottom
                DockPanel.background state.statusBarColor
                DockPanel.children [
                    TextBlock.create [
                        TextBlock.dock Dock.Left
                        TextBlock.margin (10., 5.)
                        TextBlock.fontSize 15.
                        TextBlock.textAlignment TextAlignment.Left
                        TextBlock.text state.statusBarText
                        TextBlock.focusable true
                        TextBlock.foreground "white"
                    ]
                    TextBlock.create [
                        TextBlock.dock Dock.Right
                        TextBlock.margin (10., 5.)
                        TextBlock.fontSize 14.
                        TextBlock.textDecorations TextDecorations.Underline 
                        TextBlock.textAlignment TextAlignment.Right
                        TextBlock.text "Shell"
                        TextBlock.focusable true
                        TextBlock.foreground "white"
                        TextBlock.onTapped (fun _ -> OpenShell |> dispatch)
                    ]
                ]
            ]
        // creates a hyperlink style button with a url
        let private linkButton (text: string) link = 
            TextBlock.create [
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text text
                TextBlock.foreground "deepskyblue"
                TextBlock.height 30.0
                TextBlock.onTapped (fun _ -> Utils.openURL link)
                TextBlock.textDecorations TextDecorations.Underline
                TextBlock.fontStyle FontStyle.Oblique
                TextBlock.fontSize 15.
                TextBlock.maxLines 2
            ]

        // right side bar
        let rightBar (session:MSession) (state: State) dispatch = 
            // Login info
            DockPanel.create [
                DockPanel.width 150.
                DockPanel.dock Dock.Right
                DockPanel.children [
                    StackPanel.create [
                        StackPanel.dock Dock.Top
                        StackPanel.margin 20.
                        StackPanel.orientation Orientation.Vertical
                        StackPanel.spacing 5.
                        StackPanel.verticalAlignment VerticalAlignment.Bottom
                        StackPanel.horizontalAlignment HorizontalAlignment.Right
                        StackPanel.children [
                            Image.create[
                                Image.source state.mcSkin
                                Image.width 100.
                                Image.height 100.
                            ]
                            TextBlock.create [
                                TextBlock.text session.Username
                                TextBlock.fontSize 15.
                                TextBlock.horizontalAlignment HorizontalAlignment.Center
                                TextBlock.fontWeight FontWeight.Medium
                                TextBlock.margin (0., 10., 0., 0.)
                            ]
                            Button.create [
                                Button.content "Logout"
                                Button.fontSize 10.
                                Button.height 20.
                                Button.width 40.
                                Button.padding 0.
                                Button.horizontalAlignment HorizontalAlignment.Right
                                Button.verticalAlignment VerticalAlignment.Top
                                Button.fontWeight FontWeight.Normal
                                Button.foreground "white"
                                Button.background "DeepPink"
                                Button.styles (
                                    let styles = Styles()
                                    let style = Style(fun x -> x.OfType<Button>().Template().OfType<ContentPresenter>())
    
                                    let setter = Setter(ContentPresenter.CornerRadiusProperty, CornerRadius(10.))
                                    style.Setters.Add setter
     
                                    styles.Add style
                                    styles
                                )
                                Button.onClick (fun _ -> Logout |> dispatch)
                            ]
                        ]
                    ]
                    StackPanel.create [
                        StackPanel.dock Dock.Bottom
                        StackPanel.margin 20.
                        StackPanel.orientation Orientation.Vertical
                        StackPanel.spacing 15.
                        StackPanel.verticalAlignment VerticalAlignment.Bottom
                        StackPanel.horizontalAlignment HorizontalAlignment.Center
                        StackPanel.children [
                            TextBlock.create [
                                TextBlock.text "Links"
                                TextBlock.fontSize 20.
                                TextBlock.fontWeight FontWeight.Medium
                                TextBlock.foreground "DarkSlateBlue"
                                TextBlock.horizontalAlignment HorizontalAlignment.Center
                            ]
                            linkButton "Modpack " "https://github.com/DabbingEevee/TSMP_Modded_Season_II"
                            linkButton "Mod List " "https://github.com/DabbingEevee/TSMP_Modded_Season_II/blob/master/ModList.md"
                            linkButton "Minecraft " "https://www.minecraft.net/en-us/"
                        ]
                    ]

                ]
            ]
        // left side bar
        let leftBar mLauncher (state:State) dispatch = 
            let mLauncher' = mLauncher :> IModdedLauncher
            DockPanel.create [
                DockPanel.horizontalAlignment HorizontalAlignment.Left
                DockPanel.verticalAlignment VerticalAlignment.Stretch
                DockPanel.width 150.
                DockPanel.dock Dock.Left
                DockPanel.children [
                    Border.create [
                        Border.dock Dock.Top
                        Border.width 140.
                        Border.minHeight 400.
                        Border.background "lightgray"
                        Border.borderThickness 2.
                        Border.cornerRadius 3.
                        Border.padding 4.
                        Border.margin (5.,5.,5.,20.)
                        Border.verticalAlignment VerticalAlignment.Stretch
                        Border.child (
                            DockPanel.create [
                                DockPanel.children [
                                    StackPanel.create [
                                        StackPanel.dock Dock.Top
                                        StackPanel.children [
                                            TextBlock.create [
                                                TextBlock.text "Updates"
                                                TextBlock.fontSize 20.
                                                TextBlock.fontWeight FontWeight.Medium
                                                TextBlock.foreground "DarkSlateBlue"
                                                TextBlock.horizontalAlignment HorizontalAlignment.Center
                                                TextBlock.verticalAlignment VerticalAlignment.Stretch
                                            ]
                                            match state.updateMessage with 
                                            | Some (vOld, vNew) -> 
                                                StackPanel.create [
                                                    StackPanel.children [
                                                        TextBlock.create [
                                                            TextBlock.horizontalAlignment HorizontalAlignment.Left
                                                            TextBlock.text "Installed:"
                                                            TextBlock.fontSize 18.
                                                        ]
                                                        TextBlock.create [
                                                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                                                            TextBlock.text (if vOld.Contains('|') then let lines = vOld.Split('|') in $"%s{lines.[0]}\n%s{lines.[1]}" else vOld)
                                                            TextBlock.maxLines 2
                                                        ]
                                                        TextBlock.create [
                                                            TextBlock.horizontalAlignment HorizontalAlignment.Left
                                                            TextBlock.text "Update:"
                                                            TextBlock.fontSize 18.
                                                        ]
                                                        TextBlock.create [
                                                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                                                            TextBlock.text (if vNew.Contains('|') then let lines = vNew.Split('|') in $"%s{lines.[0]}\n%s{lines.[1]}" else vNew)
                                                        ]
                                                        StackPanel.create [
                                                            StackPanel.margin 10.
                                                            StackPanel.children [
                                                                Button.create [
                                                                    Button.content "Update"
                                                                    Button.background "coral"
                                                                    Button.foreground "white"
                                                                    Button.onClick ( fun _ -> true |> ConfirmUpdateResponse |> dispatch)
                                                                    Button.styles (
                                                                        let styles = Styles()
                                                                        let style = Style(fun x -> x.OfType<Button>().Template().OfType<ContentPresenter>())
    
                                                                        let setter = Setter(ContentPresenter.CornerRadiusProperty, CornerRadius(15.0))
                                                                        style.Setters.Add setter
     
                                                                        styles.Add style
                                                                        styles
                                                                    )
                                                                ]
                                                                TextBlock.create [
                                                                    TextBlock.horizontalAlignment HorizontalAlignment.Right
                                                                    TextBlock.text "Skip  "
                                                                    TextBlock.textDecorations TextDecorations.Underline
                                                                    TextBlock.fontStyle FontStyle.Oblique
                                                                    TextBlock.onTapped ( fun _ -> false |> ConfirmUpdateResponse |> dispatch)
                                                                ]       
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            | None -> 
                                                StackPanel.create [
                                                    StackPanel.children [
                                                        TextBlock.create [
                                                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                                                            TextBlock.text state.updateMessage2
                                                            TextBlock.fontStyle FontStyle.Oblique
                                                        ]
                                                    ]
                                                ]
                                        ]
                                    ]
                                    TextBlock.create [
                                        TextBlock.dock Dock.Bottom
                                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                                        TextBlock.verticalAlignment VerticalAlignment.Bottom
                                        TextBlock.text (sprintf "Using %s" (if mLauncher'.Configuration.LiveUpdates then "Live Update" else "Release"))
                                    ]
                                ]
                            ]                        
                        )
                    ]
                    Button.create [
                        Button.dock Dock.Bottom
                        Button.width 130.
                        Button.height 30.
                        Button.horizontalAlignment HorizontalAlignment.Left
                        Button.verticalAlignment VerticalAlignment.Bottom
                        Button.margin 5.
                        Button.foreground "white"
                        Button.background "#808080"
                        Button.styles (
                            let styles = Styles()
                            let style = Style(fun x -> x.OfType<Button>().Template().OfType<ContentPresenter>())
    
                            let setter = Setter(ContentPresenter.CornerRadiusProperty, CornerRadius(25.0))
                            style.Setters.Add setter
     
                            styles.Add style
                            styles
                        )
                        Button.content "Settings"
                        Button.fontSize 15.
                        Button.onClick(fun _ -> OpenSettings |> dispatch)
                    ]
                ]                
            ]
        let startButton state dispatch = 
            Button.create [
                Button.isEnabled state.launchEnabled
                Button.onClick (fun _ -> StartGame |> dispatch)
                Button.content "Play"
                Button.fontWeight FontWeight.Bold
                Button.foreground "white"
                Button.background "limegreen"
                Button.fontSize 30.
                Button.height 50.
                Button.width 200.
                Button.styles (
                    let styles = Styles()
                    let style = Style(fun x -> x.OfType<Button>().Template().OfType<ContentPresenter>())

                    let setter = Setter(ContentPresenter.CornerRadiusProperty, CornerRadius(100.))
                    style.Setters.Add setter

                    styles.Add style
                    styles
                )
                Button.verticalAlignment VerticalAlignment.Bottom
                Button.margin 20.
            ]

    // main view
    let view mLauncher (state: State) (dispatch) =
        match state.session with 
        | Some session ->
            Grid.create[
                Grid.columnDefinitions "*"
                Grid.rowDefinitions "*"
                Grid.children[
                    DockPanel.create [
                        DockPanel.children [
                            statusBar state dispatch
                            rightBar session state dispatch
                            leftBar mLauncher state dispatch
                            topView
                            startButton state dispatch
                        ]
                    ]
                    if state.settingsOpen then 
                        darkenOverlay (fun _ -> CloseSettings |> dispatch)
                        Settings.view state.settingsState (SettingsMsg >> dispatch)
                ]
            ]
            |> generalize
        | None -> 
            Login.view state.loginState (LoginMsg >> dispatch) // Login view
            |> generalize


    module Subs =
        let statusBarSent (mLauncher: ModdedLauncher) _ =
            let sub dispatch =
                mLauncher.StatusBarEvent.Subscribe(SetStatusBar >> dispatch)
                |> ignore

            Cmd.ofSub sub

        let startButtonEnabled (mLauncher: ModdedLauncher) _ =
            let sub dispatch =
                mLauncher.StartButtonEnableEvent.Subscribe(StartGameEnabled >> dispatch)
                |> ignore

            Cmd.ofSub sub

        let confirmUpdate (mLauncher: ModdedLauncher) _ =
            let sub dispatch = 
                mLauncher.ConfirmUpdateEvent.Subscribe(ConfirmUpdate >> dispatch) 
                |> ignore
            Cmd.ofSub sub
                
        let openOutput (window: HostWindow) _ =
            let sub dispatch =
                window.KeyDown.Add(fun i -> 
                    match i.Key with 
                    | Key.O when i.KeyModifiers.HasFlag KeyModifiers.Control -> OpenOutput |> dispatch
                    | Key.S -> OpenShell |> dispatch
                    | _ -> () )
            Cmd.ofSub sub

type MainWindow() as this =
    inherit HostWindow()
    
    let mLauncher = ModdedLauncher()
    
    do
        base.Title <- "Titanic SMP Modded Season II"
        base.Width <- 800.
        base.MinWidth <- 800.
        base.Height <- 500.
        base.MinHeight <- 500.

        (mLauncher :> IModdedLauncher ).Initialize ()

        let update state msg =
            MainWindow.update state msg (this :> IWindowService) mLauncher

        let view state msg =
            MainWindow.view mLauncher state msg

        Program.mkProgram MainWindow.init update view
        |> Program.withHost this
        |> Program.withSubscription (MainWindow.Subs.statusBarSent mLauncher)
        |> Program.withSubscription (MainWindow.Subs.startButtonEnabled mLauncher)
        |> Program.withSubscription (MainWindow.Subs.confirmUpdate mLauncher)
        |> Program.withSubscription (MainWindow.Subs.openOutput this)
#if DEBUG 
        |> Program.withConsoleTrace
#endif
        |> Program.run

    interface IWindowService with
        member _.Open(kind: WindowKind): Async<unit> =
            match kind with
            | Output ->
                Dispatcher.UIThread.InvokeAsync(fun _ ->
                    let window = OutputWindow(mLauncher)
                    window.Background <- (Utils.consoleBackgroundColor |> Color.Parse |> ImmutableSolidColorBrush)
                    window.Show ())
            | GameOutput ->
                Dispatcher.UIThread.InvokeAsync(fun _ ->
                    let window = GameOutputWindow(mLauncher)
                    window.Background <- (Utils.consoleBackgroundColor |> Color.Parse |> ImmutableSolidColorBrush)
                    window.Show ())
            | Shell ->
                Dispatcher.UIThread.InvokeAsync(fun _ ->
                    let window = ShellWindow(mLauncher)
                    window.Background <- (Utils.consoleBackgroundColor |> Color.Parse |> ImmutableSolidColorBrush)
                    window.Show ())
            |> Async.AwaitTask