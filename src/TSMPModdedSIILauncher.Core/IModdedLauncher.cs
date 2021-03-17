using CmlLib.Core.Downloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSMPModdedSIILauncher.Core
{
    public interface IModdedLauncher
    {
        public void SetStatusBar(string text, Type source, StatusType statusType);

        public void WriteLine(string text, Type source);

        
        public Configuration Configuration { get; set; }
        
        public ModpackService ModpackService { get; set; }
        
        public Launcher Launcher { get; set; }
        
        public Shell Shell { get; set; }
        
        public OptifineService OptifineService { get; set; }

        #region ModpackService
        public bool ConfirmUpdate(string installedVersion, string newVersion);
        #endregion

        #region Launcher
        public void LauncherDownloadChangeProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e);
        public void LauncherDownloadChangeFile(DownloadFileChangedEventArgs e);
        public void GameOutputWriteLine(string text);
        #endregion

        public void Initialize();

        public string ShellReadLine();
        public void ShellWrite(string text);
        public void ShellWrite(string text, ConsoleColor consoleColor);
        public void ShellWriteLine(string text);
        public void ShellWriteLine(string text, ConsoleColor consoleColor);
        public void ShellClear();
    }
}
