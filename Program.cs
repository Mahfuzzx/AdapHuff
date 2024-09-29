using AdapHuff;

long fileCount = 0;
byte[] _fileCount = new byte[8];
long totalSize = 0;
long compSize = 0;
FileStream fs;
AdaptiveHuffman adaptiveHuffman = new();

Console.CursorVisible = false;

if (args.Length < 3)
{
    Console.WriteLine("Geçerli kullanım:\n");
    Console.WriteLine("Paketleme       : adaphuff -p \"kaynak klasör\" \"hedef dosya\" [/i]");
    Console.WriteLine("Açma            : adaphuff -e \"kaynak paket\" \"hedef klasör\" [/i]\n");
    Console.WriteLine("/i parametresi verilmişse hedefin mevcut olduğu durumlarda sormadan üzerine yazılır.");
    return 0;
}

bool ignoreFlag = false;
string command = args[0].ToLower();
if (args.Length > 3) if (args[3].Equals("/i", StringComparison.CurrentCultureIgnoreCase)) ignoreFlag = true;

if (command == "-p")
{
    string sourceDir = args[1];
    string targetPak = args[2];

    if (File.Exists(targetPak) && !ignoreFlag)
    {
        Console.Write("Hedef dosya zaten var. Üzerine yazılsın mı?[E/H]");
        string pKey = "";
        while (pKey != "e" || pKey != "h")
        {
            pKey = Console.ReadKey(true).KeyChar.ToString().ToLower();
            if (pKey == "h") return 0;
            else if (pKey == "e") break;
        }
    }

    if (!Directory.Exists(sourceDir))
    {
        Console.WriteLine("Kaynak bir klasör değil veya böyle bir klasör mevcut değil!");
        return 0;
    }

    /*
     * dosyanın başında 3 bytelık dosya sayısı için alan ayır.
     * klasör içeriğini oku
     * dosya var mı?
     * önce dosyalardan başla
     * bir bit yaz. dosya için 1, klasör için 0
     * bir byte dosya/klasör adı uzunluğu karakteri oluştur ve ağaca ekle, bitwriter'a gönder
     * 4 byte dosya uzunluğu karakterlerini oluştur ve ağaca ekle, bitwriter'a gönder
     * dosya içeriğini byte byte okuyarak sıkıştırma yap
     * dosyalar bitince klasörlere başla
     * alt klasörün de içeriği bitince üst klasöre geçmek için klasör olarak "." kaydet
     * en son döngü ana klasöre ulaşınca sıkıştırma bitmiş olur.
     * 
     * -e d:\test.huff "D:\test" /i
     * -p "C:\Apache24\htdocs\trumbowyg" D:\test.huff /i
     * 
     */

    SrcDir root = new(null, ".", sourceDir);
    fs = File.Create(targetPak);
    fs.Write(BitConverter.GetBytes(fileCount));
    compressDir(root);
    adaptiveHuffman.finish();
    emptyOutputBuffer();
    //Array.Copy(BitConverter.GetBytes(fileCount), _fileCount, 3);
    fs.Position = 0;
    fs.Write(BitConverter.GetBytes(fileCount));
    fs.Dispose();
    Console.WriteLine($"\nToplam {fileCount} dosya sıkıştırıldı.");
}
else if (command == "-e")
{
    string sourcePak = args[1];
    string targetDir = args[2];
    if (!File.Exists(sourcePak))
    {
        Console.WriteLine("Böyle bir kaynak paket mevcut değil!");
        return 0;
    }

    if (!Directory.Exists(targetDir))
    {
        Console.WriteLine("Hedef bir klasör değil veya böyle bir klasör mevcut değil!");
        return 0;
    }

    fs = File.OpenRead(sourcePak);
    fs.Read(_fileCount);
    byte[] newArray = new byte[8];
    Array.Copy(_fileCount, newArray, 3);
    fileCount = BitConverter.ToInt64(newArray, 0);
    Console.WriteLine(fileCount + " Adet dosya çıkarılıyor...");
    nextEntry(targetDir);
    fs.Dispose();
}

return 0;

string readBits()
{
    var bytes = new byte[256];
    fs.Read(bytes, 0, bytes.Length);
    return string.Join("", bytes.Select(x => Convert.ToString(x, 2).PadLeft(8, '0')));
}

