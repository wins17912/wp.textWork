using System.IO;
using System.Text;

namespace wp.dll.lib32.textWork
{
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