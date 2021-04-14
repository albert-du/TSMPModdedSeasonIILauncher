// Learn more about F# at http://fsharp.org

open TSMPModdedSIILauncher.Core

[<EntryPoint>]
let main argv =
    
    let launcher = ConsoleLauncher()

    launcher.Initialize ()

    launcher.Start ()

    0 // return an integer exit code
