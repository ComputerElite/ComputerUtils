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
                    Logger.Log("Starting adb with " + s.FileName + " " + s.Arguments);
                    using (Process exeProcess = Process.Start(s))
                    {
                        String IPS = exeProcess.StandardOutput.ReadToEnd();
                        String Error = exeProcess.StandardError.ReadToEnd();
                        exeProcess.WaitForExit();
                        Logger.Log("Output: " + IPS);
                        Logger.Log("Error Output: " + Error);
                        Logger.Log("Exit code: " + exeProcess.ExitCode);
                        Console.WriteLine("Output by ADB: " + IPS);
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
                    Logger.Log("Failed: " + e.ToString());
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
                    Logger.Log("Starting adb with " + s.ToString());
                    using (Process exeProcess = Process.Start(s))
                    {
                        exeProcess.WaitForExit();
                        String IPS = exeProcess.StandardOutput.ReadToEnd();
                        String Error = exeProcess.StandardError.ReadToEnd();
                        Logger.Log("Output: " + IPS);
                        Logger.Log("Error Output: " + Error);
                        Logger.Log("Exit code: " + exeProcess.ExitCode);
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
                    Logger.Log("Failed: " + e.ToString());
                    continue;
                }
            }
            return "adb100";
        }
    }
}