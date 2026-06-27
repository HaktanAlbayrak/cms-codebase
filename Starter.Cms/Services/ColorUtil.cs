using System.Globalization;

namespace Starter.Cms.Services;

/// <summary>
/// Hex renkleri Tailwind'in opacity modifier'larıyla (ör. <c>bg-brand/90</c>) uyumlu
/// "R G B" kanal biçimine çevirir. CSS değişkeni <c>--brand: 37 99 235</c> olarak yazılır,
/// Tailwind config'i <c>rgb(var(--brand) / &lt;alpha-value&gt;)</c> ile tüketir.
/// </summary>
public static class ColorUtil
{
    /// <summary>"#2563eb" → "37 99 235". Geçersizse fallback hex'i kullanır.</summary>
    public static string ToRgbChannels(string? hex, string fallback = "#2563eb")
    {
        var (r, g, b) = Parse(hex) ?? Parse(fallback) ?? (37, 99, 235);
        return $"{r} {g} {b}";
    }

    /// <summary>Hex rengi verilen oranda koyulaştırıp "R G B" kanal biçimi döndürür.</summary>
    public static string DarkenChannels(string? hex, double factor = 0.8, string fallback = "#2563eb")
    {
        var (r, g, b) = Parse(hex) ?? Parse(fallback) ?? (37, 99, 235);
        int D(int c) => Math.Clamp((int)Math.Round(c * factor), 0, 255);
        return $"{D(r)} {D(g)} {D(b)}";
    }

    private static (int R, int G, int B)? Parse(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;

        hex = hex.Trim().TrimStart('#');
        if (hex.Length == 3)
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        if (hex.Length != 6) return null;

        if (int.TryParse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) &&
            int.TryParse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) &&
            int.TryParse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            return (r, g, b);

        return null;
    }
}
