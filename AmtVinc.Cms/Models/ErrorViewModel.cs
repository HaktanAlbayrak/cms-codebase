namespace AmtVinc.Cms.Models;

/// <summary>Hata/durum sayfası için DB'siz görünüm modeli (ErrorPresentation'dan beslenir).</summary>
public class ErrorViewModel
{
    public int StatusCode { get; init; }
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
    public string Culture { get; init; } = "tr";
    public string HomeLabel { get; init; } = "";
    public string? RequestId { get; init; }
}
