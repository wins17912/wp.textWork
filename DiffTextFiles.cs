using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace wp.dll.lib32.textWork
{
    public sealed class DiffTextFiles
    {
        private FileInfo FileA           { get; }
        private FileInfo FileB           { get; }
        private Encoding FileEncoding    { get; }
        public  decimal  FileALinesCount { get; private set; }
        public  decimal  FileBLinesCount { get; private set; }

        private int _depth;

        private readonly DirectoryInfo _tempDirectory;
        private readonly DirectoryInfo _tempDirectoryA;
        private readonly DirectoryInfo _tempDirectoryB;

        public event DifferenceEventHandler NewEvent;

        public DiffTextFiles(FileInfo fileA, FileInfo fileB, DirectoryInfo tempDirectory = null, Encoding fileEncoding = null)
        {
            FileA          = fileA;
            FileB          = fileB;
            _tempDirectory = tempDirectory ?? new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Templates));
            FileEncoding   = fileEncoding  ?? Encoding.UTF8;
            _depth         = int.MaxValue;

            OnNewEvent($"TempDirectory={_tempDirectory.FullName}");

            var tempDirectoryName = DateTime.Now.Ticks.ToString();

            _tempDirectoryA = new DirectoryInfo($"{_tempDirectory.FullName}\\wp.lib32.textwork#{tempDirectoryName}#0");
            OnNewEvent($"TempDirectoryA={_tempDirectoryA.FullName}");

            _tempDirectoryB = new DirectoryInfo($"{_tempDirectory.FullName}\\wp.lib32.textwork#{tempDirectoryName}#1");
            OnNewEvent($"TempDirectoryB={_tempDirectoryB.FullName}");
        }

        public IEnumerable<DiffResult> Work()
        {
            OnNewEvent("Work started");

            if (!_tempDirectory.Exists)
            {
                _tempDirectory.Create();
                OnNewEvent("Temp directory created");
            }
            else
            {
                foreach (var directory in _tempDirectory.GetDirectories())
                {
                    try
                    {
                        directory.Delete(true);
                    }
                    catch 
                    {
                        
                    }
                }
            }

            if (!_tempDirectoryA.Exists)
            {
                _tempDirectoryA.Create();
                OnNewEvent("TempDirectoryA directory created");
            }

            if (!_tempDirectoryB.Exists)
            {
                _tempDirectoryB.Create();
                OnNewEvent("TempDirectoryB directory created");
            }

            FileA.CopyTo($"{_tempDirectoryA.FullName}\\{FileA.Name}", true);
            OnNewEvent($"FileA [{FileA.Name}] copied to TempDirectoryA");

            FileB.CopyTo($"{_tempDirectoryB.FullName}\\{FileB.Name}", true);
            OnNewEvent($"FileB [{FileB.Name}] copied to TempDirectoryB");

            for (var i = 1; i < _depth; i++)
            {
                _depth = int.MaxValue;
                GetFilesDifference(_tempDirectoryA, _tempDirectoryB, i);
                if (_tempDirectoryA.GetFiles().Length == 0 && _tempDirectoryB.GetFiles().Length == 0)
                {
                    break;
                }
            }

            var resultList = new List<DiffResult>();

            OnNewEvent("Merge results \"in A not B\"");
            GetResultList(_tempDirectoryA, DiffFile.A, ref resultList);

            OnNewEvent("Merge results \"in B not A\"");
            GetResultList(_tempDirectoryB, DiffFile.B, ref resultList);

            _tempDirectoryA.Delete(true);
            OnNewEvent("TempDirectoryA directory deleted");
            _tempDirectoryB.Delete(true);
            OnNewEvent("TempDirectoryB directory deleted");

            if (_tempDirectory.FullName != Environment.GetFolderPath(Environment.SpecialFolder.Templates))
            {
                _tempDirectory.Delete(true);
                OnNewEvent("TempDirectory directory deleted");
            }

            return resultList;
        }

        private void GetResultList(DirectoryInfo directoryInfo, DiffFile diffFile, ref List<DiffResult> list)
        {
            foreach (var file in directoryInfo.GetFiles())
            {
                using (var streamReader = new StreamReader(file.FullName, FileEncoding))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (line.Length != 0)
                        {
                            list.Add(new DiffResult(diffFile, line));
                        }
                    }

                    streamReader.Close();
                }
            }
        }

        private void GetFilesDifference(DirectoryInfo dir0, DirectoryInfo dir1, int depth)
        {
            OnNewEvent("Split files in tempDirectoryA");
            foreach (var file in dir0.GetFiles().Where(c => !c.Name.EndsWith(".0") && !c.Name.EndsWith(".1")))
            {
                Split_Work(file, dir0, depth, out var linesCount);
                FileALinesCount = linesCount > FileALinesCount ? linesCount : FileALinesCount;
            }

            OnNewEvent("Split files in tempDirectoryB");
            foreach (var file in dir1.GetFiles().Where(c => !c.Name.EndsWith(".0") && !c.Name.EndsWith(".1")))
            {
                Split_Work(file, dir1, depth, out var linesCount);
                FileBLinesCount = linesCount > FileBLinesCount ? linesCount : FileBLinesCount;
            }

            Revize(dir0, dir1);
        }

        private void Revize(DirectoryInfo dir0, DirectoryInfo dir1)
        {
            var files0 = dir0.GetFiles().Where(c => !c.Name.EndsWith(".1")).ToList();
            var files1 = dir1.GetFiles().Where(c => !c.Name.EndsWith(".1")).ToList();

            OnNewEvent($"Files count in temp directories: A={files0.Count}; B={files1.Count}");

            if (files0.Count != 0 && files1.Count != 0)
            {
                foreach (var fileInfo0 in files0)
                {
                    OnNewEvent($"Search second file for [{fileInfo0.Name}]");
                    var fileInfo1 = files1.FirstOrDefault(c => c.Name == fileInfo0.Name);
                    if (fileInfo1 != null)
                    {
                        var hashA = SafeGetHash(fileInfo0);
                        var hashB = SafeGetHash(fileInfo1);
                        if (hashA == hashB)
                        {
                            SafeDelete(fileInfo0);
                            SafeDelete(fileInfo1);

                            OnNewEvent("Files equlas, deleted");
                        }
                        else
                        {
                            OnNewEvent("Files not equal, skip");
                            if (fileInfo0.Name.EndsWith(".0"))
                            {
                                SafeRenameFile(fileInfo0);
                                OnNewEvent("File marked to skip");
                            }
                        }
                    }
                    else
                    {
                        OnNewEvent("Not found");
                        if (fileInfo0.Name.EndsWith(".0"))
                        {
                            SafeRenameFile(fileInfo0);
                            OnNewEvent("File marked to skip");
                        }
                    }
                }
            }
            else
            {
                _depth = 1;
            }
        }

        private void Split_Work(FileSystemInfo fileInfo, DirectoryInfo directory, int depth, out decimal linesCount)
        {
            OnNewEvent($"* Split file [{fileInfo.Name}][{depth}]");

            var streamWriters = new List<StreamWriterExt>();

            using (var sr = new StreamReader(fileInfo.FullName, FileEncoding))
            {
                linesCount = 0;
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Length != 0)
                    {
                        linesCount++;
                        if (line.Length < _depth)
                        {
                            _depth = line.Length;
                        }

                        var name = line.Substring(0, depth).Replace(" ", "_").ToFileName("_");
                        streamWriters.GetOrCreate(name, directory, FileEncoding).WriteLine(line);
                    }
                }

                sr.Close();
            }

            streamWriters.CloseAll();
            OnNewEvent($"Source file [{fileInfo.Name}] delete");
            fileInfo.Delete();
        }

        private void SafeDelete(FileInfo file)
        {
            while (file.Exists)
            {
                try
                {
                    file.Delete();
                    file.Refresh();
                }
                catch 
                {
                    Thread.Sleep(100);
                }
            }
        }

        private string SafeGetHash(FileInfo file)
        {
            string hash = null;

            while (hash == null)
            {
                try
                {
                    hash = File.ReadAllText(file.FullName, FileEncoding).GetHash(FileEncoding);
                }
                catch 
                {
                    Thread.Sleep(100);
                }
            }

            return hash;
        }

        private void SafeRenameFile(FileInfo file)
        {
            while (true)
            {
                try
                {
                    file.MoveTo($"{file.DirectoryName}\\{file.Name.Replace(".0", ".1")}");
                    break;
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }
        }

        private void OnNewEvent(string info) => NewEvent?.Invoke(info);
    }

    internal class StreamWriterExt
    {
        internal FileInfo     FileInfo     { get; }
        internal StreamWriter StreamWriter { get; }
        internal decimal      Counter      { get; private set; }

        internal StreamWriterExt(FileInfo fileInfo, Encoding encoding)
        {
            FileInfo     = fileInfo;
            StreamWriter = new StreamWriter(fileInfo.FullName, true, encoding);
        }

        internal void WriteLine(string line)
        {
            StreamWriter.WriteLine(line);
            Counter += 1;
        }
    }
}