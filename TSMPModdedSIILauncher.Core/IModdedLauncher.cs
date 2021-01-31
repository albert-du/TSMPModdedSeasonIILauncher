using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSMPModdedSIILauncher.Core
{
    interface IModdedLauncher
    {
        public bool VersionCheckInProgress { get; protected set; }

        public bool DownloadInProgress { get; protected set; }

        public bool InstallInProgress { get; protected set; }
        public Configuration Configuration { get; protected set; }
        public ModpackService ModpackService { get; protected set; }
        public Launcher Launcher { get; protected set; }
        public Shell Shell { get; protected set; }
        public OptifineService optifineService { get; protected set; }

        public void Start();

        public void Initialize()
        {

        }
    }
}
