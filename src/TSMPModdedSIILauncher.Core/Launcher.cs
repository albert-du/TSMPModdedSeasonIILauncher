using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Downloader;
using CmlLib.Core.Version;

namespace TSMPModdedSIILauncher.Core
{
    public class Launcher
    {

        /// <summary>
        /// Ensure the directory exists and if it doesn't, create it
        /// </summary>
        /// <param name="path">path to check and possibly create</param>
        /// <returns>path</returns>
        public static string Dir(string path)
        {
            var p = Path.GetFullPath(path);
            if (!Directory.Exists(p))
                Directory.CreateDirectory(p);

            return p;
        }
        private IModdedLauncher moddedLauncher;
        private void WriteLine(string text) => moddedLauncher.WriteLine(text, GetType()); 


        private Configuration configuration;

        public Launcher(IModdedLauncher moddedLauncher)
        {
            this.moddedLauncher = moddedLauncher;
            this.configuration = moddedLauncher.Configuration;

        }
        
        /// <summary>
        /// Launch minecraft
        /// </summary>
        /// <param name="session"></param>
        public void LaunchGame(MSession session)
        {
            moddedLauncher.SetStatusBar("Starting Minecraft", GetType(), StatusType.Launching);

            // Initializing Launcher

            // Set minecraft home directory
            // MinecraftPath.GetOSDefaultPath() return default minecraft BasePath of current OS.
            // https://github.com/AlphaBs/CmlLib.Core/blob/master/CmlLib/Core/MinecraftPath.cs

            // You can set this path to what you want like this :
            // var path = Environment.GetEnvironmentVariable("APPDATA") + "\\.mylauncher";
            var gamePath = configuration.GamePath;
            var game = new MinecraftPath(configuration.ModpackPath)
            {
                Library = Dir(gamePath + "/libraries"),
                Versions = Dir(gamePath + "/versions"),
                Runtime = Dir(gamePath + "/runtime"),
            };
            game.SetAssetsPath(gamePath + "/assets");

            // Create CMLauncher instance
            var launcher = new CMLauncher(game);
            launcher.ProgressChanged += moddedLauncher.LauncherDownloadChangeProgress;
            launcher.FileChanged += moddedLauncher.LauncherDownloadChangeFile;
            launcher.LogOutput += (s, e) =>
            {
                Console.WriteLine("NO");
                moddedLauncher.GameOutputWriteLine(e);
            };
            moddedLauncher.GameOutputWriteLine($"Initialized in {launcher.MinecraftPath.BasePath}");

            var launchOption = new MLaunchOption
            {
                MaximumRamMb = configuration.Memory,
                Session = session,
                ScreenHeight = configuration.ResolutionHeight,
                ScreenWidth = configuration.ResolutionWidth,
                JVMArguments = configuration.JVMArgs.Split(" ")
                // More options:
                // https://github.com/AlphaBs/CmlLib.Core/wiki/MLaunchOption
            };

            // (A) checks forge installation and install forge if it was not installed.
            // (B) just launch any versions without installing forge, but it can still launch forge already installed.
            // Both methods automatically download essential files (ex: vanilla libraries) and create game process.
            moddedLauncher.SetStatusBar("Minecraft Starting", GetType(), StatusType.Launching);

            // (A) download forge and launch
            var process = launcher.CreateProcess("1.12.2", "14.23.5.2854", launchOption);

            // (B) launch vanilla version
            // var process = launcher.CreateProcess("1.15.2", launchOption);

            // If you have already installed forge, you can launch it directly like this.
            // var process = launcher.CreateProcess("1.12.2-forge1.12.2-14.23.5.2838", launchOption);

            // launch by user input
            //Console.WriteLine("input version (example: 1.12.2) : ");
            //var process = launcher.CreateProcess(Console.ReadLine(), launchOption);
            moddedLauncher.SetStatusBar("Minecraft Starting", GetType(), StatusType.Launching);

            //var process = launcher.CreateProcess("1.16.2", "33.0.5", launchOption);
            WriteLine(process.StartInfo.Arguments);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Thread.Sleep(TimeSpan.FromSeconds(30));
                moddedLauncher.SetStatusBar("Minecraft Started", GetType(), StatusType.Ready);
            }).Start();



            // Below codes are print game logs in Console.
            var processUtil = new CmlLib.Utils.ProcessUtil(process);
            processUtil.OutputReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e)) moddedLauncher.GameOutputWriteLine(e);
            };
            processUtil.StartWithEvents();
            process.WaitForExit();

            // or just start it without print logs
            // process.Start();

            Console.ReadLine();

            return;
        }

        public MSession AutoLogin()
        {
            MLogin login = new();
            // if cached session is invalid, it refresh session automatically.
            // but refreshing session doesn't always succeed, so you have to handle this.
            WriteLine("Try Auto login");
            WriteLine(login.SessionCacheFilePath);
            var response = login.TryAutoLogin();

            WriteLine($"Auto login failed : {response.Result}");
            return response.Session;

        }

        /// <summary>
        /// Creates a Minecraft login
        /// </summary>
        /// <param name="email">Mojang Email</param>
        /// <param name="password">Mojang Password</param>
        /// <returns>null if login failed, an Minecraft session otherwise</returns>
        public MSession Login(string email, string password)
        {
            //https://github.com/AlphaBs/CmlLib.Core/blob/v3.0.0/CmlLibCoreSample/Program.cs
            MLogin login = new();
            
            var response = login.Authenticate(email, password);

            if (!response.IsSuccess)
            {
                // session.Message contains detailed error message. it can be null or empty string.
                WriteLine($"failed to login. {response.Result} : {response.ErrorMessage}");
                return null;
            }


            return response.Session;
        }

    }
}
