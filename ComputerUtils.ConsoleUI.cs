using ComputerUtils.VarUtils;
using System;
using System.Collections.Generic;
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

    public class ProgressBarUI
    {
        public int ProgressbarLength = 30;
        public double UpdateRate = 0.5;
        public int currentLine = 0;
        public int lastLength = 0;

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
            double percentage = done / (double)total;
            Console.SetCursorPosition(0, currentLine);
            Console.Write(new string(' ', lastLength));
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
            lastLength = Console.CursorLeft;
        }
    }

    public class DownloadProgressUI
    {
        public bool StartDownload(string downloadLink, string destination)
        {
            return DownloadThreadHandler(downloadLink, destination).Result;
        }

        public async Task<bool> DownloadThreadHandler(string downloadLink, string destination)
        {
            bool completed = false;
            bool success = false;
            Thread t = new Thread(() =>
            {
                success = DownloadThread(downloadLink, destination).Result;
                completed = true;
            });
            t.Start();
            while (!completed)
            {
                await DelayCheck();
            }
            return success;
        }

        public async Task<bool> DownloadThread(string downloadLink, string destination)
        {
            bool completed = false;
            bool success = false;
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
            c.DownloadProgressChanged += (o, e) =>
            {
                if (locked) return;
                locked = true;
                double secondsPassed = (DateTime.Now - lastUpdate).TotalSeconds;
                if (secondsPassed >= progressBar.UpdateRate)
                {
                    string current = s.ByteSizeToString(e.BytesReceived);
                    string total = s.ByteSizeToString(e.TotalBytesToReceive);
                    long bytesPerSec = (long)Math.Round((e.BytesReceived - lastBytes) / secondsPassed);
                    lastBytes = e.BytesReceived;

                    progressBar.UpdateProgress(e.BytesReceived, e.TotalBytesToReceive, current, total, s.ByteSizeToString(bytesPerSec) + "/s");
                    lastUpdate = DateTime.Now;
                }
                locked = false;
            };
            c.DownloadFileCompleted += (o, e) =>
            {
                if(e.Error == null) success = true;
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
                Thread.Sleep(200);
                frame.Continue = false;
            })).Start();
            Dispatcher.PushFrame(frame);
        }
    }
}