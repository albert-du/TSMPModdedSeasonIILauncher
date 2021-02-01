using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace TSMPModdedSIILauncher.Core
{
    public class OptifineService
    {
        private Configuration config;
        private HttpClient httpClient = new();
        private IModdedLauncher moddedLauncher;
        private void WriteLine(string text) => moddedLauncher.WriteLine(text, GetType());
        public OptifineService(IModdedLauncher moddedLauncher)
        {
            this.moddedLauncher = moddedLauncher;
            this.config = moddedLauncher.Configuration;
            httpClient.Timeout = TimeSpan.FromMinutes(3);
            httpClient.BaseAddress = new Uri( "https://optifine.net/");
        }
        public bool OptifineInstalled()
        {
            var dirInfo = new DirectoryInfo(config.ModsPath)
                .GetFiles()
                .ToList();
            return dirInfo.Exists((i) => i.Name == "optifine.jar");
        }

        public void RemoveOptifine()
        {
            moddedLauncher.SetStatusBar("Removing Optifine", GetType(), StatusType.Installing);
            WriteLine("Removing Optifine");
            var dirInfo = new DirectoryInfo(config.ModsPath)
                .GetFiles()
                .ToList();
            var file = dirInfo.Find((i) => i.Name == "optifine.jar");
            if (file is not null) File.Delete(file.FullName);
            moddedLauncher.SetStatusBar("Optifine Removed", GetType(), StatusType.Ready);
        }

        public void InstallOptifine()
        {
            WriteLine("Downloading/Installing Optifine");
            moddedLauncher.SetStatusBar("Installing Optifine", GetType(), StatusType.Installing);
            Task.Run(async () =>
            {
                RemoveOptifine();
                var page = await httpClient.GetStringAsync(config.OptifineLink);
                var lines = page.Split('\n');

                var uri = lines.First((i) => i.Trim().StartsWith("<a href='downloadx")).Substring("<a href='", "' onclick");

                var response = await httpClient.GetAsync(uri);

                using var file = File.Create(Path.Combine(config.ModsPath, "optifine.jar"));
                await response.Content.CopyToAsync(file);

            }).GetAwaiter().GetResult();
            moddedLauncher.SetStatusBar("Optifine Installed", GetType(), StatusType.Ready);
        } 
    }
}
