﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CmlLib.Core
{
    public class PackageName
    {
        public static PackageName Parse(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var spliter = name.Split(':');
            if (spliter.Length < 3)
                throw new ArgumentException("invalid name");

            var pn = new PackageName();
            pn.names = spliter;

            return pn;
        }

        private PackageName()
        {

        }

        private string[] names;

        public string this[int index] { get => names[index]; }

        public string Package { get => names[0]; }
        public string Name { get => names[1]; }
        public string Version { get => names[2]; }

        public string GetPath()
        {
            return GetPath("");
        }

        public string GetPath(string nativeId, string extension = "jar")
        {
            // de.oceanlabs.mcp : mcp_config : 1.16.2-20200812.004259 : mappings
            // de\oceanlabs\mcp \ mcp_config \ 1.16.2-20200812.004259 \ mcp_config-1.16.2-20200812.004259.zip

            // [de.oceanlabs.mcp:mcp_config:1.16.2-20200812.004259@zip]
            // \libraries\de\oceanlabs\mcp\mcp_config\1.16.2-20200812.004259\mcp_config-1.16.2-20200812.004259.zip

            // [net.minecraft:client:1.16.2-20200812.004259:slim]
            // /libraries\net\minecraft\client\1.16.2-20200812.004259\client-1.16.2-20200812.004259-slim.jar

            var filename = string.Join("-", names, 1, names.Length - 1);

            if (!string.IsNullOrEmpty(nativeId))
                filename += "-" + nativeId;
            filename += "." + extension;

            return Path.Combine(GetDirectory(), filename);
        }

        public string GetDirectory()
        {
            var dir = Package.Replace(".", "/");
            return Path.Combine(dir, Name, Version);
        }

        public string GetClassPath()
        {
            return Package + "." + Name;
        }
    }
}
