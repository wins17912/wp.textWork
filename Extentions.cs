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
    }
}