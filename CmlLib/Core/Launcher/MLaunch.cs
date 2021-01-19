﻿using CmlLib.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CmlLib.Core
{
    public class MLaunch
    {
        
        private const int DefaultServerPort = 25565;

        public const string SupportVersion = "1.16.1";
        public readonly static string[] DefaultJavaParameter = new string[]
            {
                "-XX:+UnlockExperimentalVMOptions",
                "-XX:+UseG1GC",
                "-XX:G1NewSizePercent=20",
                "-XX:G1ReservePercent=20",
                "-XX:MaxGCPauseMillis=50",
                "-XX:G1HeapRegionSize=16M"
            };

        public MLaunch(MLaunchOption option)
        {
            option.CheckValid();
            LaunchOption = option;
            this.MinecraftPath = option.Path;
        }

        MinecraftPath MinecraftPath;
        public MLaunchOption LaunchOption { get; private set; }

        /// <summary>
        /// Start Game
        /// </summary>
        public void Start()
        {
            GetProcess().Start();
        }

        /// <summary>
        /// Build game process and return it
        /// </summary>
        public Process GetProcess()
        {
            string arg = string.Join(" ", CreateArg());
            Process mc = new Process();
            mc.StartInfo.FileName = LaunchOption.JavaPath;
            mc.StartInfo.Arguments = arg;
            mc.StartInfo.WorkingDirectory = MinecraftPath.BasePath;

            return mc;
        }

        public string[] CreateArg()
        {
            var version = LaunchOption.StartVersion;

            var args = new List<string>();

            // Common JVM Arguments
            if (LaunchOption.JVMArguments != null)
                args.AddRange(LaunchOption.JVMArguments);
            else
                args.AddRange(DefaultJavaParameter);

            args.Add("-Xmx" + LaunchOption.MaximumRamMb + "m");

            if (LaunchOption.MinimumRamMb > 0)
                args.Add("-Xms" + LaunchOption.MinimumRamMb + "m");

            if (!string.IsNullOrEmpty(LaunchOption.DockName))
                args.Add("-Xdock:name=" + handleEmpty(LaunchOption.DockName));
            if (!string.IsNullOrEmpty(LaunchOption.DockIcon))
                args.Add("-Xdock:icon=" + handleEmpty(LaunchOption.DockIcon));

            // Version-specific JVM Arguments
            var libArgs = new List<string>(version.Libraries.Length);

            var mclibs = version.Libraries
                .Where(lib => lib.IsRequire && !lib.IsNative)
                .Select(lib => Path.GetFullPath(Path.Combine(MinecraftPath.Library, lib.Path)));
            libArgs.AddRange(mclibs);

            libArgs.Add(Path.Combine(MinecraftPath.Versions, version.Jar, version.Jar + ".jar"));

            var libs = IOUtil.CombinePath(libArgs.ToArray());

            var native = new MNative(MinecraftPath, LaunchOption.StartVersion);
            native.CleanNatives();
            var nativePath = native.ExtractNatives();

            var jvmdict = new Dictionary<string, string>()
            {
                { "natives_directory", nativePath },
                { "launcher_name", useNotNull(LaunchOption.GameLauncherName, "minecraft-launcher") },
                { "launcher_version", useNotNull(LaunchOption.GameLauncherVersion, "2") },
                { "classpath", libs }
            };

            if (version.JvmArguments != null)
                args.AddRange(Mapper.MapInterpolation(version.JvmArguments, jvmdict));
            else
            {
                args.Add("-Djava.library.path=" + handleEmpty(nativePath));
                args.Add("-cp " + libs);
            }

            args.Add(version.MainClass);

            // Game Arguments
            var gameDict = new Dictionary<string, string>()
            {
                { "auth_player_name", LaunchOption.Session.Username },
                { "version_name", LaunchOption.StartVersion.Id },
                { "game_directory", MinecraftPath.BasePath },
                { "assets_root", MinecraftPath.Assets },
                { "assets_index_name", version.AssetId },
                { "auth_uuid", LaunchOption.Session.UUID },
                { "auth_access_token", LaunchOption.Session.AccessToken },
                { "user_properties", "{}" },
                { "user_type", "Mojang" },
                { "game_assets", MinecraftPath.AssetLegacy },
                { "auth_session", LaunchOption.Session.AccessToken },
                { "version_type", useNotNull(LaunchOption.VersionType, version.TypeStr) }
            };

            if (version.GameArguments != null)
                args.AddRange(Mapper.MapInterpolation(version.GameArguments, gameDict));
            else
                args.AddRange(Mapper.MapInterpolation(version.MinecraftArguments.Split(' '), gameDict));

            // Options
            if (!string.IsNullOrEmpty(LaunchOption.ServerIp))
            {
                args.Add("--server " + handleEmpty(LaunchOption.ServerIp));

                if (LaunchOption.ServerPort != DefaultServerPort)
                    args.Add("--port " + LaunchOption.ServerPort);
            }

            if (LaunchOption.ScreenWidth > 0 && LaunchOption.ScreenHeight > 0)
            {
                args.Add("--width " + LaunchOption.ScreenWidth);
                args.Add("--height " + LaunchOption.ScreenHeight);
            }

            if (LaunchOption.FullScreen)
                args.Add("--fullscreen");

            return args.ToArray();
        }

        // if input1 is null, return input2
        string useNotNull(string input1, string input2)
        {
            if (string.IsNullOrEmpty(input1))
                return input2;
            else
                return input1;
        }

        string handleEmpty(string input)
        {
            if (input.Contains(" "))
                return "\"" + input + "\"";
            else
                return input;
        }
    }
}
