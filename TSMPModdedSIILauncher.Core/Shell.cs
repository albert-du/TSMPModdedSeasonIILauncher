﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        public ConfigService ConfigService {get; init; }
        public ModpackService ModpackService { get; init; }
        public OptifineService OptifineService { get; init; }
        public Launcher Launcher { get; init; }

        private Configuration config;

        private static void WriteLineCyan(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(text + "\n");
            Console.ResetColor();
        }
        private static void WriteLineRed(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text + "\n");
            Console.ResetColor();
        }

        public Shell(ConfigService configService, ModpackService modpackService, Launcher launcher, OptifineService optifineService)
        {
            ConfigService = configService;
            ModpackService = modpackService;
            OptifineService = optifineService;
            Launcher = launcher;
            config = ConfigService.Configuration;
        }

        protected ShellExecutationResult SetEmail(string email)
        {
            WriteLineCyan($"Email : string <- \"{email}\"");

            config.Email = email;
            return ShellExecutationResult.Success;
        }
        protected ShellExecutationResult SetMemory (string megabytes)
        {
            ShellExecutationResult result;
            try
            {
                config.Memory = Convert.ToInt32(megabytes);
                result = ShellExecutationResult.Success;
                WriteLineCyan($"Email: int <MB> <- \"{config.Memory}\"");
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
        protected ShellExecutationResult Update(bool force = false)
        {
            
            Console.WriteLine(!force ? "Checking for Updates" : "Forcing Update");
            
            try
            {
                Task.Run(async () => {
                    if (force || await ModpackService.NeedsDownloadAsync())
                    {
                        await ModpackService.DownloadAsync();
                        await ModpackService.InstallAsync();
                    }

                }).GetAwaiter().GetResult();
                Console.WriteLine();
                return ShellExecutationResult.Success;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Update Failed");
                Console.ResetColor();
            }
            Console.WriteLine();
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
            ShellExecutationResult response = (tokens[0], tokens.Count ) switch
            {

                ("email", 1)        => PrintResult($"Branch : string = \"{config.Email}\""),
                ("email", 2)        => SetEmail(tokens[1]),

                ("memory", 1)       => PrintResult($"Email : int<MB> = \"{config.Memory}\""),
                ("memory", 2)       => SetMemory(tokens[1]),

                ("live_updates",1)   => PrintResult($"UseLiveUpdates : bool = {config.LiveUpdates.ToString().ToLower()}"),
                ("live_updates", 2) when isBool => SetLiveUpdates(tokens[1] == "true"),

                ("resolution", 1)   => PrintResult($"Resolution : int * int = {config.ResolutionWidth} * {config.ResolutionHeight}"),
                ("resolution", 2) when tokens[1] == "height"    => PrintResult($"ResolutionHeight : int = {config.ResolutionHeight}"),
                ("resolution", 2) when tokens[1] == "width"     => PrintResult($"ResolutionWidth : int = {config.ResolutionWidth}"),
                ("resolution", 3) when tokens[1] == "height"    => SetResolutionHeight(Convert.ToInt32(tokens[2]) ),
                ("resolution", 3) when tokens[1] == "width"     => SetResolutionWidth(Convert.ToInt32(tokens[2]) ),
                ("resolution", 4) when tokens[1] == "SET"       => SetResolution(Convert.ToInt32(tokens[2]),Convert.ToInt32(tokens[3]) ),

                ("jvm", 1)          => PrintResult($"JVMArguments : string = \"{config.JVMArgs}\""),
                ("jvm", _)          => SetJVMArgs(string.Join(' ', tokens.ToArray()[1..])  ),

                ("repository", 1)   => PrintResult($"Owner, Repository : string * string = \"{config.RepoOwner}\" * \"{config.RepoName}\""),
                ("repository", 3)   => SetRepo(tokens[1], tokens[2]),

                ("branch", 1)       => PrintResult($"Branch : string = \"{config.Branch}\""),
                ("branch", 2)       => SetBranch(tokens[1]),

                ("optifine", 1)     => PrintResult($"UseOptifine : bool = {config.Optifine.ToString().ToLower()}"),
                ("optifine", 2) when isBool => SetOptifine(tokens[1]=="true"),

                ("optifine_link", 1) => PrintResult($"OptifineLink : string = \"{config.OptifineLink}\""),
                ("optifine_link", 2) => SetOptifineLink(tokens[1]),
                
                ("path", 1)         => PrintResult($"MainPath : string = \"{config.MainPath}\""),
                ("path", 2)         => SetPath(tokens[1]),

                ("update", 1)       => Update(),
                ("update", 2) when ( tokens[1] == "-f" || tokens[1] == "--force") => Update(true),

                ("config", 1) => ((Func<ShellExecutationResult>)(() => { Console.WriteLine($"{JsonSerializer.Serialize( config,new JsonSerializerOptions {WriteIndented = true }  )}\n"); return ShellExecutationResult.Success; }))(),

                ("uninstall", 1) => ((Func<ShellExecutationResult>)(() => {Console.WriteLine("type 'UNINSTALL' to uninstall the modpack and game"); if (Console.ReadLine() != "UNINSTALL") return ShellExecutationResult.Success; return Uninstall();}))(),

                ("exit", 1) => ShellExecutationResult.Exit,

                ("clear",1) => Execute("shell"),
                ("shell", 1) => ((Func<ShellExecutationResult>)(() => { Console.Clear(); Start(); return ShellExecutationResult.Exit; }))(),

                ("stop", 1) => ((Func<ShellExecutationResult>)(() => { Environment.Exit(0); return ShellExecutationResult.Success; }))(),

                (_,1) => ((Func<ShellExecutationResult>)(() => { WriteLineRed($"Unknown Command: \"{tokens[0]}\""); return ShellExecutationResult.Failure; }))(),
                _ => ShellExecutationResult.Failure,
            };
            ConfigService.SaveConfig();
            return response;
        }

        public void Start()
        {
            Console.Clear();
            Console.WriteLine("TSMP/M SII Shell v1.0");
            while(true)
            {
                Console.Write("> ");
                if ( Execute(Console.ReadLine()) == ShellExecutationResult.Exit) return;
            }
        }
    }
}
