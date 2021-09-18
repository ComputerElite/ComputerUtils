using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ComputerUtils.RandomExtensions;

namespace ComputerUtils.RandomExtensions
{
    public class RandomExtension
    {
        public static Random random = new Random();

        public bool NextBool()
        {
            return random.NextDouble() <= 0.5;
        }

        public static String Pick(List<String> array)
        {
            return array[random.Next(array.Count)];
        }

        public static String Pick(String[] array)
        {
            return array[random.Next(array.Length)];
        }
    }

    public class EightBall
    {
        public static List<String> responses = new List<String>() { "It is certain.", "It is decidedly so.", "Without a doubt.", "Yes – definitely.", "You may rely on it.", "As I see it, yes.", "Most likely.", "Outlook good.", "Yes.", "Signs point to yes.", "Reply hazy, try again.", "Ask again later.", "Better not tell you now.", "Cannot predict now.", "Concentrate and ask again.", "Don’t count on it.", "My reply is no.", "My sources say no.", "Outlook not so good.", "Very doubtful." };

        public static String returnMsg()
        {
            return RandomExtension.Pick(responses);
        }
    }
}

namespace ComputerUtils.Encryption
{
    public class Encrypter
    {
        public Tuple<byte[], byte[]> EncryptOTP(byte[] input)
        {
            byte[] output = new byte[input.Length];
            byte[] key = new byte[input.Length];
            BitArray i = new BitArray(input);
            Random rnd = new Random();
            rnd.NextBytes(key);
            BitArray k = new BitArray(key);
            i.Xor(k);
            i.CopyTo(output, 0);
            k.CopyTo(key, 0);
            return new Tuple<byte[], byte[]>(output, key);
        }

        public void EncryptFileOTP(String file, String outputDirectory, bool overrideSourceFile = false, bool useLowMem = false, bool outputToConsole = true, int batches = 1000000)
        {
            String exe = AppDomain.CurrentDomain.BaseDirectory;
            if (useLowMem)
            {
                FileStream ifile = new FileStream(file, FileMode.Open);

                File.Delete(outputDirectory + Path.GetFileName(file) + ".key");
                File.Delete(outputDirectory + Path.GetFileName(file));
                FileStream kfile = new FileStream(outputDirectory + Path.GetFileName(file) + ".key", FileMode.Append);
                FileStream ofile = new FileStream(exe + Path.GetFileName(file), FileMode.Append);
                if (outputToConsole) Console.Write("0/0 (100%)");
                for (int i = 1; i * batches < ifile.Length + batches; i++)
                {
                    int adjusted = i * batches < ifile.Length ? batches : (int)(ifile.Length % batches);
                    byte[] tmp1 = new byte[adjusted];
                    for (int ii = 0; ii < adjusted; ii++)
                    {
                        tmp1[ii] = (byte)ifile.ReadByte();
                    }

                    //EncryptOTP
                    byte[] output = new byte[tmp1.Length];
                    byte[] key = new byte[tmp1.Length];
                    BitArray inp = new BitArray(tmp1);
                    Random rnd = new Random();
                    rnd.NextBytes(key);
                    BitArray k = new BitArray(key);
                    inp.Xor(k);
                    inp.CopyTo(output, 0);
                    k.CopyTo(key, 0);

                    ofile.Write(output, 0, output.Length);
                    kfile.Write(key, 0, key.Length);
                    if (outputToConsole) Console.Write("\r" + (i * batches) + "/" + ifile.Length + " (" + ((double)i * (double)batches / ifile.Length * 100) + " %)" + new string(' ', Console.WindowWidth - ((i * batches) + "/" + ifile.Length + " (" + ((double)i * (double)batches / ifile.Length * 100) + " %)").Length - 1));
                }
                if (outputToConsole) Console.WriteLine();
                if (outputToConsole) Console.WriteLine("flushing");
                ofile.Flush();
                kfile.Flush();
                if (outputToConsole) Console.WriteLine("Closing files");
                ofile.Close();
                kfile.Close();
                ifile.Close();
            }
            else
            {
                Console.WriteLine("Started Encryption, please wait.");
                byte[] fileContents = File.ReadAllBytes(file);

                //EncryptOTP
                byte[] output = new byte[fileContents.Length];
                byte[] key = new byte[fileContents.Length];
                BitArray inp = new BitArray(fileContents);
                Random rnd = new Random();
                rnd.NextBytes(key);
                BitArray k = new BitArray(key);
                inp.Xor(k);
                inp.CopyTo(output, 0);
                k.CopyTo(key, 0);

                File.WriteAllBytes(outputDirectory + Path.GetFileName(file) + ".key", key);
                File.WriteAllBytes(exe + Path.GetFileName(file), output);
            }
            if (overrideSourceFile) File.Delete(file);
            File.Move(exe + Path.GetFileName(file), outputDirectory + Path.GetFileName(file));
        }
    }

    public class Tools
    {
        public bool XOR(bool inone, bool intwo)
        {
            //Console.WriteLine("in1: " + (inone ? "1" : "0") + " in2: " + (intwo ? "1" : "0") + " out: " + (inone && intwo || !inone && !intwo ? "0" : "1"));
            if (inone && intwo || !inone && !intwo) return false;
            else return true;
        }
    }

