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
using MessageBox = System.Windows.MessageBox;
using ComputerUtils.Data;
using ComputerUtils.VarUtils;
using ComputerUtils.ConsoleUi;
using System.Threading;

namespace ComputerUtils
{
    class Program
    {
        static String exe = AppDomain.CurrentDomain.BaseDirectory;

        [STAThread]
        static void Main(string[] args)
        {
            ProgressUITest();
            Console.WriteLine("\nPress any key to exit");
            Console.ReadLine();
        }

        static void ProgressUITest()
        {
            UndefinedEndProgressBar p = new UndefinedEndProgressBar();
            p.Start();
            for(int i = 0; i < 300000; i++)
            {
                p.UpdateProgress(i.ToString());
            }
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

        static void FileXOR()
        {
            Console.Write("First file (*.*): ");
            String file = "";
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All (*.*) | *.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(ofd.FileName)) file = ofd.FileName;
                else return;
            }
            else return;
            Console.WriteLine(file);

            Console.Write("Second File (*.*): ");
            String keyFile = "";
            ofd.Filter = "All (*.*) | *.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(ofd.FileName)) keyFile = ofd.FileName;
                else return;
            }
            else return;
            Console.WriteLine(keyFile);

            Console.WriteLine("Adjusting File sizes");
            Random rnd = new Random();
            long fileSize = new FileInfo(file).Length;
            long keyFileSize = new FileInfo(keyFile).Length;
            byte[] random = new byte[fileSize < keyFileSize ? keyFileSize - fileSize : fileSize - keyFileSize];
            rnd.NextBytes(random);
            FileStream fs = new FileStream(fileSize < keyFileSize ? file : keyFile, FileMode.Append);
            fs.Write(random, 0, random.Length);
            fs.Flush();
            fs.Close();

            Console.WriteLine("Adjusted File size. Appended " + random.Length + " bytes to " + (fileSize < keyFileSize ? "First File" : "Second File"));

            MessageBoxResult r = MessageBox.Show("Do you want to use performance mode (yes, RAM usage of about 4*FinishedFileSize) or minimim RAM mode (no, Time about 3*TimeNeeded by performancemode)?", "OTP Decrypter", MessageBoxButton.YesNo, MessageBoxImage.Question);

            Decrypter d = new Decrypter();
            Stopwatch s = Stopwatch.StartNew();
            d.DecryptOTPFile(file, keyFile, exe, r == MessageBoxResult.No);

            Console.WriteLine();
            s.Stop();
            Console.WriteLine("Key generating took " + s.ElapsedMilliseconds + " ms with a byte array of length " + new FileInfo(file).Length);
        }

        static void Decrypt()
        {
            Console.Write("File to decrypt (*.encr): ");
            String file = "";
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Encrypted files (*.encr) | *.encr; *.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(ofd.FileName)) file = ofd.FileName;
                else return;
            }
            else return;
            Console.WriteLine(file);

            Console.Write("Key (*.encrkey): ");
            String keyFile = "";
            ofd.Filter = "Keys (*.encrkey) | *.encrkey; *.*";
            if ( ofd.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(ofd.FileName)) keyFile = ofd.FileName;
                else return;
            }
            else return;
            Console.WriteLine(keyFile);

            MessageBoxResult r = MessageBox.Show("Do you want to use performance mode (yes, RAM usage of about 4*FinishedFileSize) or minimim RAM mode (no, Time about 3*TimeNeeded by performancemode)?", "OTP Decrypter", MessageBoxButton.YesNo, MessageBoxImage.Question);

            Decrypter d = new Decrypter();
            Stopwatch s = Stopwatch.StartNew();
            d.DecryptOTPFile(file, keyFile, exe, r == MessageBoxResult.No);

            Console.WriteLine();
            s.Stop();
            Console.WriteLine("Decrypting took " + s.ElapsedMilliseconds + " ms with a byte array of length " + new FileInfo(file).Length);
        }

        static void Encrypt()
        {
            Console.Write("File to encrypt (if you don't want to encrypt a file just press abort): ");
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All (*.*) | *.*";
            String file = "";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(ofd.FileName)) file = ofd.FileName;
                else return;
            }
            else return;
            Console.WriteLine(file);

            MessageBoxResult r = MessageBox.Show("Do you want to use performance mode (yes, RAM usage of about 4*FileSize) or minimim RAM mode (no, Time about 3*TimeNeeded by performancemode)?", "OTP Encrypter", MessageBoxButton.YesNo, MessageBoxImage.Question);
            Encrypter e = new Encrypter();
            Stopwatch s = Stopwatch.StartNew();
            e.EncryptFileOTP(file, exe, r == MessageBoxResult.No);

            Console.WriteLine();
            s.Stop();
            Console.WriteLine("Encrypting took " + s.ElapsedMilliseconds + " ms with a byte array of length " + new FileInfo(file).Length);
            //Console.WriteLine("Mem usage: " + Process.GetCurrentProcess().PeakWorkingSet64 + " bytes");
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
