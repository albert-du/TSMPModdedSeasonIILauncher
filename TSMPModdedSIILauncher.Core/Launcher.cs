using System;
using System.IO;
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
        public static string Dir(string path)
        {
            var p = Path.GetFullPath(path);
            if (!Directory.Exists(p))
                Directory.CreateDirectory(p);

            return p;
        }

        private string _password = "";

        private ConfigService configService;

        public string Password { private get => _password; set => _password = value; }

        public Launcher(ConfigService configService)
        {
            this.configService = configService;

        }
        public void LaunchGame()
        {
            // Initializing Launcher

            // Set minecraft home directory
            // MinecraftPath.GetOSDefaultPath() return default minecraft BasePath of current OS.
            // https://github.com/AlphaBs/CmlLib.Core/blob/master/CmlLib/Core/MinecraftPath.cs

            // You can set this path to what you want like this :
            // var path = Environment.GetEnvironmentVariable("APPDATA") + "\\.mylauncher";
            var gamePath = configService.Configuration.GamePath;
            var game = new MinecraftPath(configService.Configuration.ModpackPath)
            {
                Library = Dir(gamePath + "/libraries"),
                Versions = Dir(gamePath + "/versions"),
                Runtime = Dir(gamePath + "/runtime"),
            };
            game.SetAssetsPath(gamePath + "/assets");

            // Create CMLauncher instance
            var launcher = new CMLauncher(game);
            launcher.ProgressChanged += Downloader_ChangeProgress;
            launcher.FileChanged += Downloader_ChangeFile;
            launcher.LogOutput += (s, e) => Console.WriteLine(e);

            Console.WriteLine($"Initialized in {launcher.MinecraftPath.BasePath}");

            var launchOption = new MLaunchOption
            {
                MaximumRamMb = configService.Configuration.Memory,
                Session = Login(),
                ScreenHeight = configService.Configuration.ResolutionHeight,
                ScreenWidth = configService.Configuration.ResolutionWidth,
                JVMArguments = configService.Configuration.JVMArgs.Split(" ")
                // More options:
                // https://github.com/AlphaBs/CmlLib.Core/wiki/MLaunchOption
            };

            // (A) checks forge installation and install forge if it was not installed.
            // (B) just launch any versions without installing forge, but it can still launch forge already installed.
            // Both methods automatically download essential files (ex: vanilla libraries) and create game process.

            // (A) download forge and launch
            var process = launcher.CreateProcess("1.12.2", "14.23.5.2854", launchOption);

            // (B) launch vanilla version
            // var process = launcher.CreateProcess("1.15.2", launchOption);

            // If you have already installed forge, you can launch it directly like this.
            // var process = launcher.CreateProcess("1.12.2-forge1.12.2-14.23.5.2838", launchOption);

            // launch by user input
            //Console.WriteLine("input version (example: 1.12.2) : ");
            //var process = launcher.CreateProcess(Console.ReadLine(), launchOption);

            //var process = launcher.CreateProcess("1.16.2", "33.0.5", launchOption);
            Console.WriteLine(process.StartInfo.Arguments);

            // Below codes are print game logs in Console.
            var processUtil = new CmlLib.Utils.ProcessUtil(process);
            processUtil.OutputReceived += (s, e) => Console.WriteLine(e);
            processUtil.StartWithEvents();
            process.WaitForExit();

            // or just start it without print logs
            // process.Start();

            Console.ReadLine();

            return;
        }

        protected MSession Login()
        {
            //https://github.com/AlphaBs/CmlLib.Core/blob/v3.0.0/CmlLibCoreSample/Program.cs
            var login = new MLogin();

            // TryAutoLogin() read login cache file and check validation.
            // if cached session is invalid, it refresh session automatically.
            // but refreshing session doesn't always succeed, so you have to handle this.
            Console.WriteLine("Try Auto login");
            Console.WriteLine(login.SessionCacheFilePath);
            var response = login.TryAutoLogin();

            if (!response.IsSuccess) // cached session is invalid and failed to refresh token
            {
                Console.WriteLine("Auto login failed : {0}", response.Result.ToString());

                Console.WriteLine("Input mojang email : ");
                var email = string.IsNullOrEmpty( configService.Configuration.Email) ? Console.ReadLine() : configService.Configuration.Email;
                Console.WriteLine("Input mojang password : ");
                var pw = Console.ReadLine();

                response = login.Authenticate(email, pw);

                if (!response.IsSuccess)
                {
                    // session.Message contains detailed error message. it can be null or empty string.
                    Console.WriteLine("failed to login. {0} : {1}", response.Result, response.ErrorMessage);
                    Console.ReadLine();
                    Environment.Exit(0);
                    return null;
                }
            }

            return response.Session;
        }
        // Event Handling

        // The code below has some tricks to display logs prettier.
        // You can use a simpler event handler

        #region Pretty event handler

        int nextline = -1;

        private void Downloader_ChangeProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (nextline < 0)
                return;

            Console.SetCursorPosition(0, nextline);

            // e.ProgressPercentage: 0~100
            Console.WriteLine("{0}%", e.ProgressPercentage);
        }

        private void Downloader_ChangeFile(DownloadFileChangedEventArgs e)
        {
            // More information about DownloadFileChangedEventArgs
            // https://github.com/AlphaBs/CmlLib.Core/wiki/Handling-Events#downloadfilechangedeventargs

            Console.WriteLine("[{0}] {1} - {2}/{3}           ", e.FileKind.ToString(), e.FileName, e.ProgressedFileCount, e.TotalFileCount);
            if (e.FileKind == MFile.Resource && string.IsNullOrEmpty(e.FileName))
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            nextline = Console.CursorTop;
        }

        #endregion

    }
}
