using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Libraries.EzeDbCommon
{
    public class SupportFileFactory
    {
        public delegate void MessageCallback(string message);
        public event MessageCallback FileLoadMessages;

        private static SupportFileFactory _instance;

        private SupportFileFactory()
        {
        }

        public static SupportFileFactory Instance
        {
            get
            {
                lock (typeof(SupportFileFactory))
                {
                    if (_instance == null)
                    {
                        _instance = new SupportFileFactory();
                    }
                    return _instance;
                }
            }
        }

        public string FullPath(string fileName)
        {
            SortedList<DateTime, string> filesByModifiedTime = new SortedList<DateTime, string>();

            Match absoluteMatch = Regex.Match(fileName, string.Format(@"^([A-Za-z]\{0})?\{1}(.*)", Path.VolumeSeparatorChar, Path.DirectorySeparatorChar));
            if (absoluteMatch.Success)
            {
                if (File.Exists(fileName))
                {
                    Log("not doing extra lookup on " + fileName);
                    return fileName;
                }
                Log("ignoring root in " + fileName);
                string result = FullPath(absoluteMatch.Groups[2].Value);
                if (result == null)
                {
                    Log("still not found, ignoring path in " + fileName);
                    fileName = Path.GetFileName(fileName);
                }
                else
                {
                    return result;
                }
            }

            string workingFile = fileName;
            if (File.Exists(workingFile))
            {
                filesByModifiedTime.Add(File.GetLastWriteTime(workingFile), workingFile);
            }

            workingFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            AddFileToDictionary(filesByModifiedTime, workingFile);
#if DEBUG
            workingFile = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".." + Path.DirectorySeparatorChar + ".."), fileName);
            AddFileToDictionary(filesByModifiedTime, workingFile);
            workingFile = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + "Libraries" + Path.DirectorySeparatorChar + "DbTools" + Path.DirectorySeparatorChar), fileName);
            AddFileToDictionary(filesByModifiedTime, workingFile);
            workingFile = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + "Libraries" + Path.DirectorySeparatorChar + "DbTools" + Path.DirectorySeparatorChar), fileName);
            AddFileToDictionary(filesByModifiedTime, workingFile);
#endif
            if (filesByModifiedTime.Count == 0)
            {
                workingFile = Path.Combine(@"C:\3VR\bin\running", fileName);
                AddFileToDictionary(filesByModifiedTime, workingFile);
            }

            if (filesByModifiedTime.Count > 0)
            {
                workingFile = filesByModifiedTime.Values[filesByModifiedTime.Count - 1];
                if (workingFile != fileName)
                {
                    Log(string.Format("Using {0}.", workingFile));
                }
                return workingFile;
            }

            Log(string.Format("Couldn't locate the support file for {0}.", fileName));
            return fileName;
        }

        private void Log(string message)
        {
            if (FileLoadMessages != null)
            {
                FileLoadMessages("SupportFileFactory: " + message);
            }
        }

        private void AddFileToDictionary(SortedList<DateTime, string> filesByModifiedTime, string workingFile)
        {
            FileInfo file = new FileInfo(workingFile);
            if (file.Exists)
            {
                if (!filesByModifiedTime.ContainsKey(file.LastWriteTime))
                {
                    filesByModifiedTime.Add(file.LastWriteTime, file.FullName);
                }
            }
        }
    }
}

