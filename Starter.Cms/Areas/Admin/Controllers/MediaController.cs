using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Starter.Cms.Areas.Admin.Models;
using Starter.Cms.Services;

namespace Starter.Cms.Areas.Admin.Controllers;

/// <summary>Tek medya kütüphanesi: yüklenen dosyaları (<c>wwwroot/uploads</c>) listeler, ekler, siler ve yeniden kullandırır.</summary>
public class MediaController : AdminControllerBase
{
    private static readonly string[] MediaExtensions =
        { ".jpg", ".jpeg", ".png", ".webp", ".svg", ".ico", ".avif", ".gif", ".mp4", ".webm", ".pdf" };

    private readonly IWebHostEnvironment _env;
    private readonly IFileStorageService _files;

    public MediaController(IWebHostEnvironment env, IFileStorageService files)
    {
        _env = env;
        _files = files;
    }

    private string UploadsRoot()
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var root = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(root);
        return root;
    }

    private IEnumerable<MediaItem> Enumerate(string? folder)
    {
        var root = UploadsRoot();
        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => MediaExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Select(f =>
            {
                var info = new FileInfo(f);
                var rel = Path.GetRelativePath(root, f).Replace('\\', '/');
                var sub = rel.Contains('/') ? rel[..rel.IndexOf('/')] : "";
                return new MediaItem("/uploads/" + rel, info.Name, sub, info.Length, info.LastWriteTime);
            })
            .Where(m => string.IsNullOrEmpty(folder) || m.Folder == folder)
            .OrderByDescending(m => m.Modified);
    }

    public IActionResult Index(string? folder)
    {
        var root = UploadsRoot();
        ViewBag.Folders = Directory.GetDirectories(root).Select(Path.GetFileName).OrderBy(n => n).ToList();
        ViewBag.SelectedFolder = folder;
        return View(Enumerate(folder).ToList());
    }

    /// <summary>Medya seçici (modal) için JSON listesi.</summary>
    [HttpGet]
    public IActionResult List() =>
        Json(Enumerate(null).Select(m => new { url = m.Url, name = m.Name, folder = m.Folder }));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAjax(IFormFile? file, string folder = "general")
    {
        if (file is not { Length: > 0 }) return BadRequest(new { error = "Dosya yok." });
        try
        {
            var sub = string.IsNullOrWhiteSpace(folder) ? "general" : folder;
            var url = IsPdf(file) ? await _files.SaveDocumentAsync(file, sub) : await _files.SaveAsync(file, sub);
            return Json(new { url });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file, string folder = "general")
    {
        if (file is { Length: > 0 })
        {
            var sub = string.IsNullOrWhiteSpace(folder) ? "general" : folder;
            if (IsPdf(file)) await _files.SaveDocumentAsync(file, sub);
            else await _files.SaveAsync(file, sub);
            TempData["Success"] = AdminMessages.MediaUploaded;
        }
        return RedirectToAction(nameof(Index), new { folder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string webPath)
    {
        _files.Delete(webPath);
        TempData["Success"] = AdminMessages.MediaDeleted;
        return RedirectToAction(nameof(Index));
    }

    // ── CKEditor entegrasyonu ──
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CkUpload(IFormFile? upload, int CKEditorFuncNum)
    {
        try
        {
            if (upload is not { Length: > 0 }) throw new InvalidOperationException("Dosya bulunamadı.");
            var url = await _files.SaveAsync(upload, "icerik");
            return CkCallback(CKEditorFuncNum, url, "");
        }
        catch (Exception ex)
        {
            return CkCallback(CKEditorFuncNum, "", ex.Message);
        }
    }

    private ContentResult CkCallback(int funcNum, string url, string message) =>
        Content(
            $"<script>window.parent.CKEDITOR.tools.callFunction({funcNum}, {JsonSerializer.Serialize(url)}, {JsonSerializer.Serialize(message)});</script>",
            "text/html");

    private static bool IsPdf(IFormFile file) =>
        string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase);
}
