using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.Domain;

namespace AmtVinc.Cms.Services;

/// <summary>
/// Veritabanı destekli medya kütüphanesi. Fiziksel dosyayı <see cref="IFileStorageService"/> ile
/// <c>wwwroot/uploads</c> altına yazar, yönetilebilir metadata'yı (tür, klasör, boyut, çok dilli
/// alt/başlık/açıklama) DB'de tutar. Görsel/video/PDF/belge tek kütüphanede toplanır.
/// </summary>
public interface IMediaService
{
    /// <summary>Klasör ve/veya türe göre filtreli, aktif medya listesi (en yeni önce).</summary>
    Task<List<MediaAsset>> ListAsync(string? folder = null, MediaType? type = null);

    /// <summary>Kütüphanede kullanılan klasör adları.</summary>
    Task<List<string>> FoldersAsync();

    Task<MediaAsset?> GetAsync(int id);

    /// <summary>Dosyayı kaydeder, türünü tespit eder, çok dilli metadata satırlarını oluşturur.</summary>
    Task<MediaAsset> UploadAsync(IFormFile file, string folder);

    /// <summary>Metadata'yı (sıra, aktiflik, çok dilli başlık/alt/açıklama) günceller.</summary>
    Task UpdateAsync(MediaUpdateRequest request);

    /// <summary>DB kaydını ve fiziksel dosyayı siler.</summary>
    Task DeleteAsync(int id);

    /// <summary><c>wwwroot/uploads</c> altındaki ama DB'de olmayan dosyaları içeri aktarır (idempotent).</summary>
    Task ImportExistingFilesAsync();

    /// <summary>Uzantıdan medya türünü tespit eder.</summary>
    static MediaType DetectType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".svg" or ".ico" or ".avif" or ".gif" => MediaType.Image,
            ".mp4" or ".webm" or ".mov" or ".ogv" => MediaType.Video,
            ".pdf" => MediaType.Pdf,
            ".mp3" or ".wav" or ".ogg" or ".m4a" => MediaType.Audio,
            ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" or ".csv" or ".zip" => MediaType.Document,
            _ => MediaType.Other
        };
    }
}

/// <summary>Medya metadata güncelleme isteği — çeviri alanları dil-koduyla sözlüklenir.</summary>
public class MediaUpdateRequest
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string> Title { get; set; } = new();
    public Dictionary<string, string> Alt { get; set; } = new();
    public Dictionary<string, string> Caption { get; set; } = new();
}

public class MediaService : IMediaService
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        ApplicationDbContext db,
        IFileStorageService files,
        IWebHostEnvironment env,
        ILogger<MediaService> logger)
    {
        _db = db;
        _files = files;
        _env = env;
        _logger = logger;
    }

    public async Task<List<MediaAsset>> ListAsync(string? folder = null, MediaType? type = null)
    {
        var query = _db.MediaAssets.Include(m => m.Translations).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(folder)) query = query.Where(m => m.Folder == folder);
        if (type is not null) query = query.Where(m => m.Type == type);
        return await query.OrderByDescending(m => m.CreatedDate).ToListAsync();
    }

    public async Task<List<string>> FoldersAsync() =>
        await _db.MediaAssets.Select(m => m.Folder).Distinct().OrderBy(f => f).ToListAsync();

    public Task<MediaAsset?> GetAsync(int id) =>
        _db.MediaAssets.Include(m => m.Translations).FirstOrDefaultAsync(m => m.Id == id);

    public async Task<MediaAsset> UploadAsync(IFormFile file, string folder)
    {
        var sub = string.IsNullOrWhiteSpace(folder) ? "general" : folder.Trim();
        var url = await _files.SaveMediaAsync(file, sub);

        var asset = BuildAsset(url, file.FileName, file.Length, sub, file.ContentType);
        await AttachTranslationsAsync(asset);

        _db.MediaAssets.Add(asset);
        await _db.SaveChangesAsync();
        return asset;
    }

    public async Task UpdateAsync(MediaUpdateRequest request)
    {
        var asset = await _db.MediaAssets.Include(m => m.Translations)
            .FirstOrDefaultAsync(m => m.Id == request.Id);
        if (asset is null) return;

        asset.SortOrder = request.SortOrder;
        asset.IsActive = request.IsActive;

        foreach (var lang in await ActiveLanguageCodesAsync())
        {
            var tr = asset.Translations.FirstOrDefault(t => t.LanguageCode == lang);
            if (tr is null)
            {
                tr = new MediaAssetTranslation { LanguageCode = lang };
                asset.Translations.Add(tr);
            }
            tr.Title = Val(request.Title, lang);
            tr.Alt = Val(request.Alt, lang);
            tr.Caption = Val(request.Caption, lang);
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var asset = await _db.MediaAssets.FindAsync(id);
        if (asset is null) return;
        _files.Delete(asset.Url);
        _db.MediaAssets.Remove(asset);
        await _db.SaveChangesAsync();
    }

    public async Task ImportExistingFilesAsync()
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var root = Path.Combine(webRoot, "uploads");
        if (!Directory.Exists(root)) return;

        var known = new HashSet<string>(await _db.MediaAssets.Select(m => m.Url).ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        var added = false;
        foreach (var path in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            if (!_files.IsAllowed(path)) continue;

            var rel = Path.GetRelativePath(root, path).Replace('\\', '/');
            var url = "/uploads/" + rel;
            if (known.Contains(url)) continue;

            var info = new FileInfo(path);
            var folder = rel.Contains('/') ? rel[..rel.IndexOf('/')] : "general";
            var asset = BuildAsset(url, info.Name, info.Length, folder, ContentTypeOf(info.Name));
            await AttachTranslationsAsync(asset);

            _db.MediaAssets.Add(asset);
            known.Add(url);
            added = true;
        }

        if (added)
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("Mevcut yükleme dosyaları medya kütüphanesine içeri aktarıldı.");
        }
    }

    // ── Yardımcılar ──

    private MediaAsset BuildAsset(string url, string originalName, long size, string folder, string? contentType)
    {
        var type = IMediaService.DetectType(originalName);
        var asset = new MediaAsset
        {
            Url = url,
            FileName = Path.GetFileName(url),
            OriginalName = originalName,
            Type = type,
            Folder = folder,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? ContentTypeOf(originalName) : contentType,
            Size = size
        };

        if (type == MediaType.Image)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var physical = Path.Combine(webRoot, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (ImageDimensions.TryRead(physical) is { } dim)
            {
                asset.Width = dim.Width;
                asset.Height = dim.Height;
            }
        }

        return asset;
    }

    private async Task AttachTranslationsAsync(MediaAsset asset)
    {
        var title = Path.GetFileNameWithoutExtension(asset.OriginalName);
        foreach (var lang in await ActiveLanguageCodesAsync())
            asset.Translations.Add(new MediaAssetTranslation
            {
                LanguageCode = lang,
                Title = title,
                Alt = title
            });
    }

    private Task<List<string>> ActiveLanguageCodesAsync() =>
        _db.Languages.Where(l => l.IsActive).OrderBy(l => l.SortOrder).Select(l => l.Code).ToListAsync();

    private static string ContentTypeOf(string fileName) =>
        ContentTypes.TryGetContentType(fileName, out var ct) ? ct : "application/octet-stream";

    private static string Val(IDictionary<string, string> map, string lang) =>
        map.TryGetValue(lang, out var v) ? (v ?? "") : "";
}
