namespace AdapHuff
{
    internal class SrcFile(SrcDir parent, string name)
    {
        public SrcDir parent = parent;
        public string name = name;
        public long size = new FileInfo(parent.path + name).Length;
    }
}
