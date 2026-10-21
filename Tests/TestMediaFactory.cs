using System.Buffers.Binary;
using System.Text;

namespace Tests;

internal static class TestMediaFactory
{
    public static string CreateBlueJpg(string directory, string fileName) => CreateMedia(directory, fileName, [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xD9
    ]);

    public static string CreateJpg(
        string directory,
        string fileName,
        string original,
        string? modified = null,
        string? offset = null,
        string? subSeconds = null,
        string? gpsDate = null)
    {
        List<(ushort Tag, string Value)> exif = [(0x9003, original)];

        if (offset is not null)
            exif.Add((0x9011, offset));

        if (subSeconds is not null)
            exif.Add((0x9291, subSeconds));

        int rootCount = 1 + (modified is null ? 0 : 1) + (gpsDate is null ? 0 : 1);
        int exifOffset = 8 + 2 + rootCount * 12 + 4;
        int gpsOffset = exifOffset + 2 + exif.Count * 12 + 4;
        int dataOffset = gpsOffset + (gpsDate is null ? 0 : 18);
        List<byte> tiff = [0x4D, 0x4D, 0, 42];
        AddUInt32(tiff, 8);
        List<byte> data = [];
        AddUInt16(tiff, rootCount);

        if (modified is not null)
            AddAsciiEntry(tiff, data, 0x0132, modified, dataOffset);

        AddPointer(tiff, 0x8769, exifOffset);

        if (gpsDate is not null)
            AddPointer(tiff, 0x8825, gpsOffset);

        AddUInt32(tiff, 0);
        AddUInt16(tiff, exif.Count);

        foreach ((ushort tag, string value) in exif)
            AddAsciiEntry(tiff, data, tag, value, dataOffset);

        AddUInt32(tiff, 0);

        if (gpsDate is not null)
        {
            AddUInt16(tiff, 1);
            AddAsciiEntry(tiff, data, 0x001D, gpsDate, dataOffset);
            AddUInt32(tiff, 0);
        }

        tiff.AddRange(data);
        List<byte> jpeg = [0xFF, 0xD8, 0xFF, 0xE1];
        AddUInt16(jpeg, 2 + 6 + tiff.Count);
        jpeg.AddRange("Exif\0\0"u8.ToArray());
        jpeg.AddRange(tiff);
        jpeg.AddRange([0xFF, 0xD9]);

        return CreateMedia(directory, fileName, [.. jpeg]);
    }

    public static string CreateQuickTime(
        string directory, string fileName, DateTimeOffset movieCreated, string contentCreated)
    {
        byte[] header = new byte[100];
        DateTimeOffset epoch = new(1904, 1, 1, 0, 0, 0, TimeSpan.Zero);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), checked((uint)(movieCreated - epoch).TotalSeconds));
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8), checked((uint)(movieCreated - epoch).TotalSeconds));
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(12), 1000);
        byte[] key = Atom("mdta", [.. "com.apple.quicktime.creationdate"u8]);
        byte[] keys = Atom("keys", [0, 0, 0, 0, 0, 0, 0, 1, .. key]);
        byte[] value = Atom("data", [0, 0, 0, 1, 0, 0, 0, 0, .. Encoding.UTF8.GetBytes(contentCreated)]);
        byte[] item = Atom("\0\0\0\u0001", value);
        byte[] handler = Atom("hdlr", [0, 0, 0, 0, 0, 0, 0, 0, .. "mdta"u8.ToArray(), .. new byte[13]]);
        byte[] metadata = Atom("meta", [.. handler, .. keys, .. Atom("ilst", item)]);
        byte[] movie = Atom("moov", [.. Atom("mvhd", header), .. metadata]);

        return CreateMedia(directory, fileName, [.. Atom("ftyp", [.. "qt  \0\0\0\0qt  "u8]), .. movie]);
    }

    public static string CreateVideo(string directory, string fileName, int marker) =>
        CreateMedia(directory, fileName, [0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, (byte)marker]);

    public static string CreateWhiteJpg(string directory, string fileName) => CreateMedia(directory, fileName, [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xD9
    ]);

    public static void SetCreatedAt(string path, DateTimeOffset dateTimeOffset)
    {
        File.SetCreationTimeUtc(path, dateTimeOffset.UtcDateTime);
        File.SetLastWriteTimeUtc(path, dateTimeOffset.UtcDateTime);
    }

    private static void AddAsciiEntry(List<byte> target, List<byte> data, int tag, string value, int dataOffset)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value + '\0');
        AddUInt16(target, tag);
        AddUInt16(target, 2);
        AddUInt32(target, bytes.Length);

        if (bytes.Length <= 4)
        {
            target.AddRange(bytes);
            target.AddRange(new byte[4 - bytes.Length]);
        }
        else
        {
            AddUInt32(target, dataOffset + data.Count);
            data.AddRange(bytes);
        }
    }

    private static void AddPointer(List<byte> target, int tag, int offset)
    {
        AddUInt16(target, tag);
        AddUInt16(target, 4);
        AddUInt32(target, 1);
        AddUInt32(target, offset);
    }

    private static void AddUInt16(List<byte> bytes, int value) => bytes.AddRange([(byte)(value >> 8), (byte)value]);

    private static void AddUInt32(List<byte> bytes, int value) =>
        bytes.AddRange([(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);

    private static byte[] Atom(string type, byte[] payload)
    {
        byte[] bytes = new byte[8 + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(bytes, bytes.Length);
        Encoding.ASCII.GetBytes(type).CopyTo(bytes, 4);
        payload.CopyTo(bytes, 8);

        return bytes;
    }

    private static string CreateMedia(string directory, string fileName, byte[] bytes)
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, bytes);

        return path;
    }
}
