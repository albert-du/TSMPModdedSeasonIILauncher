using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TSMPModdedSIILauncher.Core
{
    public sealed class Configuration
    {
        public string Email { get; set; } = string.Empty;
        public string JVMArgs { get; set; } = string.Empty;
        public int ResolutionWidth { get; set; } = 0;
        public int ResolutionHeight { get; set; } = 0;
        public bool LiveUpdates { get; set; } = false;
        public int Memory { get; set; } = 6000;
        public bool Optifine { get; set; } = true;
        public string OptifineLink { get; set; } = "https://optifine.net/adloadx?f=OptiFine_1.12.2_HD_U_F5.jar";
        public string RepoOwner { get; set; } = "DabbingEevee";
        public string RepoName { get; set; } = "TSMP_Modded_Season_II";
        public string Branch { get; set; } = "master";
        public string MainPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TSMPModdedSeasonII");

        [JsonIgnore]
        public string ModpackPath { get => Path.Combine(MainPath, "modpack"); }
        
        [JsonIgnore]
        public string GamePath { get => Path.Combine(MainPath, "game"); }

        [JsonIgnore]
        public string ModsPath { get => Path.Combine(ModpackPath, "mods"); }
    }
}