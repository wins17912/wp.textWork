using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace wp.dll.lib32.textWork
{
	public sealed class DiffTextFiles
	{
		private FileInfo      FileA         { get; }
		private FileInfo      FileB         { get; }
		private Encoding      FileEncoding  { get; }
		private int           Depth         { get; }

		private readonly DirectoryInfo TempDirectory;
		private readonly DirectoryInfo TempDirectoryA;
		private readonly DirectoryInfo TempDirectoryB;

		public event DifferenceEventHandler NewEvent;

		public DiffTextFiles(FileInfo fileA, FileInfo fileB, DirectoryInfo tempDirectory = null, Encoding fileEncoding = null, int depth = 6)
		{
			FileA         = fileA;
			FileB         = fileB;
			TempDirectory = tempDirectory ?? new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Templates));
			FileEncoding  = fileEncoding  ?? Encoding.UTF8;
			Depth         = depth;

			OnNewEvent($"TempDirectory={TempDirectory.FullName}");

			var tempDirectoryName = DateTime.Now.Ticks.ToString();

			TempDirectoryA = new DirectoryInfo($"{TempDirectory.FullName}\\wp.lib32.textwork#{tempDirectoryName}#0");
			OnNewEvent($"TempDirectoryA={TempDirectoryA.FullName}");

			TempDirectoryB = new DirectoryInfo($"{TempDirectory.FullName}\\wp.lib32.textwork#{tempDirectoryName}#1");
			OnNewEvent($"TempDirectoryB={TempDirectoryB.FullName}");
		}

		public IEnumerable<DiffResult> Work()
		{
			OnNewEvent("Work started");

			if (!TempDirectory.Exists)
			{
				TempDirectory.Create();
				OnNewEvent("Temp directory created");
			}

			if (!TempDirectoryA.Exists)
			{
				TempDirectoryA.Create();
				OnNewEvent("TempDirectoryA directory created");
			}

			if (!TempDirectoryB.Exists)
			{
				TempDirectoryB.Create();
				OnNewEvent("TempDirectoryB directory created");
			}

			FileA.CopyTo($"{TempDirectoryA.FullName}\\{FileA.Name}", true);
			OnNewEvent($"FileA [{FileA.Name}] copied to TempDirectoryA");

			FileB.CopyTo($"{TempDirectoryB.FullName}\\{FileB.Name}", true);
			OnNewEvent($"FileB [{FileB.Name}] copied to TempDirectoryB");

			for (var i = 1; i < Depth; i++)
			{
				GetFilesDifference(TempDirectoryA, TempDirectoryB, i);
			}

			var resultList = new List<DiffResult>();

			OnNewEvent("Merge results \"in A not B\"");
			GetResultList(TempDirectoryA, DiffFile.A, ref resultList);

			OnNewEvent("Merge results \"in B not A\"");
			GetResultList(TempDirectoryB, DiffFile.B, ref resultList);

			TempDirectoryA.Delete(true);
			OnNewEvent("TempDirectoryA directory deleted");
			TempDirectoryB.Delete(true);
			OnNewEvent("TempDirectoryB directory deleted");

			if (TempDirectory.FullName != Environment.GetFolderPath(Environment.SpecialFolder.Templates))
			{
				TempDirectory.Delete(true);
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
					var hashA = File.ReadAllText(fileInfo0.FullName, FileEncoding).GetHash(FileEncoding);
					var hashB = File.ReadAllText(fileInfo1.FullName, FileEncoding).GetHash(FileEncoding);
					OnNewEvent($"hashA=[{hashA}]");
					OnNewEvent($"hashB=[{hashB}]");
					if (hashA == hashB)
					{
						fileInfo0.Delete();
						fileInfo1.Delete();
						OnNewEvent($"=> Files equlas, deleted");
					}
					else
					{
						OnNewEvent($"=> Files not equal, skip");
					}
				}
				else
				{
					OnNewEvent($"=X Not found");
				}
			}
		}

		private void Split_Work(FileSystemInfo fileInfo, FileSystemInfo directory, int depth)
		{
			OnNewEvent($"Split file [{fileInfo.Name}][{depth}]");

			var sws = new Dictionary<string, StreamWriter>();

			using (var sr = new StreamReader(fileInfo.FullName, FileEncoding))
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
							sw = new StreamWriter($"{directory.FullName}\\{fChar}", true, FileEncoding);
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

	public struct DiffResult
	{
		public DiffFile DiffFile { get; }
		public string   Line     { get; }

		public DiffResult(DiffFile diffFile, string line)
		{
			DiffFile = diffFile;
			Line     = line;
		}
	}

	public enum DiffFile
	{
		A,
		B
	}

	internal static class Extentions
	{
		internal static string GetHash(this string str, Encoding sourceFileEncoding) => new MD5CryptoServiceProvider().ComputeHash(sourceFileEncoding.GetBytes(str)).Aggregate(string.Empty, (current, b) => current + b.ToString("x2"));
	}
}