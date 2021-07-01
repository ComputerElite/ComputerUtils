using ComputerUtils.Logging;
using System;
using System.IO;

namespace ComputerUtils.FileManaging
{
    public class FileManager
    {
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            try
            {
                if (Directory.Exists(destDirName)) Directory.Delete(destDirName, true);
            }
            catch { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Couldn't delete " + destDirName); Console.ForegroundColor = ConsoleColor.White; }

            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                try
                {
                    Console.WriteLine("Copying " + file.Name);
                    Logger.Log("Copying " + file.Name);
                    string tempPath = System.IO.Path.Combine(destDirName, file.Name);
                    file.CopyTo(tempPath, true);
                }
                catch (Exception e) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("ERROR copying " + file.Name); Console.ForegroundColor = ConsoleColor.White; Logger.Log("Error copying " + file.Name + ": " + e.ToString(), LoggingType.Error); }
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = System.IO.Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        public static void CreateDirectoryIfNotExisting(string path)
        {
            if (!Directory.Exists(path))
            {
                Logger.Log("Creating " + path);
                Directory.CreateDirectory(path);
            }
        }

        public static void RecreateDirectoryIfExisting(string path)
        {
            if (Directory.Exists(path))
            {
                Logger.Log("Deleting " + path);
                Directory.Delete(path, true);
            }
            Logger.Log("Creating " + path);
            Directory.CreateDirectory(path);
        }
    }
}