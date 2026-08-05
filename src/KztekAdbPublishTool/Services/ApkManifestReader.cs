using System.IO.Compression;
using System.Text;

namespace KztekAdbPublishTool.Services;

/// <summary>
/// Đọc trực tiếp package name từ AndroidManifest.xml bên trong file .apk (định dạng Binary XML / AXML)
/// — không cần aapt/aapt2 (platform-tools không có sẵn 2 công cụ này), chỉ dùng System.IO.Compression
/// để mở .apk như .zip rồi tự parse cấu trúc chunk nhị phân của AXML.
/// </summary>
public static class ApkManifestReader
{
    private const int ChunkStringPool = 0x0001;
    private const int ChunkXmlStartElement = 0x0102;

    public static string? TryGetPackageName(string apkPath)
    {
        try
        {
            using var zip = ZipFile.OpenRead(apkPath);
            var entry = zip.GetEntry("AndroidManifest.xml");
            if (entry == null) return null;

            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ParsePackageName(ms.ToArray());
        }
        catch
        {
            // File hỏng, không phải APK hợp lệ, hoặc format AXML không như kỳ vọng — bỏ qua,
            // để user tự nhập package thủ công như trước.
            return null;
        }
    }

    private static string? ParsePackageName(byte[] data)
    {
        using var reader = new BinaryReader(new MemoryStream(data));

        // XML chunk header (generic ResChunk_header): type, headerSize, chunkSize
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt32();

        var strings = new List<string>();

        while (reader.BaseStream.Position <= reader.BaseStream.Length - 8)
        {
            var chunkStart = reader.BaseStream.Position;
            var chunkType = reader.ReadUInt16();
            reader.ReadUInt16(); // headerSize — không cần vì đã biết cấu trúc từng loại chunk
            var chunkSize = reader.ReadUInt32();

            if (chunkSize < 8 || chunkStart + chunkSize > reader.BaseStream.Length)
                break; // dữ liệu bất thường — dừng an toàn thay vì đọc tràn

            if (chunkType == ChunkStringPool)
            {
                strings = ReadStringPool(reader, chunkStart);
            }
            else if (chunkType == ChunkXmlStartElement)
            {
                reader.ReadUInt32(); // lineNumber
                reader.ReadUInt32(); // comment
                reader.ReadUInt32(); // namespaceUri
                var nameIdx = reader.ReadUInt32();
                var elementName = GetString(strings, (int)nameIdx);

                reader.ReadUInt16(); // attributeStart
                reader.ReadUInt16(); // attributeSize
                var attributeCount = reader.ReadUInt16();
                reader.ReadUInt16(); // idIndex
                reader.ReadUInt16(); // classIndex
                reader.ReadUInt16(); // styleIndex

                for (var i = 0; i < attributeCount; i++)
                {
                    reader.ReadUInt32(); // attribute namespaceUri
                    var attrNameIdx = reader.ReadUInt32();
                    var attrRawValueIdx = reader.ReadInt32();
                    reader.ReadUInt16(); // typedValue.size
                    reader.ReadByte();   // res0
                    var dataType = reader.ReadByte();
                    var typedData = reader.ReadInt32();

                    if (GetString(strings, (int)attrNameIdx) == "package")
                    {
                        if (attrRawValueIdx >= 0)
                            return GetString(strings, attrRawValueIdx);
                        if (dataType == 3) // TYPE_STRING
                            return GetString(strings, typedData);
                    }
                }

                // Attribute "package" luôn nằm trên thẻ <manifest> gốc — hết attribute của thẻ đầu
                // tiên mà không thấy thì không cần đọc tiếp toàn bộ cây XML còn lại.
                if (elementName == "manifest") return null;
            }

            reader.BaseStream.Position = chunkStart + chunkSize;
        }

        return null;
    }

    private static string? GetString(List<string> strings, int index)
        => index >= 0 && index < strings.Count ? strings[index] : null;

    private static List<string> ReadStringPool(BinaryReader reader, long chunkStart)
    {
        var stringCount = reader.ReadUInt32();
        reader.ReadUInt32(); // styleCount
        var flags = reader.ReadUInt32();
        var stringsStart = reader.ReadUInt32();
        reader.ReadUInt32(); // stylesStart

        var isUtf8 = (flags & 0x100) != 0;

        var offsets = new uint[stringCount];
        for (var i = 0; i < stringCount; i++)
            offsets[i] = reader.ReadUInt32();

        var result = new List<string>((int)stringCount);
        var dataStart = chunkStart + stringsStart;

        for (var i = 0; i < stringCount; i++)
        {
            reader.BaseStream.Position = dataStart + offsets[i];
            result.Add(isUtf8 ? ReadUtf8String(reader) : ReadUtf16String(reader));
        }

        return result;
    }

    private static string ReadUtf16String(BinaryReader reader)
    {
        // Độ dài mã hóa dạng 1 hoặc 2 uint16 (bit cao nhất của giá trị đầu báo hiệu có phần mở rộng).
        int length = reader.ReadUInt16();
        if ((length & 0x8000) != 0)
        {
            var low = reader.ReadUInt16();
            length = ((length & 0x7FFF) << 16) | low;
        }

        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = (char)reader.ReadUInt16();
        return new string(chars);
    }

    private static string ReadUtf8String(BinaryReader reader)
    {
        // Chuỗi UTF-8 trong string pool có 2 tiền tố độ dài: số ký tự rồi số byte — cả hai dùng
        // cùng kiểu mã hóa 1-hoặc-2-byte (bit cao nhất báo hiệu byte thứ 2).
        ReadUtf8Length(reader); // số ký tự — không cần dùng tới
        var byteLength = ReadUtf8Length(reader);
        var bytes = reader.ReadBytes(byteLength);
        return Encoding.UTF8.GetString(bytes);
    }

    private static int ReadUtf8Length(BinaryReader reader)
    {
        int length = reader.ReadByte();
        if ((length & 0x80) != 0)
        {
            var low = reader.ReadByte();
            length = ((length & 0x7F) << 8) | low;
        }
        return length;
    }
}
