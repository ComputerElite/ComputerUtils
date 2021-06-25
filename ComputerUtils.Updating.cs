using ComputerUtils.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.Json;

namespace ComputerUtils.Updating
{
    public class Updater
    {
        public string version = "1.0.0";
        public string exe = AppDomain.CurrentDomain.BaseDirectory;
        public string exeName = "";
        public string AppName = "";
        public string GitHubRepoLink = "";

        public Updater(string currentVersion, string GitHubRepoLink, string AppName, string exeName = "auto")
        {
            this.version = currentVersion;
            this.GitHubRepoLink = GitHubRepoLink;
            this.AppName = AppName;
            this.exeName = exeName;
        }

        public Updater() { }

        public bool CheckUpdate()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Checking for updates");
            UpdateEntry latest = GetLatestVersion();
            if (latest.comparedToCurrentVersion == 1)
            {
                Logger.Log("Update available");
                Console.WriteLine("New update availabel! Current version: " + version + ", latest version: " + latest.Version);
                return true;
            }
            else if (latest.comparedToCurrentVersion == -2)
            {
                Logger.Log("Error while checking for updates", LoggingType.Error);
                Console.WriteLine("An Error occured while checking for updates");
            }
            else if (latest.comparedToCurrentVersion == -1)
            {
                Logger.Log("User on preview version");
                Console.WriteLine("Have fun on a preview version (" + version + "). You can downgrade to the latest stable release (" + latest.Version + ") by pressing enter.");
                return true;
            }
            else
            {
                Logger.Log("User on newest version");
                Console.WriteLine("You are on the newest version");
            }
            return false;
        }

        public UpdateEntry GetLatestVersion()
        {
            try
            {
                Logger.Log("Fetching newest version");
                WebClient c = new WebClient();
                c.Headers.Add("user-agent", AppName + "/" + version);
                String json = c.DownloadString(GitHubRepoLink + "/main/update.json");
                UpdateFile updates = JsonSerializer.Deserialize<UpdateFile>(json);
                UpdateEntry latest = updates.Updates[0];
                latest.comparedToCurrentVersion = latest.GetVersion().CompareTo(new System.Version(version));
                return latest;
            }
            catch
            {
                Logger.Log("Fetching of newest version failed", LoggingType.Error);
                return new UpdateEntry();
            }

        }

        public void Update()
        {
            Console.WriteLine(AppName + " started in update mode. Fetching newest version");
            UpdateEntry e = GetLatestVersion();
            Console.WriteLine("Updating to version " + e.Version + ". Starting download (this may take a few seconds)");
            WebClient c = new WebClient();
            Logger.Log("Downloading update");
            c.DownloadFile(e.Download, exe + "update.zip");
            Logger.Log("Unpacking");
            Console.WriteLine("Unpacking update");
            string destDir = new DirectoryInfo(Path.GetDirectoryName(exe)).Parent.FullName + "\\";
            string launchableExe = "";
            using (ZipArchive archive = ZipFile.OpenRead(exe + "update.zip"))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    String name = entry.FullName;
                    if (name.EndsWith(".exe")) launchableExe = name;
                    if (name.EndsWith("/")) continue;
                    if (name.Contains("/")) Directory.CreateDirectory(destDir + System.IO.Path.GetDirectoryName(name));
                    entry.ExtractToFile(destDir + entry.FullName, true);
                }
            }
            if(exeName != "auto")
            {
                launchableExe = exeName;
            }
            File.Delete(exe + "update.zip");
            Logger.Log("Update successful");
            Console.WriteLine("Updated to version " + e.Version + ". Changelog:\n" + e.Changelog + "\n\nStart " + AppName + " by pressing any key");
            Console.ReadKey();
            Process.Start(destDir + launchableExe);
        }

        public void StartUpdate()
        {
            Logger.Log("Duplicating exe for update");
            Console.WriteLine("Duplicating required files");
            if (Directory.Exists(exe + "updater")) Directory.Delete(exe + "updater", true);
            Directory.CreateDirectory(exe + "updater");
            foreach (string f in Directory.GetFiles(exe))
            {
                File.Copy(f, exe + "updater\\" + Path.GetFileName(f), true);
            }
            Logger.Log("Starting update. Closing program");
            Console.WriteLine("Starting update.");
            Process.Start(exe + "updater\\" + Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location), "--update");
            Environment.Exit(0);
        }
    }

    public class UpdateFile
    {
        public List<UpdateEntry> Updates { get; set; } = new List<UpdateEntry>();
    }

    public class UpdateEntry
    {
        public List<string> Creators { get; set; } = new List<string>();
        public string Changelog { get; set; } = "N/A";
        public string Download { get; set; } = "N/A";
        public string Version { get; set; } = "1.0.0";
        public int comparedToCurrentVersion = -2; //0 = same, -1 = earlier, 1 = newer, -2 Error

        public Version GetVersion()
        {
            return new Version(Version);
        }
    }
}