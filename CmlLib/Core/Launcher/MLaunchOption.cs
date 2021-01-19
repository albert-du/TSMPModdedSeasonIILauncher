﻿using CmlLib.Core.Auth;
using CmlLib.Core.Version;
using System;

namespace CmlLib.Core
{
    public class MLaunchOption
    {
        public MinecraftPath Path { get; set; }
        public MVersion StartVersion { get; set; }
        public MSession Session { get; set; }

        public string JavaPath { get; set; } = "";
        public int MaximumRamMb { get; set; } = 1024;
        public int MinimumRamMb { get; set; }
        public string[] JVMArguments { get; set; }

        public string DockName { get; set; }
        public string DockIcon { get; set; }

        public string ServerIp { get; set; }
        public int ServerPort { get; set; } = 25565;

        public int ScreenWidth { get; set; } = 0;
        public int ScreenHeight { get; set; } = 0;
        public bool FullScreen { get; set; } = false;

        public string VersionType { get; set; }
        public string GameLauncherName { get; set; }
        public string GameLauncherVersion { get; set; }

        internal void CheckValid()
        {
            string exMsg = null; // error message

            if (Path == null)
                exMsg = nameof(Path) + " is null";

            if (MaximumRamMb < 1)
                exMsg = "MaximumRamMb is too small.";

            if (StartVersion == null)
                exMsg = "StartVersion is null";

            if (Session == null)
                Session = MSession.GetOfflineSession("tester123");

            if (!Session.CheckIsValid())
                exMsg = "Invalid Session";

            if (ServerPort < 0 || ServerPort > 65535)
                exMsg = "Invalid ServerPort";

            if (ScreenWidth < 0 || ScreenHeight < 0)
                exMsg = "Screen Size must be greater than or equal to zero.";

            if (exMsg != null) // if launch option is invalid, throw exception
                throw new ArgumentException(exMsg);
        }
    }
}
