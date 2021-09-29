using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using ComputerUtils.Encryption;
using System.Windows.Forms;
using System.Windows;
using ComputerUtils.Data;
using ComputerUtils.VarUtils;
using ComputerUtils.ConsoleUi;
using System.Threading;
using ComputerUtils.Webserver;
using ComputerUtils.Updating;

namespace ComputerUtils_Tester
{
    class Program
    {
        static String exe = AppDomain.CurrentDomain.BaseDirectory;

        [STAThread]
        static void Main(string[] args)
        {
            UpdateTest();
            Console.WriteLine("\nPress any key to exit");
            Console.ReadLine();
        }

        static void UpdateTest()
        {
            Updater u = new Updater("1.3.1", "https://github.com/ComputerElite/Rift-downgrader", "Rift Downgrader");
            u.CheckUpdate();
        }

        static void WebserverTest()
        {
            Table t = new Table();
            Column a = new Column();
            a.header = "Produkt";
            a.entries = new List<object>() { "Test 1", "Test2" };
            Column b = new Column();
            b.header = "beschreibung";
            b.entries = new List<object>() { "auauauaa", "aaaaaaa" };
            t.colums.Add(a);
            t.colums.Add(b);
            Console.WriteLine(t);
        }

        static void ProgressUITest()
        {
            UndefinedEndProgressBar p = new UndefinedEndProgressBar();
            p.Start();
            p.SetupSpinningWheel(200);
            Thread.Sleep(4000);
            p.StopSpinningWheel();
        }

        static void DownloadUITest()
        {
            DownloadProgressUI d = new DownloadProgressUI();
            d.StartDownload("https://github.com/ComputerElite/BM/releases/download/1.15/BM.zip", "D:\\test.zip");
            Console.WriteLine("Download finished");
        }

        static void ConsoleUITest()
        {
            ConsoleUiController uiController = new ConsoleUiController();
            uiController.ConsoleUiInputFinishedEvent += () =>
            {
                Console.WriteLine();
                Console.WriteLine("Input finished");
                Console.ReadLine();
            };
            bool toggleValue = false;
            uiController.AddUiToggle(0, 2, "woah a toggle").ConsoleUiToggleToggledEvent += (e) =>
            {
                toggleValue = e.value;
            };
            uiController.AddUiButton(0, 4, "woah a button").ConsoleUiButtonPressed += () =>
            {
                Console.WriteLine("button pressed");
            };
            uiController.Start();
        }

        static void Base64()
        {
            String r = "VkZkd2FsQlJiejBL";
            for (int i = 0; i < 5; i++)
            {

                String tmp = "";
                foreach (char c in Convert.FromBase64String(r)) tmp += c;

                //r = Convert.ToBase64String(Encoding.UTF8.GetBytes(r));
                r = tmp;
                Console.WriteLine(r + "\n");
            }
        }

        static void EncryptionTest()
        {
            Console.WriteLine("Test the method:");
            String i = Console.ReadLine();
            Byte[] input = new byte[i.Length];
            String inputv = "";
            for (int c = 0; c < i.Length; c++)
            {
                input[c] = Convert.ToByte(i[c]);
                Console.WriteLine(input[c].ToString());
                inputv += Convert.ToString(input[c], 2).PadLeft(8);
                inputv += " ";
            }

            Encrypter e = new Encrypter();
            Tuple<byte[], byte[]> o = e.EncryptOTP(input);

            String key = "";
            String output = "";
            for (int c = 0; c < o.Item1.Length; c++)
            {
                key += Convert.ToString(o.Item2[c], 2).PadLeft(8);
                output += Convert.ToString(o.Item1[c], 2).PadLeft(8);
                key += " ";
                output += " ";
            }

            Console.WriteLine("Input:  " + inputv);
            Console.WriteLine("key:    " + key);
            Console.WriteLine("output: " + output);

            Console.WriteLine("Swapping output and input");

            Decrypter d = new Decrypter();
            byte[] output2 = d.DecryptOTP(o.Item1, o.Item2);

            String o2 = "";
            for (int c = 0; c < o.Item1.Length; c++)
            {
                o2 += Convert.ToString(output2[c], 2).PadLeft(8);
                o2 += " ";
            }

            Console.WriteLine("Input:  " + output);
            Console.WriteLine("key:    " + key);
            Console.WriteLine("output: " + o2);
            Console.WriteLine("Matching: " + (o2 == inputv));
        }

        static void RandBool()
        {
            int neg = 0;
            List<int> randomnumbers = new List<int>();
            Random r = new Random();
            int max = r.Next(50, 200);
            for (int i = 0; i < max; i++)
            {
                randomnumbers.Add(r.Next(-500, 500));
                Console.WriteLine("number #" + (i + 1) + ": " + randomnumbers[i]);
            }
            foreach (int n in randomnumbers)
            {
                if (n < 0) neg++;
            }
            Console.WriteLine("positive numbers: " + (randomnumbers.Count - neg) + " out of " + randomnumbers.Count + " numbers.");
            Console.WriteLine("negative numbers: " + neg + " out of " + randomnumbers.Count + " numbers.");
        }
    }
}
