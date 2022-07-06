namespace wp.dll.lib32.textWork
{
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
}