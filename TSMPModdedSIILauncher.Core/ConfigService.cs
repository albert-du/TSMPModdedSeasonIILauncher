using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text;

namespace TSMPModdedSIILauncher.Core
{
    public class ConfigService
    {
        protected static readonly string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TSMPModdedSeasonII","config.json");
        public Configuration Configuration { get; set; }

        public ConfigService()
        {
            try
            {
                var jsonString = File.ReadAllText(path);
                Configuration = JsonSerializer.Deserialize<Configuration>(jsonString);
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                ResetSettings();
            }
            catch (System.IO.FileNotFoundException)
            {
                ResetSettings();
            }
        }


        public void SaveConfig()
        {
            // check if directory exists, if not, create
            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));

            var jsonString = JsonSerializer.Serialize(Configuration);
            File.WriteAllText(path, jsonString);
        }
        public void ResetSettings()
        {
            Configuration = new Configuration();
            SaveConfig();
        }

    }
}
