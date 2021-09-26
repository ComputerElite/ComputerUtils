using ComputerUtils.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ComputerUtils.ADB
{
    public class ADBInteractor
    {
        public List<string> ADBPaths { get; set; } = new List<string>() { "adb.exe", "User\\Android\\platform-tools_r29.0.4-windows\\platform-tools\\adb.exe", "User\\AppData\\Roaming\\SideQuest\\platform-tools\\adb.exe", "C:\\Program Files\\SideQuest\\resources\\app.asar.unpacked\\build\\platform-tools\\adb.exe" };


        public bool Pull(string source, string destination)
        {
            Logger.Log("Pulling " + source + " to " + destination, LoggingType.ADB);
            return adb("pull \"" + source + "\" \"" + destination + "\"");
        }
        public bool InstallAPK(string pathToApk, AndroidUser u)
        {
            return InstallAPK(pathToApk, u.id);
        }
        public bool InstallAPK(string pathToApk, string user = "0")
        {
            Logger.Log("Installing " + pathToApk + " on user " + user + ". This may take a bit.", LoggingType.ADB);
            return adb("install --user " + user + " \"" + pathToApk + "\"");
        }

        public bool Uninstall(string package, AndroidUser u)
        {
            return Uninstall(package, u.id);
        }
        public bool Uninstall(string package, string user = "0")
        {
            Logger.Log("Uninstalling " + package + " on user " + user, LoggingType.ADB);
            return adb("uninstall --user " + user + " \"" + package + "\"");
        }

        public bool Push(string source, string destination)
        {
            Logger.Log("Pushing " + source + " to " + destination, LoggingType.ADB);
            return adb("push \"" + source + "\" \"" + destination + "\"");
        }

        public List<string> ListPackages(AndroidUser u)
        {
            return ListPackages(u.id);
        }
        public List<string> ListPackages(string user = "0")
        {
            Logger.Log("Listing packages of user " + user);
            List<string> packages = new List<string>();
            foreach(string s in adbS("shell pm list packages --user " + user).Split('\n'))
            {
                if(s.Contains(":")) packages.Add(s.Split(':')[1]);
            }
            return packages;
        }

        public bool StopApp(string appid)
        {
            Logger.Log("Stopping " + appid, LoggingType.ADB);
            return adb("shell am force-stop " + appid);
        }

        public List<AndroidUser> GetUsers()
        {
            Logger.Log("Getting all Users", LoggingType.ADB);
            List<AndroidUser> users = new List<AndroidUser>();
            foreach (string s in adbS("shell pm list users").Split('\n'))
            {
                if (s.Trim().StartsWith("UserInfo{"))
                {
                    users.Add(new AndroidUser(s.Trim().Replace("UserInfo{", "").Split(':')[0], s.Trim().Replace("UserInfo{", "").Split(':')[1]));
                    
                }
            }
            Logger.Log("Got " + users.Count + " users. Usernames will not be shows due to privacy reasons.", LoggingType.ADB);
            return users;
        }

        public bool adb(String Argument)
        {
            return adbThreadHandler(Argument).Result;
        }

        public async Task<bool> adbThreadHandler(String Argument)
        {
            bool returnValue = false;
            String txtAppend = "N/A";
            Thread t = new Thread(() =>
            {
                switch (adbThread(Argument))
                {
                    case "true":
                        returnValue = true;
                        txtAppend = "";
                        break;
                    case "adb110":
                        txtAppend = "\n\n\nAn error Occured (Code: ADB110). Check following:\n\n- Your Quest is connected, Developer Mode enabled and USB Debugging enabled.";
                        break;
                    case "adb100":
                        txtAppend = "\n\nAn error Occured (Code: ADB100). Check following:\n\n- You have adb installed.";
                        break;
                    case "false":
                        txtAppend = "\n\nAn unhandled ADB error has occured. More info in log";
                        break;
                }
            });
            t.IsBackground = true;
            t.Start();
            while (txtAppend == "N/A")
            {
                await DelayCheck();
            }
            if (txtAppend != "N/A")
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(txtAppend);
            }
            return returnValue;
        }

        public string adbThread(String Argument)
        {
            String User = System.Environment.GetEnvironmentVariable("USERPROFILE");
            foreach (String ADB in ADBPaths)
            {

                ProcessStartInfo s = new ProcessStartInfo();
                s.CreateNoWindow = true;
                s.UseShellExecute = false;
                s.FileName = ADB.Replace("User", User);
                s.Arguments = Argument;
                s.RedirectStandardOutput = true;
                s.RedirectStandardError = true;
                try
                {
                    // Start the process with the info we specified.
                    // Call WaitForExit and then the using statement will close.
                    Logger.Log("Starting adb with " + s.FileName + " " + s.Arguments, LoggingType.ADBIntern);
                    using (Process exeProcess = Process.Start(s))
                    {
                        String IPS = exeProcess.StandardOutput.ReadToEnd();
                        String Error = exeProcess.StandardError.ReadToEnd();
                        exeProcess.WaitForExit();
                        Logger.Log("Output: " + IPS, LoggingType.ADBIntern);
                        Logger.Log("Error Output: " + Error, LoggingType.ADBIntern);
                        Logger.Log("Exit code: " + exeProcess.ExitCode, LoggingType.ADBIntern);
                        if(!Logger.displayLogInConsole) Console.WriteLine("Output by ADB: " + IPS);
                        if (IPS.Contains("no devices/emulators found") && exeProcess.ExitCode != 0)
                        {
                            return "adb110";
                        }
                        if(exeProcess.ExitCode != 0)
                        {
                            Logger.Log("An unhandled ADB error has occured: Output: \n" + IPS + "\n\nError Output: " + Error, LoggingType.Warning);
                            return "false";
                        }
                        return "true";
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("ADB Failed: " + e.ToString(), LoggingType.Warning);
                    continue;
                }
            }
            return "adb100";
        }

        public string adbS(String Argument)
        {
            return adbSThreadHandler(Argument).Result;
        }

        public async Task<string> adbSThreadHandler(String Argument)
        {
            string returnValue = "Error";
            String txtAppend = "N/A";
            Thread t = new Thread(() =>
            {
                String MethodReturnValue = adbSThread(Argument);
                switch (MethodReturnValue)
                {
                    case "adb110":
                        txtAppend = "\n\nAn error Occured (Code: ADB110). Check following:\n\n- Your Quest is connected, Developer Mode enabled and USB Debugging enabled.";
                        break;
                    case "adb100":
                        txtAppend = "\n\nAn error Occured (Code: ADB100). Check following:\n\n- You have adb installed.";
                        break;
                    case "false":
                        txtAppend = "\n\nAn unhandled ADB error has occured. More info in log";
                        break;
                    default:
                        returnValue = MethodReturnValue;
                        break;
                }
            });
            t.IsBackground = true;
            t.Start();
            while (txtAppend == "N/A" && returnValue == "Error")
            {
                await DelayCheck();
            }
            if (txtAppend != "N/A")
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(txtAppend);
            }
            return returnValue;
        }

        public async Task DelayCheck()
        {
            var frame = new DispatcherFrame();
            new Thread((ThreadStart)(() =>
            {
                Thread.Sleep(500);
                frame.Continue = false;
            })).Start();
            Dispatcher.PushFrame(frame);
        }

        public string adbSThread(String Argument)
        {
            String User = System.Environment.GetEnvironmentVariable("USERPROFILE");

            foreach (String ADB in ADBPaths)
            {
                ProcessStartInfo s = new ProcessStartInfo();
                s.CreateNoWindow = true;
                s.UseShellExecute = false;
                s.FileName = ADB.Replace("User", User);
                s.WindowStyle = ProcessWindowStyle.Minimized;
                s.Arguments = Argument;
                s.RedirectStandardOutput = true;
                s.RedirectStandardError = true;
                try
                {
                    // Start the process with the info we specified.
                    // Call WaitForExit and then the using statement will close.
                    Logger.Log("Starting adb with " + s.FileName + " " + s.Arguments, LoggingType.ADBIntern);
                    using (Process exeProcess = Process.Start(s))
                    {
                        String IPS = exeProcess.StandardOutput.ReadToEnd();
                        String Error = exeProcess.StandardError.ReadToEnd();
                        exeProcess.WaitForExit();
                        Logger.Log("Output: " + IPS, LoggingType.ADBIntern);
                        Logger.Log("Error Output: " + Error, LoggingType.ADBIntern);
                        Logger.Log("Exit code: " + exeProcess.ExitCode, LoggingType.ADBIntern);
                        if (IPS.Contains("no devices/emulators found") && exeProcess.ExitCode != 0)
                        {
                            return "adb110";
                        }
                        if (exeProcess.ExitCode != 0)
                        {
                            Logger.Log("An unhandled ADB error has occured: Output: \n" + IPS + "\n\nError Output: " + Error, LoggingType.Warning);
                            return "false";
                        }
                        return IPS;
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("ADB Failed: " + e.ToString(), LoggingType.Warning);
                    continue;
                }
            }
            return "adb100";
        }
    }

    public class AndroidUser
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";

        public AndroidUser(string id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public override string ToString()
        {
            return id + ": " + name;
        }
    }
}