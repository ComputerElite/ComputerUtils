﻿using ComputerUtils.Logging;
using ComputerUtils.VarUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ComputerUtils.ConsoleUi
{
    public class ConsoleUiController
    {
        public Thread inputThread = null;
        public int y = 0;
        public int x = 0;
        public int initialY = 0;
        public int initialX = 0;
        public bool input = true;
        public List<ConsoleUiToggle> toggles = new List<ConsoleUiToggle>();
        public List<ConsoleUiButton> buttons = new List<ConsoleUiButton>();

        public delegate void finished();
        public event finished ConsoleUiInputFinishedEvent;

        public static string QuestionString(string question)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(question);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.White;
            return Console.ReadLine();
        }

        public static string ShowMenu(string[] options, string questionName = "choice")
        {
            Logger.Log("Setting up menu with " + options.Length + " options");
            Console.ForegroundColor = ConsoleColor.White;
            for (int i = 0; i < options.Length; i++)
            {
                Console.WriteLine("[" + (i + 1) + "] " + options[i]);
            }
            String choice = QuestionString(questionName + ": ");
            Logger.Log("User choose option " + choice + " in menu");
            return choice;
        }


        public ConsoleUiToggle AddUiToggle(int xOffset, int yOffset, string label)
        {
            ConsoleUiToggle toggle = new ConsoleUiToggle(this);
            toggle.xStart = xOffset;
            toggle.yStart = yOffset;
            toggle.label = label;
            toggles.Add(toggle);
            return toggles[toggles.Count - 1];
        }

        public ConsoleUiButton AddUiButton(int xOffset, int yOffset, string label)
        {
            ConsoleUiButton button = new ConsoleUiButton(this);
            button.xStart = xOffset;
            button.yStart = yOffset;
            button.label = label;
            buttons.Add(button);
            return buttons[buttons.Count - 1];
        }

        public void Start()
        {
            inputThread = new Thread(InputThread);
            Console.WriteLine("");
            Console.WriteLine("space = toggle, esc = end input");
            Console.WriteLine("");
            initialX = Console.CursorLeft;
            initialY = Console.CursorTop;
            input = true;
            inputThread.Start();
            RedrawUi();
        }

        public void RedrawUi()
        {
            for (int i = 0; i < toggles.Count; i++)
            {
                toggles[i].Update();
            }
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].Update();
            }
        }

        public void UpdateToggles()
        {
            for(int i = 0; i < toggles.Count; i++)
            {
                toggles[i].UpdateValue(x, y);
                toggles[i].Update();
            }
        }

        public void UpdateButtons()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].UpdateValue(x, y);
                buttons[i].Update();
            }
        }

        public void InputThread()
        {
            while (input)
            {
                ConsoleKeyInfo k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.Escape)
                {
                    ConsoleUiInputFinishedEvent();
                    return;
                }
                else if (k.Key == ConsoleKey.DownArrow)
                {
                    y++;
                }
                else if (k.Key == ConsoleKey.UpArrow)
                {
                    y--;
                }
                else if (k.Key == ConsoleKey.LeftArrow)
                {
                    x--;
                }
                else if (k.Key == ConsoleKey.RightArrow)
                {
                    x++;
                }
                else if (k.Key == ConsoleKey.Spacebar || k.Key == ConsoleKey.Enter)
                {
                    UpdateToggles();
                    UpdateButtons();
                }

                if (x < 0) x = 0;
                else if (x >= Console.WindowWidth) x = Console.WindowWidth - 1;
                else if (y < 0) y = 0;
                Console.SetCursorPosition(initialX + x, initialY + y);
            }
        }
    }

    public class ConsoleUiToggleEventArgs
    {
        public bool value = false;

        public ConsoleUiToggleEventArgs(bool value)
        {
            this.value = value;
        }
    }

    public class ConsoleUiToggle
    {
        public int xStart = 0;
        public int yStart = 0;
        public int xActionOffset = 1;
        public bool value = false;
        public string label = "";
        public ConsoleUiController controller = null;

        public delegate void ConsoleUiToggleEvent(ConsoleUiToggleEventArgs args);
        public event ConsoleUiToggleEvent ConsoleUiToggleToggledEvent;

        public ConsoleUiToggle(ConsoleUiController controller)
        {
            this.controller = controller;
        }

        public void Update()
        {
            Console.SetCursorPosition(xStart + controller.initialX, yStart + controller.initialY);
        }

        public void UpdateValue(int x, int y)
        {
            if (x == xStart + xActionOffset && y == yStart)
            {
                value = !value;
                ConsoleUiToggleToggledEvent(new ConsoleUiToggleEventArgs(value));
            }
        }
    }

    public class ConsoleUiButton
    {
        public int xStart = 0;
        public int yStart = 0;
        public string label = "";
        public ConsoleUiController controller = null;

        public delegate void ConsoleUiButtonEvent();
        public event ConsoleUiButtonEvent ConsoleUiButtonPressed;

        public ConsoleUiButton(ConsoleUiController controller)
        {
            this.controller = controller;
        }

        public void Update()
        {
            Console.SetCursorPosition(xStart + controller.initialX, yStart + controller.initialY);
            Console.Write("[" + label + "] ");
        }

        public void UpdateValue(int x, int y)
        {
            if (x > xStart && x < xStart + 2 + label.Length && y == yStart)
            {
                ConsoleUiButtonPressed();
            }
        }
    }

    public class BaseUiElement
    {
        public int currentLine = 0;
        public int lastLength = 0;

        public void ClearCurrentLine()
        {
            Console.SetCursorPosition(0, currentLine);
            Console.Write(new string(' ', lastLength));
        }

        public void StoreCurrentLineLength()
        {
            lastLength = Console.CursorLeft;
        }
    }

    public class UndefinedEndProgressBar : BaseUiElement
    {
        public static string[] characters = new string[] { "|", "/", "-", "\\", "|", "/", "-", "\\" };
        public int currentIndex = 0;
        public Thread spinningWheelThread = null;
        public string currentText = "";

        public void Start()
        {
            currentLine = Console.CursorTop;
        }

        public void SetupSpinningWheel(int msPerSpin)
        {
            spinningWheelThread = new Thread(() =>
            {
                while(true)
                {
                    UpdateProgress(currentText);
                    Thread.Sleep(msPerSpin);
                }
            });
            spinningWheelThread.Start();
        }

        public void StopSpinningWheel()
        {
            spinningWheelThread.Abort();
            Console.WriteLine();
        }

        public void UpdateProgress(string task, bool NextLine = false)
        {
            if(NextLine)
            {
                Console.WriteLine();
                Start();
            } else
            {
                ClearCurrentLine();
            }
            currentText = task;
            Console.SetCursorPosition(2, currentLine);
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write(characters[currentIndex] + " ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(task);
            currentIndex++;
            if (currentIndex >= characters.Length) currentIndex = 0;
            StoreCurrentLineLength();
        }
    }

    public class ProgressBarUI : BaseUiElement
    {
        public int ProgressbarLength = 30;
        public double UpdateRate = 0.5;
        public long done = 0;
        public long total = 0;

        public void Start()
        {
            currentLine = Console.CursorTop;
        }

        public void UpdateProgress(int done, long total, string doneText = "", string totalText = "", string extraText = "")
        {
            UpdateProgress((long)done, (long)total, doneText, totalText, extraText);
        }

        public void UpdateProgress(long done, long total, string doneText = "", string totalText = "", string extraText = "")
        {
            this.done = done;
            this.total = total;
            ClearCurrentLine();
            double percentage = done / (double)total;
            Console.SetCursorPosition(2, currentLine);
            for (int i = 0; i < ProgressbarLength; i++)
            {
                double localPercentage = (double)i / ProgressbarLength;
                Console.ForegroundColor = ConsoleColor.Blue;
                if (localPercentage < percentage) Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write("█");
            }
            Console.Write(" ");
            if(doneText != "" && totalText != "") Console.Write(doneText + " / " + totalText + "   ");
            Console.Write(extraText);
            StoreCurrentLineLength();
        }
    }

    public class DownloadProgressUI
    {
        public bool StartDownload(string downloadLink, string destination, bool showETA = true)
        {
            return DownloadThreadHandler(downloadLink, destination, showETA).Result;
        }

        public async Task<bool> DownloadThreadHandler(string downloadLink, string destination, bool showETA = true)
        {
            bool completed = false;
            bool success = false;
            Thread t = new Thread(() =>
            {
                success = DownloadThread(downloadLink, destination, showETA).Result;
                completed = true;
            });
            t.Start();
            while (!completed)
            {
                await DelayCheck();
            }
            return success;
        }

        public async Task<bool> DownloadThread(string downloadLink, string destination, bool showETA = true)
        {
            bool completed = false;
            bool success = false;
            Logger.Log("Downloading " + Path.GetFileName(destination) + " from " + downloadLink + " to " + destination);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Downloading ");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(downloadLink);
            Console.ForegroundColor = ConsoleColor.White;
            int currentLine = Console.CursorTop;
            WebClient c = new WebClient();
            SizeConverter s = new SizeConverter();
            DateTime lastUpdate = DateTime.Now;
            bool locked = false;
            int lastLength = 0;
            long lastBytes = 0;
            ProgressBarUI progressBar = new ProgressBarUI();
            progressBar.Start();
            List<long> lastBytesPerSec = new List<long>();
            long BytesToRecieve = 0;
            c.DownloadProgressChanged += (o, e) =>
            {
                if (locked) return;
                locked = true;
                double secondsPassed = (DateTime.Now - lastUpdate).TotalSeconds;
                if (secondsPassed >= progressBar.UpdateRate)
                {
                    BytesToRecieve = e.TotalBytesToReceive;
                    string current = s.ByteSizeToString(e.BytesReceived);
                    string total = s.ByteSizeToString(BytesToRecieve);
                    long bytesPerSec = (long)Math.Round((e.BytesReceived - lastBytes) / secondsPassed);
                    lastBytesPerSec.Add(bytesPerSec);
                    if (lastBytesPerSec.Count > 5) lastBytesPerSec.RemoveAt(0);
                    lastBytes = e.BytesReceived;
                    long avg = 0;
                    foreach (long l in lastBytesPerSec) avg += l;
                    avg = avg / lastBytesPerSec.Count;
                    progressBar.UpdateProgress(e.BytesReceived, BytesToRecieve, current, total, s.ByteSizeToString(bytesPerSec, 0) + "/s" + (showETA ? ("   ETA " + s.SecondsToBetterString((e.TotalBytesToReceive - e.BytesReceived) / avg)) : ""));
                    lastUpdate = DateTime.Now;
                }
                locked = false;
            };
            c.DownloadFileCompleted += (o, e) =>
            {
                if(e.Error == null) success = true;
                Logger.Log("Did download succeed: " + success + (success ? "" : ":\n" + e.ToString()));
                progressBar.UpdateProgress(BytesToRecieve, BytesToRecieve, s.ByteSizeToString(BytesToRecieve), s.ByteSizeToString(BytesToRecieve), success ? "Finished" : "An error occured");
                completed = true;
                Console.WriteLine();
            };
            
            c.DownloadFileAsync(new Uri(downloadLink), destination);
            while (!completed)
            {
                await DelayCheck();
            }
            return success;
        }

        public async Task DelayCheck()
        {
            var frame = new DispatcherFrame();
            new Thread((ThreadStart)(() =>
            {
                Thread.Sleep(100);
                frame.Continue = false;
            })).Start();
            Dispatcher.PushFrame(frame);
        }
    }
}