void nextEntry(string trPath)
{
    if (!trPath.EndsWith('\\')) trPath += "\\";
    if (!Directory.Exists(trPath)) Directory.CreateDirectory(trPath);
    var bits = adaptiveHuffman.bitBuffer;
    if (bits == "") bits = readBits();
    else adaptiveHuffman.bitShift();
    int chr = -1;
    while (fileCount > 0)
    {
        if (bits.StartsWith('1'))
        {
            var fileNameLen = 0;
            if (adaptiveHuffman.bitBufferLength == 0) fileNameLen = adaptiveHuffman.bitRead(bits[1..]);
            else
            {
                if (adaptiveHuffman.bitBufferLength < 256) fileNameLen = adaptiveHuffman.bitRead(readBits());
                else fileNameLen = adaptiveHuffman.bitRead("");
            }
            string fileName = "";
            while (fileName.Length < fileNameLen)
            {
                if (adaptiveHuffman.bitBufferLength < 256) chr = adaptiveHuffman.bitRead(readBits());
                else chr = adaptiveHuffman.bitRead("");
                fileName += (char)chr;
            }
            byte[] fileLenArray = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                if (adaptiveHuffman.bitBufferLength < 256) chr = adaptiveHuffman.bitRead(readBits());
                else chr = adaptiveHuffman.bitRead("");
                fileLenArray[i] = (byte)chr;
            }
            var fileLen = BitConverter.ToInt64(fileLenArray, 0);
            string filePath = trPath + fileName;
            Console.WriteLine("Dosya " + fileName);
            // dosya mevcut, üzerine yazılsın mı uyarısı?
            using FileStream tfs = File.OpenWrite(filePath);
            while (fileLen > 0)
            {
                if (adaptiveHuffman.bitBufferLength < 256) chr = adaptiveHuffman.bitRead(readBits());
                else chr = adaptiveHuffman.bitRead("");
                tfs.WriteByte((byte)chr);
                fileLen--;
            }
            bits = adaptiveHuffman.bitBuffer;
            adaptiveHuffman.bitShift();
            fileCount--;
        }
        else
        {
            var dirNameLen = 0;
            if (adaptiveHuffman.bitBufferLength == 0) dirNameLen = adaptiveHuffman.bitRead(bits[1..]);
            else
            {
                if (adaptiveHuffman.bitBufferLength < 256) dirNameLen = adaptiveHuffman.bitRead(readBits());
                else dirNameLen = adaptiveHuffman.bitRead("");
            }
            string dirName = "";
            while (dirName.Length < dirNameLen)
            {
                if (adaptiveHuffman.bitBufferLength < 256) chr = adaptiveHuffman.bitRead(readBits());
                else chr = adaptiveHuffman.bitRead("");
                dirName += (char)chr;
            }
            if (dirName == ".") return;
            Console.WriteLine("Klasör " + trPath + dirName);
            nextEntry(trPath + dirName);
            adaptiveHuffman.bitShift();
        }
    }
}

void emptyOutputBuffer()
{
    if (adaptiveHuffman.outputBufferLength > 0)
    {
        compSize += adaptiveHuffman.outputBufferLength;
        fs.Write(adaptiveHuffman.outputBuffer);
    }
}

void compressDir(SrcDir srcDir)
{
    if (srcDir.files.Count > 0)
    {
        fileCount += srcDir.files.Count;
        foreach (var file in srcDir.files)
        {
            var filePath = srcDir.path + file.name;
            var fileLen = new FileInfo(filePath).Length;
            totalSize += fileLen;
            Console.WriteLine(filePath);
            //var curPos = Console.GetCursorPosition();
            adaptiveHuffman.bitWrite('1');
            adaptiveHuffman.addChar2Tree((byte)file.name.Length);
            adaptiveHuffman.addChar2Tree(file.name);
            adaptiveHuffman.addChar2Tree(BitConverter.GetBytes(fileLen));
            emptyOutputBuffer();
            byte[] buffer = File.ReadAllBytes(filePath);
            adaptiveHuffman.addChar2Tree(buffer);
            emptyOutputBuffer();
            /*foreach (var chr in buffer)
            {
                adaptiveHuffman.addChar2Tree(chr);
                if (adaptiveHuffman.outputBufferLength > 0)
                {
                    fs.Write(adaptiveHuffman.outputBuffer);
                }
            }*/
            Console.WriteLine(" " + fileCount + " adet dosya, toplam " + totalSize + " byte, sıkıştırılmış: " + compSize + ", Oran:" + compSize * 100 / totalSize);
            /*using FileStream ffs = File.OpenRead(filePath);
            var rd = ffs.ReadByte();
            while (rd > -1)
            {
                adaptiveHuffman.addChar2Tree((byte)rd);
                if (adaptiveHuffman.outputBufferLength > 0)
                {
                    fs.Write(adaptiveHuffman.outputBuffer);
                }
                rd = ffs.ReadByte();
                Console.SetCursorPosition(curPos.Left, curPos.Top);
                Console.Write(100 * ffs.Position / ffs.Length);
            }*/
        }
    }
    if (srcDir.dirs.Count > 0)
    {
        foreach (var dir in srcDir.dirs)
        {
            adaptiveHuffman.bitWrite('0');
            adaptiveHuffman.addChar2Tree((byte)dir.name.Length);
            adaptiveHuffman.addChar2Tree(dir.name);
            emptyOutputBuffer();
            compressDir(dir);
        }
    }
    adaptiveHuffman.bitWrite('0');
    adaptiveHuffman.addChar2Tree(1);
    adaptiveHuffman.addChar2Tree('.');
    emptyOutputBuffer();
}
