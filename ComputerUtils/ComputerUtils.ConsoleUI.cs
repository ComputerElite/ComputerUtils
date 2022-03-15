using ComputerUtils.Logging;
using ComputerUtils.Timing;
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
            string f = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.White;
            return f;
        }

        public static string SecureQuestionString(string question)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(question);
            Console.ForegroundColor = ConsoleColor.Cyan;
            string s = "";
            ConsoleKeyInfo k;
            while((k = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if(k.Key == ConsoleKey.Backspace && s.Length >= 1)
                {
                    Console.Write("\b \b");
                    s = s.Substring(0, s.Length - 1);
                } else if(k.Key != ConsoleKey.Backspace)
                {
                    s += k.KeyChar;
                    Console.Write("*");
                }
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            return s;
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

        /// <summary>
        /// Clears all line from currentLine to the current cursor position.
        /// </summary>
        public void ClearCurrentLine()
        {
            int amount = Console.CursorTop - currentLine + 1;
            for (int i = 0; i < amount; i++)
            {
                Console.SetCursorPosition(0, currentLine + i);
                Console.Write(new string(' ', Console.WindowWidth));
            }
        }

        [Obsolete]
        public void StoreCurrentLineLength()
        {
            lastLength = (Console.CursorTop - currentLine + 1) * Console.WindowWidth;
        }
    }

    public class UndefinedEndProgressBar : BaseUiElement
    {
        public static string[] characters = new string[] { "|", "/", "-", "\\", "|", "/", "-", "\\" };
        public int currentIndex = 0;
        public Thread spinningWheelThread = null;
        public string currentText = "";
        public bool aborted = false;
        public bool started = false;

        public void Start()
        {
            aborted = false;
            currentLine = Console.CursorTop;
            if(spinningWheelThread == null && !aborted && !started) SetupSpinningWheel(500);
            started = true;
        }

        public void SetupSpinningWheel(int msPerSpin)
        {
            if (spinningWheelThread != null) return;
            currentLine = Console.CursorTop;
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
            aborted = true;
            spinningWheelThread.Abort();
            Console.WriteLine();
        }

        public void UpdateProgress(string task, bool NextLine = false)
        {
            if (spinningWheelThread == null && !aborted) SetupSpinningWheel(500);
            if (NextLine)
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
        }
    }

    public class ProgressBarUI : BaseUiElement
    {
        public int ProgressbarLength = 30;
        public double UpdateRate = 0.5;
        public long done = 0;
        public long total = 0;
        public int eTARange = 10;

        public void Start()
        {
            currentLine = Console.CursorTop;
        }

        public void UpdateProgress(int done, long total, string doneText = "", string totalText = "", string extraText = "", bool ETA = false)
        {
            UpdateProgress((long)done, (long)total, doneText, totalText, extraText, ETA);
        }
        public List<long> last = new List<long>();
        public DateTime lastUpdate = DateTime.Now;
        public void UpdateProgress(long done, long total, string doneText = "", string totalText = "", string extraText = "", bool ETA = false)
        {
            if (ETA)
            {
                long lastPerSec = (long)Math.Round((done - this.done) / (DateTime.Now - lastUpdate).TotalSeconds);
                last.Add(lastPerSec);
                if (last.Count > eTARange) last.RemoveAt(0);
                long avg = 0;
                foreach (long l in last) avg += l;
                avg = avg / last.Count;
                extraText += "  ETA " + (avg == 0 ? "N/A" : SizeConverter.SecondsToBetterString((total - done) / avg));
            }
            lastUpdate = DateTime.Now;
            this.done = done;
            this.total = total;
            ClearCurrentLine();
            double percentage = (double)done / (double)total;
            Console.SetCursorPosition(2, currentLine);
            for (int i = 1; i <= ProgressbarLength; i++)
            {
                double localPercentage = (double)i / ProgressbarLength;
                Console.ForegroundColor = ConsoleColor.Blue;
                if (localPercentage <= percentage) Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write("█");
            }
            Console.Write(" ");
            if(doneText != "" && totalText != "") Console.Write(doneText + " / " + totalText + "   ");
            Console.Write(extraText);
        }
    }

    public class DownloadProgressUI 
    {
        public bool StartDownload(string downloadLink, string destination, bool logLink = true, bool showETA = true, Dictionary<string, string> headers = null, bool clearAfterwads = false)
        {
            return DownloadThreadHandler(downloadLink, destination, logLink, showETA, headers, clearAfterwads).Result;
        }

        public async Task<bool> DownloadThreadHandler(string downloadLink, string destination, bool logLink = true, bool showETA = true, Dictionary<string, string> headers = null, bool clearAfterwads = false)
        {
            bool completed = false;
            bool success = false;
            Thread t = new Thread(() =>
            {
                success = DownloadThread(downloadLink, destination, logLink, showETA, headers, clearAfterwads).Result;
                completed = true;
            });
            t.Start();
            while (!completed)
            {
                await TimeDelay.DelayWithoutThreadBlock(100);
            }
            if(success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success");
            } else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error while downloading");
            }
            Console.ForegroundColor = ConsoleColor.White;
            return success;
        }

        public async Task<bool> DownloadThread(string downloadLink, string destination, bool logLink = true, bool showETA = true, Dictionary<string, string> headers = null, bool clearAfterwads = false)
        {
            bool completed = false;
            bool success = false;
            int currentLine = Console.CursorTop;
            Logger.Log("Downloading " + Path.GetFileName(destination) + " from " + (logLink ? downloadLink : "hidden") + " to " + destination);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Downloading ");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(Logger.CensorString(downloadLink));
            Console.ForegroundColor = ConsoleColor.White;
            WebClient c = new WebClient();
           
            bool locked = false;
            long lastBytes = 0;
            ProgressBarUI progressBar = new ProgressBarUI();
            progressBar.eTARange = 20;
            DateTime lastUpdate = DateTime.MinValue;
            progressBar.Start();
            List<long> lastBytesPerSec = new List<long>();
            long BytesToRecieve = 0;
            progressBar.UpdateProgress(0, 1, "0", "0", "Download started");
            c.DownloadProgressChanged += (o, e) =>
            {
                if (locked) return;
                
                locked = true;
                double secondsPassed = (DateTime.Now - lastUpdate).TotalSeconds;
                if (secondsPassed >= progressBar.UpdateRate)
                {
                    BytesToRecieve = e.TotalBytesToReceive;
                    string current = SizeConverter.ByteSizeToString(e.BytesReceived);
                    string total = SizeConverter.ByteSizeToString(BytesToRecieve);
                    long bytesPerSec = (long)Math.Round((e.BytesReceived - lastBytes) / secondsPassed);
                    lastBytesPerSec.Add(bytesPerSec);
                    if (lastBytesPerSec.Count > 5) lastBytesPerSec.RemoveAt(0);
                    lastBytes = e.BytesReceived;
                    long avg = 0;
                    foreach (long l in lastBytesPerSec) avg += l;
                    avg = avg / lastBytesPerSec.Count;
                    progressBar.UpdateProgress(e.BytesReceived, BytesToRecieve, current, total, SizeConverter.ByteSizeToString(bytesPerSec, 0) + "/s", true);
                    lastUpdate = DateTime.Now;
                }
                locked = false;
            };
            c.DownloadFileCompleted += (o, e) =>
            {
                if(e.Error == null) success = true;
                Logger.Log("Did download succeed: " + success + (success ? "" : ":\n" + e.Error.ToString()));
                progressBar.UpdateProgress(BytesToRecieve, BytesToRecieve, SizeConverter.ByteSizeToString(BytesToRecieve), SizeConverter.ByteSizeToString(BytesToRecieve), success ? "Finished" : "An error occured");
                completed = true;
                Console.WriteLine();
                if (clearAfterwads)
                {
                    progressBar.currentLine = currentLine;
                    progressBar.ClearCurrentLine();
                }
            };
            if(headers != null)
            {
                foreach(KeyValuePair<string, string> h in headers)
                {
                    c.Headers[h.Key] = h.Value;
                }
            }
            c.DownloadFileAsync(new Uri(downloadLink), destination);
            while (!completed)
            {
                await TimeDelay.DelayWithoutThreadBlock(100);
            }
            return success;
        }
    }
}