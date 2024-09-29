namespace AdapHuff
{
    internal class Node(Node? parent, byte branch, byte? chr)
    {
        private long _count = 0;
        public byte? chr = chr;
        public Node? parent = parent;
        public byte branch = branch;
        public Node? left;
        public Node? right;

        public string code
        {
            get
            {
                if (parent == null) return "";
                else return parent.code + branch.ToString();
            }
        }

        public int? level
        {
            get
            {
                if (parent != null) return parent.level + 1; else return 0;
            }
        }

        public long count
        {
            get { return _count; }
            set
            {
                _count = value;
                if (parent != null) { parent.count++; }
            }
        }
    }
}
