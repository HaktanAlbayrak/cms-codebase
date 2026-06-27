namespace Starter.Cms.Services;

/// <summary>
/// Yüklenen görselleri <c>wwwroot/uploads/{klasör}</c> altına kaydeden basit yerel
/// dosya deposu. URL alanları geriye dönük uyumlu kalır: bir alan dosya ile
/// güncellenmezse mevcut yol korunur.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Görsel/video kaydeder ve web yolunu (<c>/uploads/...</c>) döner.</summary>
    Task<string> SaveAsync(IFormFile file, string subfolder);

    /// <summary>Belge (PDF) kaydeder ve web yolunu döner.</summary>
    Task<string> SaveDocumentAsync(IFormFile file, string subfolder);

    /// <summary>Yerel bir yükleme dosyasını siler (best-effort; harici URL'lere dokunmaz).</summary>
    void Delete(string? webPath);
}

public class FileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".svg", ".ico", ".avif", ".gif", ".mp4", ".webm"
    };

    private static readonly HashSet<string> AllowedDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf"
    };

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public Task<string> SaveAsync(IFormFile file, string subfolder) =>
        SaveCoreAsync(file, subfolder, AllowedExtensions);

    public Task<string> SaveDocumentAsync(IFormFile file, string subfolder) =>
        SaveCoreAsync(file, subfolder, AllowedDocumentExtensions);

    private async Task<string> SaveCoreAsync(IFormFile file, string subfolder, HashSet<string> allowed)
    {
        if (file is null || file.Length == 0)
            throw new InvalidOperationException("Boş dosya yüklenemez.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !allowed.Contains(ext))
            throw new InvalidOperationException($"Desteklenmeyen dosya türü: {ext}");

        var safeFolder = string.Concat(subfolder.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        if (string.IsNullOrEmpty(safeFolder)) safeFolder = "general";

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var targetDir = Path.Combine(webRoot, "uploads", safeFolder);
        Directory.CreateDirectory(targetDir);

        var baseName = Slugify(Path.GetFileNameWithoutExtension(file.FileName));
        var fileName = $"{baseName}-{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(targetDir, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(stream);

        return $"/uploads/{safeFolder}/{fileName}";
    }

    public void Delete(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath)) return;
        if (!webPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var relative = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(webRoot, relative);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Yükleme dosyası silinemedi: {Path}", webPath);
        }
    }

    private static string Slugify(string value)
    {
        var slug = new string(value.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        slug = string.Join('-', slug.Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrEmpty(slug) ? "file" : slug[..Math.Min(slug.Length, 40)];
    }
}
