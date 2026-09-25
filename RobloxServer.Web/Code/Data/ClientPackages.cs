using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RobloxServer.Data
{
    /// <summary>
    /// Files RobloxPlayerLauncher installs from this site, like setup.roblox.com in 2013:
    ///   App_Data/Clients/2012M.zip, App_Data/Clients/2013M.zip  (one zip per client)
    ///   App_Data/Launcher/RobloxPlayerLauncher.exe            (optional, for self updates)
    /// The version of a package is the start of its SHA-256, so replacing a zip makes every
    /// launcher download the new one.
    /// </summary>
    public static class ClientPackages
    {
        public const string Launcher = "Launcher";
        public const string LauncherFileName = "RobloxPlayerLauncher.exe";

        public class Package
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string FileName { get; set; }
            public long Size { get; set; }
            public string Sha256 { get; set; }
            public DateTime Updated { get; set; }

            public string Version
            {
                get { return "version-" + Sha256.Substring(0, 16); }
            }
        }

        static readonly object Sync = new object();
        static readonly Dictionary<string, Package> Cache = new Dictionary<string, Package>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Launcher or one of Config.Clients, with the canonical spelling; null for anything else.</summary>
        public static string CleanName(string name)
        {
            name = (name ?? "").Trim();
            if (string.Equals(name, Launcher, StringComparison.OrdinalIgnoreCase))
            {
                return Launcher;
            }
            return Config.Clients.FirstOrDefault(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
        }

        static string PathFor(string name)
        {
            return name == Launcher
                ? System.IO.Path.Combine(Config.DataPath, "Launcher", LauncherFileName)
                : System.IO.Path.Combine(Config.DataPath, "Clients", name + ".zip");
        }

        /// <summary>The package, or null when it has not been uploaded to App_Data.</summary>
        public static Package Find(string name)
        {
            name = CleanName(name);
            if (name == null)
            {
                return null;
            }

            string path = PathFor(name);
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                return null;
            }

            lock (Sync)
            {
                Package cached;
                if (Cache.TryGetValue(name, out cached) && cached.Size == info.Length && cached.Updated == info.LastWriteTimeUtc)
                {
                    return cached;
                }

                string hash;
                using (var sha = SHA256.Create())
                using (var stream = File.OpenRead(path))
                {
                    hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                }

                var package = new Package
                {
                    Name = name,
                    Path = path,
                    FileName = System.IO.Path.GetFileName(path),
                    Size = info.Length,
                    Sha256 = hash,
                    Updated = info.LastWriteTimeUtc
                };
                Cache[name] = package;
                return package;
            }
        }
    }
}
