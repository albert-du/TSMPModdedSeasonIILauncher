namespace TSMPModdedSIILauncher

open System
open System.IO

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI

open TSMPModdedSIILauncher.Core
open TSMPModdedSIILauncher.Views

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Load "avares://Avalonia.Themes.Default/DefaultTheme.xaml"
        this.Styles.Load "avares://Avalonia.Themes.Default/Accents/BaseLight.xaml"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime -> desktopLifetime.MainWindow <- MainWindow()
        | _ -> ()

module Program =
    [<EntryPoint>]
    let main (args: string []) =
        if File.Exists (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TSMPModdedSeasonII", "CONSOLELAUNCHER")) then
            let consoleLauncher = new ConsoleLauncher()
            consoleLauncher.Initialize()
            consoleLauncher.Start()
            0
        else 
            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .StartWithClassicDesktopLifetime(args)