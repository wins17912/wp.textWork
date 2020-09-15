﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace wp.dll.lib32.textWork
{
    public sealed class DiffTextFiles
    {
        private FileInfo      FileA              { get; }
        private FileInfo      FileB              { get; }
        private DirectoryInfo ResultDirectory    { get; }
        private Encoding      SourceFileEncoding { get; }

        private readonly DirectoryInfo _tempDirectoryA;
        private readonly DirectoryInfo _tempDirectoryB;

        public event DifferenceEventHandler NewEvent;

        public DiffTextFiles(FileInfo fileA, FileInfo fileB, DirectoryInfo resultDirectory, Encoding sourceFileEncoding = null)
        {
            FileA              = fileA;
            FileB              = fileB;
            ResultDirectory    = resultDirectory;
            SourceFileEncoding = sourceFileEncoding ?? Encoding.UTF8;

            var tempDirectoryName = $"#{DateTime.Now.Ticks.ToString().Substring(0, 8)}";

            _tempDirectoryA = new DirectoryInfo($"{ResultDirectory.FullName}\\A{tempDirectoryName}");
            OnNewEvent($"tempDirectoryA={_tempDirectoryA.FullName}");

            _tempDirectoryB = new DirectoryInfo($"{ResultDirectory.FullName}\\B{tempDirectoryName}");
            OnNewEvent($"tempDirectoryB={_tempDirectoryB.FullName}");
        }

        public void Work()
        {
            OnNewEvent("Work started");

            if (!ResultDirectory.Exists)
            {
                ResultDirectory.Create();
                OnNewEvent("Result directory created");
            }

            if (!_tempDirectoryA.Exists)
            {
                _tempDirectoryA.Create();
                OnNewEvent("tempDirectoryA directory created");
            }

            if (!_tempDirectoryB.Exists)
            {
                _tempDirectoryB.Create();
                OnNewEvent("tempDirectoryB directory created");
            }

            FileA.CopyTo($"{_tempDirectoryA.FullName}\\{FileA.Name}", true);
            OnNewEvent($"FileA [{FileA.Name}] copied to tempDirectoryA");

            FileB.CopyTo($"{_tempDirectoryB.FullName}\\{FileB.Name}", true);
            OnNewEvent($"FileB [{FileB.Name}] copied to tempDirectoryB");

            GetFilesDifference(_tempDirectoryA, _tempDirectoryB, 1);
            GetFilesDifference(_tempDirectoryA, _tempDirectoryB, 2);
            GetFilesDifference(_tempDirectoryA, _tempDirectoryB, 3);
            GetFilesDifference(_tempDirectoryA, _tempDirectoryB, 4);
            GetFilesDifference(_tempDirectoryA, _tempDirectoryB, 5);
            GetFilesDifference(_tempDirectoryA, _tempDirectoryB, 6);
            GetFilesDifference(_tempDirectoryA, _tempDirectoryB, 8);
            GetFilesDifference(_tempDirectoryA, _tempDirectoryB, 10);
            GetFilesDifference(_tempDirectoryA, _tempDirectoryB, 14);
            GetFilesDifference(_tempDirectoryA, _tempDirectoryB, 18);
            GetFilesDifference(_tempDirectoryA, _tempDirectoryB, 22);

            OnNewEvent("Merge results \"in A not B\"");
            using (var streamWriter = new StreamWriter($"{ResultDirectory.FullName}\\AnotB.txt", true, SourceFileEncoding))
            {
                foreach (var file in _tempDirectoryA.GetFiles())
                {
                    OnNewEvent($"=>{file.Name}");
                    using (var streamReader = new StreamReader(file.FullName, SourceFileEncoding))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            if (line.Length != 0)
                            {
                                streamWriter.WriteLine(line);
                            }
                        }

                        streamReader.Close();
                    }
                }

                streamWriter.Close();
            }

            OnNewEvent("Merge results \"in B not A\"");
            using (var streamWriter = new StreamWriter($"{ResultDirectory.FullName}\\BnotA.txt", true, SourceFileEncoding))
            {
                foreach (var file in _tempDirectoryB.GetFiles())
                {
                    OnNewEvent($"=>{file.Name}");
                    using (var streamReader = new StreamReader(file.FullName, SourceFileEncoding))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            if (line.Length != 0)
                            {
                                streamWriter.WriteLine(line);
                            }
                        }

                        streamReader.Close();
                    }
                }

                streamWriter.Close();
            }

            _tempDirectoryA.Delete(true);
            OnNewEvent("tempDirectoryA directory deleted");
            _tempDirectoryB.Delete(true);
            OnNewEvent("tempDirectoryB directory deleted");
        }

        private void GetFilesDifference(DirectoryInfo dir0, DirectoryInfo dir1, int depth, bool withRevise = true)
        {
            OnNewEvent($"Split files in tempDirectoryA");
            foreach (var file in dir0.GetFiles())
            {
                Split_Work(file, dir0, depth);
            }

            OnNewEvent($"Split files in tempDirectoryB");
            foreach (var file in dir1.GetFiles())
            {
                Split_Work(file, dir1, depth);
            }

            if (withRevise)
            {
                Revize(dir0, dir1);
            }
        }

        private void Revize(DirectoryInfo dir0, DirectoryInfo dir1)
        {
            var files0 = dir0.GetFiles();
            var files1 = dir1.GetFiles();

            OnNewEvent($"Files count in temp directories: A={files0.Length}; B={files1.Length}");

            foreach (var fileInfo0 in files0)
            {
                OnNewEvent($"Search second file for [{fileInfo0.Name}]");
                var fileInfo1 = files1.FirstOrDefault(c => c.Name == fileInfo0.Name);
                if (fileInfo1 != null)
                {
                    OnNewEvent($"=> Finded");
                    var hashA = File.ReadAllText(fileInfo0.FullName, SourceFileEncoding).GetHash(SourceFileEncoding);
                    var hashB = File.ReadAllText(fileInfo1.FullName, SourceFileEncoding).GetHash(SourceFileEncoding);
                    OnNewEvent($"hash: A=[{hashA}]; B=[{hashB}]");
                    if (hashA == hashB)
                    {
                        fileInfo0.Delete();
                        fileInfo1.Delete();
                        OnNewEvent($"=> Files equlas, deleted");
                    }
                    else
                    {
                        OnNewEvent($"=> Files not equal? skip");
                    }
                }
                else
                {
                    OnNewEvent($"=X Not found");
                }
            }
        }

        private void Split_Work(FileInfo fileInfo, DirectoryInfo directory, int depth)
        {
            OnNewEvent($"Split file [{fileInfo.Name}][{depth} symbols]");

            var sws = new Dictionary<string, StreamWriter>();

            using (var sr = new StreamReader(fileInfo.FullName, SourceFileEncoding))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Length != 0)
                    {
                        var fChar = line.Substring(0, depth).Replace(" ", "_").Replace("\t", "_");

                        var sw = sws.FirstOrDefault(c => c.Key == fChar).Value;
                        if (sw == null)
                        {
                            sw = new StreamWriter($"{directory.FullName}\\{fChar}", true, SourceFileEncoding);
                            sws.Add(fChar, sw);
                        }

                        sw.WriteLine(line);
                    }
                }
            }

            OnNewEvent($"Source file [{fileInfo.Name}] delete");
            fileInfo.Delete();

            foreach (var sw in sws.Values)
            {
                sw.Close();
            }
        }


        private void OnNewEvent(string info) => NewEvent?.Invoke(info);
    }

    internal static class Extentions
    {
        internal static string GetHash(this string str, Encoding sourceFileEncoding) => new MD5CryptoServiceProvider().ComputeHash(sourceFileEncoding.GetBytes(str)).Aggregate(string.Empty, (current, b) => current + b.ToString("x2"));
    }
}