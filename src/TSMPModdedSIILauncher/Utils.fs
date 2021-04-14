namespace TSMPModdedSIILauncher

open System.IO
open System.Net.Http
open System
open System.Diagnostics
open System.Runtime.InteropServices
open TSMPModdedSIILauncher.Core

module internal Utils =

    let private httpClient = new HttpClient()

    // Colors from https://dev.to/anupa/beautify-your-windows-terminal-1la8
    let consoleColor = function
        | ConsoleColor.Black ->         "#101116"
        | ConsoleColor.DarkRed ->       "#ff5680"
        | ConsoleColor.DarkGreen ->     "#00ff9c"
        | ConsoleColor.DarkYellow ->    "#fffc58"
        | ConsoleColor.DarkBlue ->      "#00b0ff"
        | ConsoleColor.DarkMagenta ->   "#d57bff"
        | ConsoleColor.DarkCyan ->      "#76c1ff"
        | ConsoleColor.DarkGray ->      "#c7c7c7"
        | ConsoleColor.Gray ->          "#686868"
        | ConsoleColor.Red ->           "#ff6e67"
        | ConsoleColor.Green ->         "#5ffa68"
        | ConsoleColor.Yellow ->        "#fffc67"
        | ConsoleColor.Blue ->          "#6871ff"
        | ConsoleColor.Magenta ->       "#d682ec"
        | ConsoleColor.Cyan ->          "#60fdff"
        | ConsoleColor.White ->         "#ffffff"
        | _ -> failwithf "Could not resolve color"

    let consoleBackgroundColor = "#1d2342"
    let consoleForegroundColor = "#b8ffe1"

    let statusToColor status =
        match status with
        | StatusType.Ready ->       "#7b68ee"
        | StatusType.Error ->       "#ff5050"
        | StatusType.Warning ->     "#ff8533"
        | StatusType.Downloading -> "#ff3399"
        | StatusType.Installing ->  "#bf66ff"
        | StatusType.Initializing ->"#20da9a"
        | StatusType.Launching ->   "#ca96e3"
        | _ ->                      "#7b68ee"

    let openURL url =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            Process.Start(ProcessStartInfo("cmd", sprintf $"/c start %s{url}" ) ) |> ignore
        else if RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            Process.Start("xdg-open", url) |> ignore
        else if RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            Process.Start("open", url) |> ignore

    let mcSkinStreamAsync uuid =
        async {
            let ms = new MemoryStream ()
            let! stream = httpClient.GetStreamAsync($"https://crafatar.com/avatars/%s{uuid}?size=100") |> Async.AwaitTask
            stream.CopyTo(ms)
            return ms
        }