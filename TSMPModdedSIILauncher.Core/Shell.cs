using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using CmlLib.Core.Auth;

namespace TSMPModdedSIILauncher.Core
{
    public enum ShellExecutationResult
    {
        Success = 0,
        Failure = 1,
        Exit = 2
    }
    public class Shell
    {
        private readonly object shellLock = new object();

        public ModpackService ModpackService { get; init; }
        public OptifineService OptifineService { get; init; }
        public Launcher Launcher { get; init; }
        
        private Configuration config;

        private IModdedLauncher moddedLauncher;

        private void WriteLine(string text) => moddedLauncher.WriteLine(text, GetType()); 

        private void WriteLineCyan(string text) => moddedLauncher.ShellWriteLine(text + "\n", ConsoleColor.Cyan);
        private void WriteLineRed(string text) => moddedLauncher.ShellWriteLine(text + "\n", ConsoleColor.Red);
        public Shell(IModdedLauncher moddedLauncher)
        {
            this.moddedLauncher = moddedLauncher;
            ModpackService = moddedLauncher.ModpackService;
            OptifineService = moddedLauncher.OptifineService;
            Launcher = moddedLauncher.Launcher;
            config = moddedLauncher.Configuration;
        }

        protected ShellExecutationResult SetEmail(string email)
        {
            WriteLineCyan($"Email : string <- \"{email}\"");

            config.Email = email;

            Execute("session clear");
            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetMemory (string megabytes)
        {
            ShellExecutationResult result;
            try
            {
                config.Memory = Convert.ToInt32(megabytes);
                result = ShellExecutationResult.Success;
                WriteLineCyan($"Email: int <MB> <- {config.Memory}");
            }
            catch
            {
                result = ShellExecutationResult.Failure;
                WriteLineRed($"Failed to convert {megabytes} to int<MB>");
            }

            return result;
                
        }
        protected ShellExecutationResult SetLiveUpdates(bool useLiveUpdates)
        {
            WriteLineCyan($"UseLiveUpdates : bool <- {useLiveUpdates.ToString().ToLower()}");

            config.LiveUpdates = useLiveUpdates;

            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetResolutionWidth(int width)
        {
            WriteLineCyan($"ResolutionWidth : int <- {width}");
            config.ResolutionWidth = width;
            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetResolutionHeight(int height)
        {
            WriteLineCyan($"ResolutionHeight: int <- {height}");
            config.ResolutionHeight = height;
            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetResolution(int width, int height)
        {
            WriteLineCyan($"Resolution : int * int <- {width} * {height}");
            config.ResolutionHeight = height;
            config.ResolutionWidth = width;
            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetJVMArgs(string args)
        {
            WriteLineCyan($"JVMArguments: string <- \"{args}\"");
            config.JVMArgs = args;
            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetRepo(string owner, string repo)
        {
            WriteLineCyan($"Owner, Repository : string * string <- \"{owner}\" * \"{repo}\"");

            if (owner != "*" && owner != "_") config.RepoOwner = owner;
            if (repo != "*" && repo != "_") config.RepoName = repo;

            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetBranch(string branch)
        {
            WriteLineCyan($"Branch : string <- \"{branch}\"");

            config.Branch = branch;

            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetOptifine(bool useOptifine)
        {
            WriteLineCyan($"UseOptifine : bool <- {useOptifine.ToString().ToLower()}");

            if (!useOptifine) OptifineService.RemoveOptifine(); 

            config.Optifine = useOptifine;

            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetOptifineLink(string link)
        {
            WriteLineCyan($"OptifineLink : string <- \"{link}\"");

            OptifineService.RemoveOptifine();
            config.OptifineLink = link;

            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetPath(string path)
        {
            WriteLineCyan($"MainPath : string <- \"{path}\"");

            config.MainPath = path;

            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetExperimentalLauncher(bool useExperimentalLauncher)
        {
            WriteLineCyan($"UseExperimentalLauncher : bool <- {useExperimentalLauncher}");

            config.UseExperimentalLauncher = useExperimentalLauncher;

            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetConsoleLauncher(bool useConsoleLauncher)
        {
            WriteLineCyan($"UseConsoleLauncher : bool <- {useConsoleLauncher}");
            config.UseConsoleLauncher = useConsoleLauncher;
            return ShellExecutationResult.Success;
        }

        protected ShellExecutationResult Update(bool force = false)
        {
            
            moddedLauncher.ShellWriteLine(!force ? "Checking for Updates" : "Forcing Update");
            
            try
            {
                Task.Run(async () => {
                    if (force || await ModpackService.NeedsDownloadAsync())
                    {
                        await ModpackService.DownloadAsync();
                        await ModpackService.InstallAsync();
                    }

                }).GetAwaiter().GetResult();
                moddedLauncher.ShellWriteLine("");
                return ShellExecutationResult.Success;
            }
            catch (Exception e)
            {
                moddedLauncher.ShellWriteLine(e.Message);

                moddedLauncher.ShellWriteLine("Update Failed", ConsoleColor.Red);
            }
            moddedLauncher.ShellWriteLine("");
            return ShellExecutationResult.Failure;
        }

        protected ShellExecutationResult Uninstall()
        {
            ShellExecutationResult result;
            try
            {

                ModpackService.Uninstall();
                result = ShellExecutationResult.Success;
            }
            catch
            {
                result = ShellExecutationResult.Failure;
            }
            return result;
        }

        public ShellExecutationResult Execute(string input)
        {
            ShellExecutationResult PrintResult(string text)
            {
                WriteLineCyan(text);
                return ShellExecutationResult.Success;
            }

            var tokens = input.Split(' ').Select(i => i.Trim('\"') ).ToList();
            var isBool = tokens.Count > 1 && (tokens[1] == "true" || tokens[1] == "false");
            ShellExecutationResult response;
            lock (shellLock)
            {
                response = (tokens[0], tokens.Count) switch
                {

                    ("email", 1) => PrintResult($"Email : string = \"{config.Email}\""),
                    ("email", 2) => SetEmail(tokens[1]),

                    ("memory", 1) => PrintResult($"Email : int<MB> = {config.Memory}"),
                    ("memory", 2) => SetMemory(tokens[1]),

                    ("live_updates", 1) => PrintResult($"UseLiveUpdates : bool = {config.LiveUpdates.ToString().ToLower()}"),
                    ("live_updates", 2) when isBool => SetLiveUpdates(tokens[1] == "true"),

                    ("resolution", 1) => PrintResult($"Resolution : int * int = {config.ResolutionWidth} * {config.ResolutionHeight}"),
                    ("resolution", 2) when tokens[1] == "height" => PrintResult($"ResolutionHeight : int = {config.ResolutionHeight}"),
                    ("resolution", 2) when tokens[1] == "width" => PrintResult($"ResolutionWidth : int = {config.ResolutionWidth}"),
                    ("resolution", 3) when tokens[1] == "height" => SetResolutionHeight(Convert.ToInt32(tokens[2])),
                    ("resolution", 3) when tokens[1] == "width" => SetResolutionWidth(Convert.ToInt32(tokens[2])),
                    ("resolution", 4) when tokens[1] == "SET" => SetResolution(Convert.ToInt32(tokens[2]), Convert.ToInt32(tokens[3])),

                    ("jvm", 1) => PrintResult($"JVMArguments : string = \"{config.JVMArgs}\""),
                    ("jvm", _) => SetJVMArgs(string.Join(' ', tokens.ToArray()[1..])),

                    ("repository", 1) => PrintResult($"Owner, Repository : string * string = \"{config.RepoOwner}\" * \"{config.RepoName}\""),
                    ("repository", 3) => SetRepo(tokens[1], tokens[2]),

                    ("branch", 1) => PrintResult($"Branch : string = \"{config.Branch}\""),
                    ("branch", 2) => SetBranch(tokens[1]),

                    ("optifine", 1) => PrintResult($"UseOptifine : bool = {config.Optifine.ToString().ToLower()}"),
                    ("optifine", 2) when isBool => SetOptifine(tokens[1] == "true"),

                    ("optifine_link", 1) => PrintResult($"OptifineLink : string = \"{config.OptifineLink}\""),
                    ("optifine_link", 2) => SetOptifineLink(tokens[1]),

                    ("path", 1) => PrintResult($"MainPath : string = \"{config.MainPath}\""),
                    ("path", 2) => SetPath(tokens[1]),

                    ("experimental_launcher", 1) => PrintResult($"UseExperimentalLauncher : bool = {config.UseExperimentalLauncher.ToString().ToLower()}"),
                    ("experimental_launcher", 2) when isBool => SetExperimentalLauncher(tokens[1] == "true"),

                    ("console_launcher", 1) => PrintResult($"UseConsoleLauncher : bool = {config.UseConsoleLauncher.ToString().ToLower()}"),
                    ("console_launcher", 2) when isBool => SetConsoleLauncher(tokens[1] == "true"),

                    ("update", 1) => Update(),
                    ("update", 2) when (tokens[1] == "-f" || tokens[1] == "--force") => Update(true),

                    ("config", 1) => ((Func<ShellExecutationResult>)(() => { moddedLauncher.ShellWriteLine($"{JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true })}\n"); return ShellExecutationResult.Success; }))(),
                    ("config", 2) when (tokens[1] == "clear") => ((Func<ShellExecutationResult>)(() => { Configuration.ResetSettings(config); return ShellExecutationResult.Success; }))(),

                    ("session", 1) => ((Func<ShellExecutationResult>)(() => { var login = new MLogin(); moddedLauncher.ShellWriteLine($"{JsonSerializer.Serialize(login.ReadSessionCache(), new JsonSerializerOptions { WriteIndented = true }) }\n"); return ShellExecutationResult.Success; }))(),
                    ("session", 2) when (tokens[1] == "clear") => ((Func<ShellExecutationResult>)(() => { var login = new MLogin(); login.DeleteTokenFile(); moddedLauncher.ShellWriteLine("Cleared Session Cache\n"); return ShellExecutationResult.Success; }))(),

                    ("uninstall", 1) => ((Func<ShellExecutationResult>)(() => { moddedLauncher.ShellWriteLine("type 'UNINSTALL' to uninstall the modpack and game"); if (moddedLauncher.ShellReadLine() != "UNINSTALL") return ShellExecutationResult.Success; return Uninstall(); }))(),

                    ("exit", 1) => ShellExecutationResult.Exit,

                    ("clear", 1) => Execute("shell"),

                    ("shell", 1) => ((Func<ShellExecutationResult>)(() => { moddedLauncher.ShellClear(); Start(); return ShellExecutationResult.Exit; }))(),

                    ("stop", 1) => ((Func<ShellExecutationResult>)(() => { Environment.Exit(0); return ShellExecutationResult.Success; }))(),

                    (_, 1) => ((Func<ShellExecutationResult>)(() => { WriteLineRed($"Unknown Command: \"{tokens[0]}\""); return ShellExecutationResult.Failure; }))(),
                    _ => ShellExecutationResult.Failure,
                };
                config.Save();
            }
            
            return response;
        }

        public void Start()
        {
            moddedLauncher.ShellClear();
            moddedLauncher.ShellWriteLine("TSMP/M SII Shell v1.0");
            while(true)
            {
                moddedLauncher.ShellWrite("> ");
                if ( Execute(moddedLauncher.ShellReadLine()) == ShellExecutationResult.Exit) return;
            }
        }
    }
}
