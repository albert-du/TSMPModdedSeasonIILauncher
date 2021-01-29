using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TSMPModdedSIILauncher.Core;

namespace TSMPModdedSIILauncher.ConsoleApp
{
    class Program
    {

        private static Configuration configuration;
        private static ModpackService modpackService;
        private static Launcher launcher;
        private static Shell shell;
        private static OptifineService optifineService;
        public static string EditText(string defaultText, bool onlyInt = false)
        {
            //https://stackoverflow.com/questions/41197511/how-to-put-some-text-in-c-sharp-console-instead-of-user-typing-it-in
            Console.Write(defaultText);
            string input = defaultText;
            while(true)
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


        /// <summary>
        /// Stops the program if something about the hardware or os is incompatible, not implemented
        /// </summary>
        private static bool EnvironmentValid()
        {
            return true;
        }

        private static void OpenConfigMenu()
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
                            var input = EditText(configuration.Email).Replace(" ", "") ?? "";
                            shell.Execute($"email \"{input}\"");
                        }
                        Console.ResetColor();
                        Console.Write("press any key to continue");
                        Console.ReadKey(true);
                        break;
                    case '2':
                        Console.WriteLine("Memory Mb :");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        {
                            var input = EditText(configuration.Memory.ToString(), true).Replace(" ", "");
                            if (input != "") shell.Execute($"memory {Convert.ToInt32(input)}");
                        }
                        Console.ResetColor();
                        Console.Write("press any key to continue");
                        Console.ReadKey(true);
                        break;
                    case '3':
                        Console.WriteLine("JVM arguments:");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        {
                            var input = EditText(configuration.JVMArgs);
                            shell.Execute($"jvm \"{input}\"");
                        }
                        Console.ResetColor();
                        Console.Write("press any key to continue");
                        Console.ReadKey(true);
                        break;
                    case '4':
                        Console.WriteLine("Resolution Width:");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        {
                            var input = EditText(configuration.ResolutionWidth.ToString(),true).Replace(" ", "");
                            if (input != "") shell.Execute($"resolution width {Convert.ToInt32(input)}");
                        }
                        Console.ResetColor();
                        Console.Write("press any key to continue");
                        Console.ReadKey(true);
                        break;
                    case '5':
                        Console.WriteLine("Resolution Height:");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        {
                            var input = EditText(configuration.ResolutionHeight.ToString(), true).Replace(" ", "");
                            if (input != "") shell.Execute($"resolution height {Convert.ToInt32(input)}");
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
                            var input = EditText(configuration.LiveUpdates ? "y" : "n").Replace(" ", "");
                            if (input == "y") shell.Execute($"live_updates true");
                            else shell.Execute($"live_updates false");
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
                            Configuration.ResetSettings(configuration);
                        }
                        break;
                }

            } while (userInput != '8');
            Console.Clear();
            Console.WriteLine("Starting Titanic SMP Modded Season II\n\n\n");
        }

        static void Main(string[] args)
        {
            configuration   =   Configuration.LoadConfiguration();
            optifineService =   new(configuration);
            modpackService  =   new(configuration);
            launcher        =   new(configuration);
            shell           =   new(configuration, modpackService, launcher,optifineService);

            Console.ResetColor();

            Console.Title = "Titanic SMP Modded Season II Launcher";
            Console.WriteLine("Launching Titanic SMP Modded Season II");
            Thread.Sleep(300);
            if (!EnvironmentValid()) return;

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
                                shell.Start();
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
                var updateRequired = await modpackService.NeedsDownloadAsync();
                if (updateRequired)
                {
                    await modpackService.DownloadAsync();
                    await modpackService.InstallAsync();
                }

            } ).GetAwaiter().GetResult();
            if (!optifineService.OptifineInstalled() && configuration.Optifine)
            {
                // install
                Console.WriteLine("Installing optifine");
                optifineService.InstallOptifine();
            }
            else if (optifineService.OptifineInstalled() && !configuration.Optifine)
            {
                //remove
                Console.WriteLine("Removing optifine");
                optifineService.RemoveOptifine();
            }

            // Launch
            var session = skipAutoLogin ? null : Launcher.AutoLogin();
            while (session is null)
            {
                Console.WriteLine("Input mojang email : ");
                var email = string.IsNullOrEmpty(configuration.Email) ? Console.ReadLine() : configuration.Email;
                Console.WriteLine("Input mojang password : ");
                var pw = Console.ReadLine();
                Console.SetCursorPosition(1, 0);
                for (int i = 0; i <= pw.Length; i++)
                    Console.Write("*");
                Console.WriteLine();

                session = Launcher.Login(email, pw);
                if (session is not null)
                {
                    launcher.LaunchGame(session);

                    Console.ReadKey();
                    break;
                }
            }
        }
    }
}
