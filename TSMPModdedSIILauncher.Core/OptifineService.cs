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
        private string modsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TSMPModdedSeasonII", "modpack", "mods");
        public OptifineService(Configuration config)
        {
            this.config = config;
            httpClient.Timeout = TimeSpan.FromMinutes(3);
            httpClient.BaseAddress = new Uri( "https://optifine.net/");
        }
        public bool OptifineInstalled()
        {
            var dirInfo = new DirectoryInfo(modsPath)
                .GetFiles()
                .ToList();
            return dirInfo.Exists((i) => i.Name == "optifine.jar");
        }

        public void RemoveOptifine()
        {
            var dirInfo = new DirectoryInfo(modsPath)
                .GetFiles()
                .ToList();
            var file = dirInfo.Find((i) => i.Name == "optifine.jar");
            if (file is not null) File.Delete(file.FullName);
        }

        public void InstallOptifine()
        {
            Task.Run(async () =>
            {
                RemoveOptifine();
                var page = await httpClient.GetStringAsync(config.OptifineLink);
                var lines = page.Split('\n');

                var uri = lines.First((i) => i.Trim().StartsWith("<a href='downloadx")).Substring("<a href='", "' onclick");

                var response = await httpClient.GetAsync(uri);

                using var file = File.Create(Path.Combine(modsPath, "optifine.jar"));
                await response.Content.CopyToAsync(file);

            }).GetAwaiter().GetResult();
        } 
    }
}
