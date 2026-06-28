using System.Buffers.Binary;

namespace Starter.Cms.Services;

/// <summary>
/// Harici bağımlılık olmadan, dosya başlığından görsel boyutu (genişlik×yükseklik) okuyan
/// best-effort yardımcı. PNG / GIF / JPEG / BMP / WEBP destekler. Başarısız olursa <c>null</c>
/// döner — medya kaydı yine de oluşturulur, boyut yalnızca bilgilendirme amaçlıdır.
/// </summary>
public static class ImageDimensions
{
    public static (int Width, int Height)? TryRead(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> head = stackalloc byte[32];
            var read = stream.Read(head);
            if (read < 24) return null;

            // PNG: 89 50 4E 47 ... IHDR @ 16
            if (head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47)
                return (BinaryPrimitives.ReadInt32BigEndian(head.Slice(16, 4)),
                        BinaryPrimitives.ReadInt32BigEndian(head.Slice(20, 4)));

            // GIF: "GIF8" — width/height little-endian @ 6
            if (head[0] == (byte)'G' && head[1] == (byte)'I' && head[2] == (byte)'F')
                return (BinaryPrimitives.ReadUInt16LittleEndian(head.Slice(6, 2)),
                        BinaryPrimitives.ReadUInt16LittleEndian(head.Slice(8, 2)));

            // BMP: "BM" — width/height little-endian @ 18
            if (head[0] == (byte)'B' && head[1] == (byte)'M')
                return (BinaryPrimitives.ReadInt32LittleEndian(head.Slice(18, 4)),
                        BinaryPrimitives.ReadInt32LittleEndian(head.Slice(22, 4)));

            // WEBP: "RIFF"...."WEBP"
            if (head[0] == (byte)'R' && head[1] == (byte)'I' && head[2] == (byte)'F' && head[3] == (byte)'F'
                && head[8] == (byte)'W' && head[9] == (byte)'E' && head[10] == (byte)'B' && head[11] == (byte)'P')
                return ReadWebp(head);

            // JPEG: SOF marker taraması gerekir (akış üzerinde)
            if (head[0] == 0xFF && head[1] == 0xD8)
                return ReadJpeg(stream);

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static (int, int)? ReadWebp(ReadOnlySpan<byte> head)
    {
        // VP8 (lossy): boyut @ 26 (14-bit), VP8L (lossless) ve VP8X (extended) farklı.
        var fourCc = System.Text.Encoding.ASCII.GetString(head.Slice(12, 4));
        if (fourCc == "VP8 ")
        {
            int w = (head[26] | (head[27] << 8)) & 0x3FFF;
            int h = (head[28] | (head[29] << 8)) & 0x3FFF;
            return (w, h);
        }
        if (fourCc == "VP8L")
        {
            int b = head[21] | (head[22] << 8) | (head[23] << 16) | (head[24] << 24);
            int w = (b & 0x3FFF) + 1;
            int h = ((b >> 14) & 0x3FFF) + 1;
            return (w, h);
        }
        if (fourCc == "VP8X")
        {
            int w = (head[24] | (head[25] << 8) | (head[26] << 16)) + 1;
            int h = (head[27] | (head[28] << 8) | (head[29] << 16)) + 1;
            return (w, h);
        }
        return null;
    }

    private static (int, int)? ReadJpeg(FileStream stream)
    {
        stream.Position = 2;
        Span<byte> buf = stackalloc byte[5];
        while (stream.Read(buf.Slice(0, 2)) == 2)
        {
            if (buf[0] != 0xFF) return null;
            byte marker = buf[1];

            // SOF0..SOF15 (C4/C8/CC hariç) boyut taşır.
            bool isSof = marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC;
            if (stream.Read(buf.Slice(0, 2)) != 2) return null;
            int segLen = (buf[0] << 8) | buf[1];

            if (isSof)
            {
                // Segment gövdesi: precision(1) + height(2) + width(2)
                if (stream.Read(buf.Slice(0, 5)) != 5) return null;
                int height = (buf[1] << 8) | buf[2];
                int width = (buf[3] << 8) | buf[4];
                return (width, height);
            }

            stream.Position += segLen - 2;
        }
        return null;
    }
}
