namespace TSMPModdedSIILauncher.Views

open Elmish
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Components.Hosts

[<AutoOpen>]
module private GameOutputWindow =
    open TSMPModdedSIILauncher
    open TerminalView

    type State = { source: TerminalSource }

    let init () = { source = TerminalSource.empty }, Cmd.none

    type Msg = | Writeline of string

    let update (msg: Msg) (state: State): State * Cmd<Msg> =
        match msg with
        | Writeline text -> {state with source = TerminalSource.writeLine text state.source }, Cmd.none

    let view (state:State) _ =
        Terminal.create [ Terminal.source state.source ]

    module Subs =
        let WriteLine (mLauncher: ModdedLauncher) _ =
            let sub dispatch =
                mLauncher.GameOutputEvent.Subscribe (Writeline >> dispatch)
                |> ignore
            Cmd.ofSub sub

type GameOutputWindow(mLauncher) as this =
    inherit HostWindow()

    do
        base.Title <- "Game Output"
        base.MinHeight <- 400.
        base.MinWidth <- 700.
        base.Height <- 400.
        base.Width <- 700.

        Program.mkProgram init update view
        |> Program.withHost this
        |> Program.withSubscription (Subs.WriteLine mLauncher)
        |> Program.run