    public class Decrypter
    {
        public byte[] DecryptOTP(Byte[] input, Byte[] key)
        {
            byte[] output = new byte[input.Length];
            BitArray i = new BitArray(input);
            BitArray k = new BitArray(key);
            i.Xor(k);
            i.CopyTo(output, 0);
            return output;
        }

        public void DecryptOTPFile(String file, String keyFile, String outputDirectory, bool useLowMem = false, bool outputToConsole = true, int batches = 1000000)
        {
            if (useLowMem)
            {
                File.Delete(outputDirectory + Path.GetFileNameWithoutExtension(keyFile));
                FileStream ifile = new FileStream(file, FileMode.Open);
                FileStream kfile = new FileStream(keyFile, FileMode.Open);
                FileStream ofile = new FileStream(outputDirectory + Path.GetFileNameWithoutExtension(keyFile), FileMode.Append);
                for (int i = 1; i * batches < ifile.Length + batches; i++)
                {
                    int adjusted = i * batches < ifile.Length ? batches : (int)(ifile.Length % batches);
                    byte[] tmp11 = new byte[adjusted];
                    byte[] tmp12 = new byte[adjusted];
                    Console.WriteLine(tmp11.Length);
                    for (int ii = 0; ii < adjusted; ii++)
                    {
                        tmp11[ii] = (byte)ifile.ReadByte();
                        tmp12[ii] = (byte)kfile.ReadByte();
                    }

                    //DecryptOTP
                    byte[] output1 = new byte[tmp11.Length];
                    BitArray inp = new BitArray(tmp11);
                    BitArray k = new BitArray(tmp12);
                    inp.Xor(k);
                    inp.CopyTo(output1, 0);

                    ofile.Write(output1, 0, output1.Length);
                    if(outputToConsole) Console.Write("\r" + (i * batches) + "/" + ifile.Length + " (" + ((double)i * (double)batches / ifile.Length * 100) + " %)" + new string(' ', Console.WindowWidth - ((i * batches) + "/" + ifile.Length + " (" + ((double)i * (double)batches / ifile.Length * 100) + " %)").Length - 1));
                }
                ofile.Flush();
                ifile.Close();
                kfile.Close();
                ofile.Close();
            }
            else
            {
                if (outputToConsole) Console.WriteLine("Started Decryption, please wait.");
                byte[] fileContents = File.ReadAllBytes(file);
                byte[] keyFileContents = File.ReadAllBytes(keyFile);

                //DecryptOPT
                byte[] output = new byte[fileContents.Length];
                BitArray i = new BitArray(fileContents);
                BitArray k = new BitArray(keyFileContents);
                i.Xor(k);
                i.CopyTo(output, 0);

                File.WriteAllBytes(outputDirectory + Path.GetFileNameWithoutExtension(keyFile), output);
            }
        }
    }

    public class Header
    {
        public string fileName { get; set; } = "name";
        public string extension { get; set; } = "ext";
    }
}

namespace ComputerUtils.RegexTemplates
{
    public class RegexTemplates
    {
        public static bool IsIP(String input)
        {
            return Regex.IsMatch(input, "((2(5[0-5]|[0-4][0-9])|1?[0-9]?[0-9])\\.){3}(2(5[0-5]|[0-4][0-9])|1?[0-9]?[0-9])");
        }

        public static String GetIP(String input)
        {
            Match found = Regex.Match(input, "((2(5[0-5]|[0-4][0-9])|1?[0-9]?[0-9])\\.){3}(2(5[0-5]|[0-4][0-9])|1?[0-9]?[0-9])");
            if (!found.Success) return "";
            return found.Value;
        }

        public static bool IsDiscordInvite(String input)
        {
            return Regex.IsMatch(input, "(https?://)?(www.)?(discord.(gg|io|me|li)|discordapp.com/invite)/.+[a-zA-Z0-9]");
        }

        public static String GetDiscordInvite(String input)
        {
            Match found = Regex.Match(input, "(https ?://)?(www.)?(discord.(gg|io|me|li)|discordapp.com/invite)/.+[a-zA-Z0-9]");
            if (!found.Success) return "";
            return found.Value;
        }
    }
}

namespace ComputerUtils.VarUtils
{
    public class Combinator
    {
        public String[] Combinate(String[] arr)
        {
            List<String> output = new List<String>();
            if (arr.Length == 1)
            {
                return arr;
            }
            else if (arr.Length == 2)
            {
                output.Add(arr[0] + arr[1]);
                output.Add(arr[1] + arr[0]);
            }
            else
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    List<String> tmp = new List<string>(arr);
                    tmp.RemoveAt(i);
                    String[] combined = Combinate(tmp);
                    for (int c = 0; c < combined.Length; c++)
                    {
                        output.Add(arr[i] + combined[c]);
                    }
                }
            }
            return output.ToArray();
        }

        public String[] Combinate(List<string> arr)
        {
            return Combinate(arr.ToArray());
        }

        public int[] Combinate(List<int> arr)
        {
            return Array.ConvertAll(Combinate(Array.ConvertAll(arr.ToArray(), s => s.ToString())), i => int.Parse(i));
        }
    }
}