using CmlLib.Core.Downloader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TSMPModdedSIILauncher.Core
{
    public class ConsoleLauncher : IModdedLauncher
    {
        public static string EditText(string defaultText, bool onlyInt = false)
        {
            //https://stackoverflow.com/questions/41197511/how-to-put-some-text-in-c-sharp-console-instead-of-user-typing-it-in
            Console.Write(defaultText);
            string input = defaultText;
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return input;
                }
                //Was is a backspace? 
                if (key.Key == ConsoleKey.Backspace)
                {
                    //Did we delete too much?
                    if (Console.CursorLeft == 0)
                    {
                        continue; //suppress
                        //Console.SetCursorPosition(Console.CursorLeft + Console.WindowWidth, Console.CursorTop += 1);
                    }
                    else
                    {
                        //Put the cursor on character back
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        //Delete it with a space
                        Console.Write(" ");
                        //Put it back again
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        //Delete the last char of the input
                        input = string.Join("", input.Take(input.Length - 1));
                    }
                }
                //Regular key? add it to the input
                else if (!onlyInt || char.IsDigit(key.KeyChar))
                {
                    input += key.KeyChar.ToString();
                    Console.Write(key.KeyChar);
                } //else it must be another control code (ESC etc) or something.
            }
        }

        private void OpenConfigMenu()
        {
            char userInput;
            do
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Settings\n");
                Console.ResetColor();
                Console.WriteLine("1. Set Default Email Address");
                Console.WriteLine("2. Set Memory Max");
                Console.WriteLine("3. Set JVM arguments");
                Console.WriteLine("4. Set Resolution Width");
                Console.WriteLine("5. Set Resolution Height");
                Console.WriteLine("6. Set Live Updates");
                Console.WriteLine("7. Reset Settings");
                Console.WriteLine("8. Exit Settings Menu");

                userInput = Console.ReadKey(true).KeyChar;

                switch (userInput)
                {
                    case '1':
                        Console.WriteLine("Default Email Address:");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        {
                            var input = EditText(Configuration.Email).Replace(" ", "") ?? "";
                            Shell.Execute($"email \"{input}\"");
                        }
                        Console.ResetColor();
                        Console.Write("press any key to continue");
                        Console.ReadKey(true);
                        break;
                    case '2':
                        Console.WriteLine("Memory Mb :");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        {
                            var input = EditText(Configuration.Memory.ToString(), true).Replace(" ", "");
                            if (input != "") Shell.Execute($"memory {Convert.ToInt32(input)}");
                        }
                        Console.ResetColor();
                        Console.Write("press any key to continue");
                        Console.ReadKey(true);
                        break;
                    case '3':
                        Console.WriteLine("JVM arguments:");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        {
                            var input = EditText(Configuration.JVMArgs);
                            Shell.Execute($"jvm \"{input}\"");
                        }
                        Console.ResetColor();
                        Console.Write("press any key to continue");
                        Console.ReadKey(true);
                        break;
                    case '4':
                        Console.WriteLine("Resolution Width:");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        {
                            var input = EditText(Configuration.ResolutionWidth.ToString(), true).Replace(" ", "");
                            if (input != "") Shell.Execute($"resolution width {Convert.ToInt32(input)}");
                        }
                        Console.ResetColor();
                        Console.Write("press any key to continue");
                        Console.ReadKey(true);
                        break;
                    case '5':
                        Console.WriteLine("Resolution Height:");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        {
                            var input = EditText(Configuration.ResolutionHeight.ToString(), true).Replace(" ", "");
                            if (input != "") Shell.Execute($"resolution height {Convert.ToInt32(input)}");
                        }
                        Console.ResetColor();
                        Console.Write("press any key to continue");
                        Console.ReadKey(true);
                        break;
                    case '6':
                        Console.WriteLine("Use Live Updates");
                        Console.WriteLine("y to use live updates, n to not");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        {
                            var input = EditText(Configuration.LiveUpdates ? "y" : "n").Replace(" ", "");
                            if (input == "y") Shell.Execute($"live_updates true");
                            else Shell.Execute($"live_updates false");
                        }
                        Console.ResetColor();
                        Console.Write("press any key to continue");
                        Console.ReadKey(true);
                        break;
                    case '7':
                        Console.WriteLine("Reset Settings");
                        Console.Write("press 'y' to reset settings, any other key to continue");
                        if (Console.ReadKey(true).KeyChar == 'y')
                        {
                            Console.WriteLine("Resetting Settings");
                            Configuration.ResetSettings(Configuration);
                        }
                        break;
                }

            } while (userInput != '8');
            Console.Clear();
            Console.WriteLine("Starting Titanic SMP Modded Season II\n\n\n");
        }

        public Configuration Configuration { get; set; }
        public ModpackService ModpackService { get; set; }
        public Launcher Launcher { get; set; }
        public Shell Shell { get; set; }
        public OptifineService OptifineService { get; set; }

        public bool ConfirmUpdate(string installedVersion, string newVersion)
        {
            Console.WriteLine($"Installed Version: {installedVersion}");
            Console.WriteLine($"Latest Version: {newVersion}");

            Console.WriteLine("Would You Like to install the latest version? (y,n)");
            if (Console.ReadKey(true).KeyChar == 'y')
            {
                return true;
            }
            else
            {
                Console.WriteLine("Skipping update");
                return false;
            }
        }

        public void GameOutputWriteLine(string text) => Console.WriteLine(text);

        public void Initialize()
        {
            Configuration = Configuration.LoadConfiguration();
            ModpackService = new(this);
            Launcher = new(this);
            OptifineService = new(this);
            Shell = new(this);
        }

        int nextline = -1;

        public void LauncherDownloadChangeFile(DownloadFileChangedEventArgs e)
        {
            // More information about DownloadFileChangedEventArgs
            // https://github.com/AlphaBs/CmlLib.Core/wiki/Handling-Events#downloadfilechangedeventargs

            Console.WriteLine("[{0}] {1} - {2}/{3}           ", e.FileKind.ToString(), e.FileName, e.ProgressedFileCount, e.TotalFileCount);
            if (e.FileKind == MFile.Resource && string.IsNullOrEmpty(e.FileName))
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            nextline = Console.CursorTop;

        }

        public void LauncherDownloadChangeProgress(object sender, ProgressChangedEventArgs e)
        {
            if (nextline < 0)
                return;

            Console.SetCursorPosition(0, nextline);

            // e.ProgressPercentage: 0~100
            Console.WriteLine("{0}%", e.ProgressPercentage);
        }

        public void SetStatusBar(string text, Type source, StatusType statusType)
        {
        }

        public void Start()
        {
            Console.ResetColor();

            Console.Title = "Titanic SMP Modded Season II Launcher";
            Console.WriteLine("Launching Titanic SMP Modded Season II");
            Thread.Sleep(300);

            // Countdown Timer
            var seconds = 10;

            bool skipAutoLogin = false;

            bool exited = false;
            while (!exited)
            {
                if (seconds < 0)
                {
                    break;
                }
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Starting in {seconds} seconds     ");
                Console.ResetColor();
                Console.WriteLine("Press 'Esc' to abort launch     ");
                Console.WriteLine("Press 'Space' to skip countdown   ");
                Console.WriteLine(skipAutoLogin ? "Auto Login Skipped                  " : "Press 'l' to skip auto-login    ");
                Console.WriteLine("Press 's' to open shell    ");
                Console.WriteLine("Press 'Enter' to config launch(er) settings   ");
                for (int i = 0; i < 20; i++)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo cki = Console.ReadKey(true);

                        switch (cki.Key)
                        {
                            case ConsoleKey.Escape:
                                return;
                            case ConsoleKey.Enter:
                                OpenConfigMenu();
                                break;
                            case ConsoleKey.Spacebar:
                                exited = true;
                                break;
                            case ConsoleKey.S:
                                Shell.Start();
                                Console.Clear();
                                break;
                            case ConsoleKey.L:
                                skipAutoLogin = true;
                                break;
                        }

                    }

                    Thread.Sleep(50);
                }
                seconds -= 1;
                try { Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 6); }
                catch { }

            }
            // Check installation
            // Download if needed
            Console.WriteLine("\n\n\n\n\n\n");

            Task.Run(async () => {
                var updateRequired = await ModpackService.NeedsDownloadAsync();
                if (updateRequired)
                {
                    await ModpackService.DownloadAsync();
                    await ModpackService.InstallAsync();
                }

            }).GetAwaiter().GetResult();
            if (!OptifineService.OptifineInstalled() && Configuration.Optifine)
            {
                // install
                Console.WriteLine("Installing optifine");
                OptifineService.InstallOptifine();
            }
            else if (OptifineService.OptifineInstalled() && !Configuration.Optifine)
            {
                //remove
                Console.WriteLine("Removing optifine");
                OptifineService.RemoveOptifine();
            }

            // Launch
            var session = skipAutoLogin ? null : Launcher.AutoLogin();
            while (session is null)
            {
                Console.WriteLine("Input mojang email : ");
                var email = string.IsNullOrEmpty(Configuration.Email) ? Console.ReadLine() : Configuration.Email;
                Console.WriteLine("Input mojang password : ");
                var pw = Console.ReadLine();
                Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 1);
                for (int i = 0; i <= pw.Length; i++)
                    Console.Write("*");
                Console.WriteLine();
                session = Launcher.Login(email, pw);
            }
            Launcher.LaunchGame(session);

            Console.ReadKey();
        }

        public void WriteLine(string text, Type source)
        {
            Console.WriteLine($"[{source.Name}] {text}");
        }
        public string ShellReadLine() => Console.ReadLine();
        public void ShellWrite(string text) => Console.Write(text);
        public void ShellWrite(string text, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            Console.Write(text);
            Console.ResetColor();
        }
        public void ShellWriteLine(string text) => Console.WriteLine(text);
        public void ShellWriteLine(string text, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        public void ShellClear() => Console.Clear();
    }
}
