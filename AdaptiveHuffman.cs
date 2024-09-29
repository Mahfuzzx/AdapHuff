namespace AdapHuff
{
    internal class AdaptiveHuffman
    {
        private readonly Node[] chars;
        private byte[] _outputBuffer = new byte[1165536];
        private long _outputBufferLength;

        public byte[] outputBuffer
        {
            get
            {
                byte[] output = new byte[_outputBufferLength];
                Array.Copy(_outputBuffer, output, _outputBufferLength);
                _outputBufferLength = 0;
                return output;
            }
            set { _outputBuffer = value; }
        }

        public long outputBufferLength
        {
            get { return _outputBufferLength; }
        }

        public long bitBufferLength
        {
            get { return _bitBuffer.Length; }
        }

        private readonly Node tree = new(null, 0, null);
        private readonly Node none;
        private string _bitBuffer = "";

        public string bitBuffer
        {
            get { return _bitBuffer; }
            set
            {
                _bitBuffer = value;
                while (_bitBuffer.Length > 7)
                {
                    byte newByte = Convert.ToByte(_bitBuffer[..8], 2);
                    //Array.Resize(ref _outputBuffer, _outputBuffer.Length + 1);
                    _outputBuffer[_outputBufferLength] = newByte;
                    _outputBufferLength++;
                    _bitBuffer = bitBuffer[8..];
                }
            }
        }

        public AdaptiveHuffman()
        {
            chars = new Node[257];
            none = new(tree, 1, null);
            tree.right = none;
        }

        public void finish()
        {
            if (bitBuffer.Length > 0) bitBuffer = _bitBuffer.PadRight(8, '0');
        }

        Node? getNode(Node parent)
        {
            if (_bitBuffer.StartsWith('1'))
            {
                _bitBuffer = _bitBuffer[1..];
                if (parent.right != null)
                {
                    if (parent.right == none) return none;
                    if (parent.right.chr != null) return parent.right;
                    else return getNode(parent.right);
                }
            }
            else
            {
                _bitBuffer = _bitBuffer[1..];
                if (parent.left != null)
                {
                    if (parent.left.chr != null) return parent.left;
                    else return getNode(parent.left);
                }
            }
            return null;
        }

        public void bitShift()
        {
            _bitBuffer = _bitBuffer[1..];
        }

        public int bitRead(string bits)
        {
            _bitBuffer += bits;
            int chr = -1;
            if (tree.count == 0)
            {
                chr = Convert.ToByte(_bitBuffer[..8], 2);
                chars[chr] = new(tree, 0, (byte)chr);
                chars[chr].count++;
                tree.left = chars[chr];
                _bitBuffer = _bitBuffer[8..];
            }
            else
            {
                var node = getNode(tree) ?? throw new Exception("Wow!");
                if (node == none)
                {
                    chr = Convert.ToByte(_bitBuffer[..8], 2);
                    _bitBuffer = _bitBuffer[8..];
                    if (chars[chr] == null)
                    {
                        Node nonesParent = none.parent ?? tree;
                        Node newNode = new(nonesParent, 1, null);
                        chars[chr] = new(newNode, 0, (byte)chr);
                        none.parent = newNode;
                        nonesParent.right = newNode;
                        newNode.left = chars[chr];
                        newNode.right = none;
                    }
                }
                else
                {
                    if (node.chr != null) chr = (int)node.chr;
                }
                sortNodes(chars[chr], chars[chr].count + 1);
            }
            return chr;
        }

        public void bitWrite(char bit)
        {
            bitBuffer += bit;
        }

        private void bitWrite(byte chr)
        {
            var bits = Convert.ToString(chr, 2).PadLeft(8, '0');
            bitBuffer += bits;
        }

        private void bitWrite(string bits)
        {
            bitBuffer += bits;
        }

        Node? findMatch(Node node, Node compare, long newCount)
        {
            Node? ret = null;
            var nCodeLen = node.code.Length;
            var cCodeLen = compare.code.Length;
            if (nCodeLen <= cCodeLen) return null;
            if (compare != node && compare != tree && compare != node.parent && newCount == compare.count + 1) return compare;
            if (compare.left != null) ret = findMatch(node, compare.left, newCount);
            if (ret == null && compare.right != null) return findMatch(node, compare.right, newCount);
            else return ret;
        }

        void sortNodes(Node node, long newCount, bool isThisParent = false)
        {
            if (node == none || node == tree) return;
            var match = newCount == 1 ? null : findMatch(node, tree, newCount);
            if (match != null)
            {
                var tbr = node.branch;
                var tpr = node.parent;
                node.branch = match.branch;
                node.parent = match.parent;
                if (node.branch == 0) (node.parent ?? tree).left = node;
                if (node.branch == 1) (node.parent ?? tree).right = node;
                match.branch = tbr;
                match.parent = tpr;
                if (match.branch == 0) (match.parent ?? tree).left = match;
                if (match.branch == 1) (match.parent ?? tree).right = match;
            }
            var tprnt = node.parent ?? tree;
            sortNodes(tprnt, tprnt.count + 1, true);
            if (!isThisParent) node.count++;
        }

        public void addChar2Tree(byte chr)
        {
            if (tree.count == 0)
            {
                bitWrite(chr);
                chars[chr] = new(tree, 0, chr);
                chars[chr].count++;
                tree.left = chars[chr];
            }
            else
            {
                if (chars[chr] == null)
                {
                    bitWrite(none.code);
                    bitWrite(chr);
                    Node nonesParent = none.parent ?? tree;
                    Node newNode = new(nonesParent, 1, null);
                    chars[chr] = new(newNode, 0, chr);
                    none.parent = newNode;
                    nonesParent.right = newNode;
                    newNode.left = chars[chr];
                    newNode.right = none;
                }
                else bitWrite(chars[chr].code);
                sortNodes(chars[chr], chars[chr].count + 1);
            }
        }

        public void addChar2Tree(byte[] chrs)
        {
            foreach (byte chr in chrs)
            {
                addChar2Tree(chr);
            }
        }

        public void addChar2Tree(char chr)
        {
            addChar2Tree(Convert.ToByte(chr));
        }

        public void addChar2Tree(string chrs)
        {
            foreach (char c in chrs)
            {
                addChar2Tree((byte)c);
            }
        }
    }
}
