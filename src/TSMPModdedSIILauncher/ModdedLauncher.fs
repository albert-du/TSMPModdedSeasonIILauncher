namespace TSMPModdedSIILauncher

open System
open TSMPModdedSIILauncher.Core

type ShellEventMsg =
| Clear
| ReadLine
| Write of string * ConsoleColor option
| WriteLine of string * ConsoleColor option

type ModdedLauncher () =
    let gameOutputEvent =           Event<string> ()
    let outputEvent =               Event<string> ()
    let shellEvent =                Event<ShellEventMsg> ()
    let shellReadlineEvent =        Event<string> ()
    let statusBarEvent =            Event<string * Type * StatusType> ()
    let startButtonEnableEvent =    Event<bool> ()
    let confirmUpdateEvent =        Event<string * string> ()
    let confirmUpdateResultEvent =  Event<bool> ()

    member _.GameOutputEvent = gameOutputEvent.Publish
    member _.PublishGameOutput text = gameOutputEvent.Trigger text

    member _.OutputEvent = outputEvent.Publish
    member _.PublishOutput text = outputEvent.Trigger text

    member _.ShellEvent = shellEvent.Publish
    member _.PublishShell eventArgs = shellEvent.Trigger eventArgs

    member _.ShellReadlineCallback = shellReadlineEvent.Publish
    member _.PublishShellReadlineCallback text = shellReadlineEvent.Trigger text

    member _.StatusBarEvent = statusBarEvent.Publish
    member _.PublishStatusBarEvent (text:string) (sender:Type) (_type:StatusType) = statusBarEvent.Trigger (text, sender, _type)

    member _.StartButtonEnableEvent = startButtonEnableEvent.Publish
    member _.PublishStartButtonEnableEvent (enableStartButton:bool) = startButtonEnableEvent.Trigger enableStartButton

    member _.ConfirmUpdateEvent = confirmUpdateEvent.Publish
    member _.PublishConfirmUpdateEvent (installed: string, newVersion: string) = confirmUpdateEvent.Trigger (installed, newVersion)

    member _.ConfirmUpdateResultEvent = confirmUpdateResultEvent.Publish
    member _.PublishConfirmUpdateResultEvent (update: bool) = confirmUpdateResultEvent.Trigger update

    interface IModdedLauncher with
        member val Configuration = null with get, set
        member val Launcher = null with get, set
        member val ModpackService = null with get, set
        member val OptifineService = null with get, set
        member val Shell = null with get, set

        member this.ConfirmUpdate(installedVersion: string, newVersion: string): bool =
            this.PublishStatusBarEvent "Awaiting Update Confirmation" typeof<ModdedLauncher> StatusType.Initializing

            this.PublishConfirmUpdateEvent( installedVersion, newVersion)
            let j = 
                async {
                    let! result = Async.AwaitEvent(this.ConfirmUpdateResultEvent)
                    return result
                } |> Async.RunSynchronously 
            j 

        member this.Initialize(): unit =
            let moddedLauncher = this :> IModdedLauncher

            moddedLauncher.Configuration <- Configuration.LoadConfiguration()
            moddedLauncher.Launcher <- Launcher(this)
            moddedLauncher.ModpackService <- ModpackService(this)
            moddedLauncher.OptifineService <- OptifineService(this)
            moddedLauncher.Shell <- Shell(this)

            // start shell
            async {
                while true do
                    moddedLauncher.Shell.Start()
            }
            |> Async.Start

            // update status bar
            async {
                do! Async.Sleep(1000)
                moddedLauncher.SetStatusBar ("Loading Complete", typeof<ModdedLauncher>, StatusType.Ready)
            }
            |> Async.Start

        member this.LauncherDownloadChangeFile(e: CmlLib.Core.Downloader.DownloadFileChangedEventArgs): unit =
            (this :> IModdedLauncher).SetStatusBar ($"Downloading %O{e.FileKind}: %s{e.FileName} %d{e.ProgressedFileCount}/%d{e.TotalFileCount}", typeof<Launcher>, StatusType.Downloading )

        member this.LauncherDownloadChangeProgress(sender: obj, e: System.ComponentModel.ProgressChangedEventArgs): unit =
            (this :> IModdedLauncher).SetStatusBar ($"Downloading %%%d{e.ProgressPercentage}", typeof<Launcher>, StatusType.Downloading )

        member this.SetStatusBar(text: string, source: System.Type, statusType: StatusType): unit =
            this.PublishStatusBarEvent text source statusType

        member this.WriteLine(text: string, source: System.Type): unit =
            this.PublishOutput $"[%s{source.Name}] %s{text}"

        member this.GameOutputWriteLine(text: string): unit =
            this.PublishGameOutput text

        // Shell handlers
        member this.ShellClear(): unit =
            this.PublishShell ShellEventMsg.Clear

        member this.ShellReadLine(): string =
            this.PublishShell ShellEventMsg.ReadLine
            async {
                let! text = Async.AwaitEvent(this.ShellReadlineCallback)
                return text
            }
            |> Async.RunSynchronously

        member this.ShellWrite(text: string): unit =
            this.PublishShell (ShellEventMsg.Write (text, None))

        member this.ShellWrite(text: string, consoleColor: ConsoleColor): unit =
            this.PublishShell (ShellEventMsg.Write (text, Some consoleColor))

        member this.ShellWriteLine(text: string): unit =
            this.PublishShell (ShellEventMsg.WriteLine (text, None))

        member this.ShellWriteLine(text: string, consoleColor: ConsoleColor): unit =
            this.PublishShell (ShellEventMsg.WriteLine (text, Some consoleColor))