using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace wp.dll.lib32.textWork
{
    internal static class Extentions
    {
        internal static string GetHash(this string str, Encoding sourceFileEncoding) => new MD5CryptoServiceProvider().ComputeHash(sourceFileEncoding.GetBytes(str)).Aggregate(string.Empty, (current, b) => current + b.ToString("x2"));

        internal static string ToFileName(this string input, string replace = null) =>
            input
                .Replace("\\", replace   ?? string.Empty)
                .Replace("/", replace    ?? string.Empty)
                .Replace(":", replace    ?? string.Empty)
                .Replace("*", replace    ?? string.Empty)
                .Replace("?", replace    ?? string.Empty)
                .Replace("<", replace    ?? string.Empty)
                .Replace(">", replace    ?? string.Empty)
                .Replace("|", replace    ?? string.Empty)
                .Replace(".", replace    ?? string.Empty)
                .Replace("\r\n", replace ?? " ")
                .Replace("\r", replace   ?? " ")
                .Replace("\n", replace   ?? " ")
                .Replace("\t", replace   ?? " ")
                .Trim();

        internal static StreamWriterExt GetOrCreate(this List<StreamWriterExt> list, string name, DirectoryInfo directory, Encoding encoding)
        {
            var ext = list.FirstOrDefault(c => c.FileInfo.Name == name);
            if (ext == null)
            {
                ext = new StreamWriterExt(new FileInfo($"{directory.FullName}\\{name}"), encoding);
                list.Add(ext);
            }
            
            return ext;
        }

        internal static void CloseAll(this IEnumerable<StreamWriterExt> ieEnumerable)
        {
            foreach (var streamWriterExt in ieEnumerable)
            {
                streamWriterExt.StreamWriter.Close();
                if (streamWriterExt.Counter == 1)
                {
                    streamWriterExt.FileInfo.MoveTo($"{streamWriterExt.FileInfo.FullName}.0");
                }
            }
        }
    }
}