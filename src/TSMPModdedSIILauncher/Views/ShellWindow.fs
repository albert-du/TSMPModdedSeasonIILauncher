namespace TSMPModdedSIILauncher.Views

open Elmish
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Components.Hosts

[<AutoOpen>]
module private ShellWindow =
    open System
    open Avalonia
    open Avalonia.Controls
    open Avalonia.Controls.ApplicationLifetimes
    open Avalonia.Input
    open TSMPModdedSIILauncher
    open TerminalView

    type State =
      { terminalSource: TerminalSource
        reading : bool }

    type Msg =
        | Clear
        | ReadLine
        | Write of string * ConsoleColor option
        | WriteLine of string * ConsoleColor option
        | TextEntered of string
        | KeyInput of KeyEventArgs
        | TextInput of TextInputEventArgs

    let init () =
      { terminalSource = TerminalSource.empty
        reading = false}, "clear" |> Msg.TextEntered |> Cmd.ofMsg

    let update (msg: Msg) (state: State) (mLauncher: ModdedLauncher): State * Cmd<Msg> =
        match msg with 
        | Clear -> {state with terminalSource = TerminalSource.empty}, Cmd.none
        | ReadLine -> {state with reading = true}, Cmd.none
        | Write (text, color) -> {state with terminalSource = TerminalSource.writeColor text color state.terminalSource },Cmd.none
        | WriteLine (text, color) -> {state with terminalSource = TerminalSource.writeLineColor text color state.terminalSource },Cmd.none
        | KeyInput args -> 
            if state.reading then
                let (newSource, enteredText) = TerminalSource.handleKeyInput args state.terminalSource  
                {state with terminalSource = newSource},
                match enteredText with 
                | None -> Cmd.none 
                | Some text -> text |> TextEntered |> Cmd.ofMsg
            else state,Cmd.none

        | TextInput args -> {state with terminalSource = TerminalSource.handleTextInput args state.terminalSource; }, Cmd.none
        | TextEntered text -> 
            // handle text input
            match text with
            | "exit" -> 
                match Application.Current.ApplicationLifetime with
                | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime -> 
                    Seq.cast<Window> desktopLifetime.Windows
                    |> Seq.filter (fun i -> i.Title = "TSMP/M SII Shell v1.0" )
                    |> Seq.iter (fun i -> i.Close())
                | _ -> ()     
            | _ -> mLauncher.PublishShellReadlineCallback text 
            {state with reading = false}, (text, None) |> WriteLine |> Cmd.ofMsg

    let view (state: State) dispatch = 
        Terminal.create [
            Terminal.source state.terminalSource
            Terminal.readEnabled state.reading
        ]
            
    module Subs =
        let shellEvents (mLauncher: ModdedLauncher) _ =
            let sub dispatch =
                mLauncher.ShellEvent.Subscribe (
                    function
                    | ShellEventMsg.Clear -> (dispatch Clear)
                    | ShellEventMsg.ReadLine -> (dispatch ReadLine)
                    | ShellEventMsg.Write (text , color)->  (dispatch (Write (text, color)))
                    | ShellEventMsg.WriteLine (text , color)->  (dispatch (WriteLine (text, color)))
                )
                |> ignore
            Cmd.ofSub sub

        let keyHandler (window: HostWindow) _ = let sub dispatch = window.KeyDown.Add (KeyInput >> dispatch) in Cmd.ofSub sub

        let textHandler (window: HostWindow) _ = let sub dispatch = window.TextInput.Add (TextInput >> dispatch) in Cmd.ofSub sub

type ShellWindow(mLauncher) as this =
    inherit HostWindow()

    do
        base.Title <- "TSMP/M SII Shell v1.0"
        base.MinHeight <- 400.
        base.MinWidth <- 700.
        base.Height <- 400.
        base.Width <- 700.

        let update msg state =
            update msg state mLauncher

        Program.mkProgram init update view
        |> Program.withHost this
        |> Program.withSubscription (Subs.shellEvents mLauncher)
        |> Program.withSubscription (Subs.keyHandler this )
        |> Program.withSubscription (Subs.textHandler this )
        #if DEBUG 
        |> Program.withConsoleTrace 
        #endif
        |> Program.run