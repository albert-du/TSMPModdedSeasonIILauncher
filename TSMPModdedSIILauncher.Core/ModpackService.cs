using System;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using static System.Console;

namespace TSMPModdedSIILauncher.Core
{

    public class ModpackService
    {
        private HttpClient httpClient = new();
        private enum installType { Live, Release, None }

        private GitHubClient githubClient = new GitHubClient(new ProductHeaderValue("epic-thing"));

        private ConfigService configService;

        private string modpackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TSMPModdedSeasonII");

        private static string GetNumbers(string input) => new string(input.Where(c => char.IsDigit(c) || c == '.' ).ToArray());

        private async Task<installType> getInstallTypeAsync()
        {
            var filePath = Path.Combine(modpackPath, "install_type.txt");
            if (File.Exists(filePath))
                return (await File.ReadAllLinesAsync(filePath))[0] == "live" ? installType.Live : installType.Release;
            else return installType.None;
        }
        private async Task<decimal> getInstallVersionAsync()
        {
            var filePath = Path.Combine(modpackPath, "install_type.txt");
            if (File.Exists(filePath))
                return Convert.ToDecimal(GetNumbers((await File.ReadAllLinesAsync(filePath))[1]));
            else return -1;
        }

        public async Task DownloadAsync()
        {
            WriteLine("Preparing to Download");
            var zipPath = Path.Combine(modpackPath, "modpack.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);
            string url;
            if (configService.Configuration.LiveUpdates)
            {
                url = $"https://github.com/{configService.Configuration.RepoOwner}/{configService.Configuration.RepoName}/archive/{configService.Configuration.Branch}.zip";
            }
            else
            {
                var asset = (await GetAllReleasesAsync())[0].Assets.First(x => x.Name.Contains("modpack.zip"));
                url = asset.BrowserDownloadUrl;
            }
            WriteLine($"Downloading from {url}\nThis may take a few minutes");

            // Download and display loading animation
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await httpClient.GetAsync(url);
            
            TimeSpan ts = stopwatch.Elapsed;

            if (!Directory.Exists(modpackPath)) Directory.CreateDirectory(modpackPath);
            using var destination = File.Create(zipPath);
            await response.Content.CopyToAsync(destination);
            WriteLine($"Download Complete in {ts.Minutes}:{ts.Seconds}" );

            stopwatch.Reset();

        }

        public async Task InstallAsync()
        {
            string[] excludedPaths = { "saves", "resourcepacks", "crash-reports", "shaderpacks", "screenshots", "options", "logs", "options.txt","optionsshader.txt","optionsof.txt" };
            string[] unnecessaryPaths = { ".github", "modules" };
            WriteLine("Installing");
            var zipPath = Path.Combine(modpackPath, "modpack.zip");
            var tempPath = Path.Combine(Path.GetTempPath(), "TSMPModdedSII");
            var installPath = Path.Combine(modpackPath, "modpack");
            using var zipArchive = ZipFile.OpenRead(zipPath);
            Directory.Delete(tempPath, true);
            ZipFile.ExtractToDirectory(zipPath, tempPath);

            // delete old files and folders
            WriteLine("Cleaning Directory");
            foreach (var folder in Directory.GetDirectories(installPath).Select(i => new DirectoryInfo(i)))
            {
                if (!excludedPaths.Contains(folder.Name) ) Directory.Delete(folder.FullName, true);
            }
            foreach (var file in Directory.GetFiles(installPath).Select(i => new FileInfo(i)))
            {
                if (!excludedPaths.Contains(file.Name)) File.Delete(file.FullName);
            }
            WriteLine("Done Cleaning Directory");

            // copy new files and folders
            var tempPathInfo = new DirectoryInfo(tempPath);
            if (tempPathInfo.GetFiles().Length == 0) tempPath = Path.Combine(tempPath, tempPathInfo.GetDirectories().First().Name);

            WriteLine("Copying Directories");
            foreach (var folder in Directory.GetDirectories(tempPath).Select(i => new DirectoryInfo(i)))
                if (! excludedPaths.Contains(folder.Name) && !unnecessaryPaths.Contains(folder.Name) ) DirectoryCopy(folder.FullName, Path.Combine(installPath, folder.Name), true);

            WriteLine("Copying Files");
            foreach (var file in Directory.GetFiles(tempPath).Select(i => new FileInfo(i)))
                if (!excludedPaths.Contains(file.Name) && !unnecessaryPaths.Contains(file.Name)) file.CopyTo(Path.Combine(installPath, file.Name), false);


            Directory.Delete(tempPath, true);
            //write version
            var versionFile = Path.Combine(modpackPath, "install_type.txt");
            if (File.Exists(versionFile)) File.Delete(versionFile);
            File.WriteAllLines(versionFile, new string[] {
                configService.Configuration.LiveUpdates ? "live" : "release",
                configService.Configuration.LiveUpdates ?     File.ReadAllLines(Path.Combine(modpackPath, "modpack", "version.txt"))[0]   : GetNumbers((await GetAllReleasesAsync())[0].TagName)
            });

            WriteLine("Installation complete");

        }

        public async Task<bool> needsDownloadAsync()
        {
            var installedType = await getInstallTypeAsync();
            var installedVersion = await getInstallVersionAsync();
            var selectedType = configService.Configuration.LiveUpdates ? installType.Live : installType.Release;

            var newestVersion = selectedType == installType.Release? Convert.ToDecimal(GetNumbers((await GetAllReleasesAsync())[0].TagName)) : (await GetLatestLiveVersionAsync()) ;
            if ( (installedType == installType.None) || ( selectedType != installedType) )
            {
                // you've changed the live version config, will need to instal
                return true;
            }
            else if (newestVersion != installedVersion)
            {
                WriteLine($"Installed Version: {installedVersion} | {new DateTime((long)installedVersion, DateTimeKind.Utc ).ToLocalTime()}" );
                WriteLine($"Latest Version: {newestVersion}" + (configService.Configuration.LiveUpdates ? $" | {new DateTime(Convert.ToInt64(await GetLatestLiveVersionAsync()), DateTimeKind.Utc).ToLocalTime() } " : ""));

                WriteLine("Would You Like to install the latest version? (y,n)");
                if (ReadKey(true).KeyChar == 'y')
                {
                    return true;
                }
                else
                {
                    WriteLine("Skipping update");
                }

            }
            return false;
           
        }

        public ModpackService (ConfigService configService)
        {
            this.configService = configService;
            httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public Task<IReadOnlyList<Release>> GetAllReleasesAsync() => githubClient.Repository.Release.GetAll(configService.Configuration.RepoOwner, configService.Configuration.RepoName);

        public async Task<decimal> GetLatestLiveVersionAsync() => Convert.ToDecimal( (await githubClient.Repository.Content.GetAllContentsByRef(configService.Configuration.RepoOwner, configService.Configuration.RepoName, "version.txt",configService.Configuration.Branch))[0].Content.Split('\n')[0] );

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}
