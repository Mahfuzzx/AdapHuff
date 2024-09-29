namespace AdapHuff
{
    internal class SrcDir(SrcDir? parent, string name, string path)
    {
        private List<SrcDir>? _dirs = null;
        private List<SrcFile>? _files = null;
        public SrcDir? parent = parent;
        public string name = name;
        public string path = path.EndsWith('\\') ? path : path + '\\';

        public List<SrcFile> files
        {
            get
            {
                if (_files == null)
                {
                    //Console.WriteLine(path+" files");
                    _files = [];
                    string[] files = Directory.GetFiles(path);
                    foreach (string file in files)
                    {
                        _files.Add(new SrcFile(this, new FileInfo(file).Name));
                    }
                }
                return _files;
            }
        }

        public List<SrcDir> dirs
        {
            get
            {
                if (_dirs == null)
                {
                    //Console.WriteLine(path + " dirs");
                    _dirs = [];
                    string[] dirs = Directory.GetDirectories(path);
                    foreach (string dir in dirs)
                    {
                        _dirs.Add(new SrcDir(this, new DirectoryInfo(dir).Name, dir));
                    }
                }
                return _dirs;
            }
        }
    }
}
