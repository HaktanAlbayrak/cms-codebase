using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Cms.Areas.Admin.Models;
using Starter.Cms.Data;
using Starter.Cms.Domain;
using Starter.Cms.Services;

namespace Starter.Cms.Areas.Admin.Controllers;

/// <summary>
/// Tek medya kütüphanesi — artık veritabanı destekli içerik yapısı. Görsel/video/PDF/belge
/// yükler, türe/klasöre göre listeler, çok dilli metadata'yı (başlık/alt/açıklama) düzenletir,
/// siler ve <c>_MediaPicker</c> ile her alanda yeniden kullandırır.
/// </summary>
public class MediaController : AdminControllerBase
{
    private readonly IMediaService _media;
    private readonly IFileStorageService _files;
    private readonly ApplicationDbContext _db;

    public MediaController(IMediaService media, IFileStorageService files, ApplicationDbContext db)
    {
        _media = media;
        _files = files;
        _db = db;
    }

    public async Task<IActionResult> Index(string? folder, MediaType? type)
    {
        ViewBag.Folders = await _media.FoldersAsync();
        ViewBag.SelectedFolder = folder;
        ViewBag.SelectedType = type;
        return View(await _media.ListAsync(folder, type));
    }

    /// <summary>Medya seçici (modal) için JSON listesi; opsiyonel tür filtresi.</summary>
    [HttpGet]
    public async Task<IActionResult> List(MediaType? type)
    {
        var items = await _media.ListAsync(null, type);
        return Json(items.Select(m => new
        {
            url = m.Url,
            name = m.OriginalName,
            folder = m.Folder,
            type = m.Type.ToString().ToLowerInvariant(),
            alt = m.Translations.FirstOrDefault()?.Alt ?? ""
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAjax(IFormFile? file, string folder = "general")
    {
        if (file is not { Length: > 0 }) return BadRequest(new { error = "Dosya yok." });
        try
        {
            var asset = await _media.UploadAsync(file, folder);
            return Json(new { url = asset.Url, type = asset.Type.ToString().ToLowerInvariant() });
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
            await _media.UploadAsync(file, folder);
            TempData["Success"] = AdminMessages.MediaUploaded;
        }
        return RedirectToAction(nameof(Index), new { folder });
    }

    public async Task<IActionResult> Edit(int id)
    {
        await PopulateLanguagesAsync();
        var asset = await _media.GetAsync(id);
        if (asset is null) return NotFound();

        var model = new MediaEditModel
        {
            Id = asset.Id,
            SortOrder = asset.SortOrder,
            IsActive = asset.IsActive
        };
        foreach (var t in asset.Translations)
        {
            model.Title[t.LanguageCode] = t.Title;
            model.Alt[t.LanguageCode] = t.Alt;
            model.Caption[t.LanguageCode] = t.Caption;
        }
        ViewBag.Asset = asset;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MediaEditModel model)
    {
        await _media.UpdateAsync(new MediaUpdateRequest
        {
            Id = model.Id,
            SortOrder = model.SortOrder,
            IsActive = model.IsActive,
            Title = model.Title,
            Alt = model.Alt,
            Caption = model.Caption
        });
        TempData["Success"] = AdminMessages.MediaSaved;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _media.DeleteAsync(id);
        TempData["Success"] = AdminMessages.MediaDeleted;
        return RedirectToAction(nameof(Index));
    }

    // ── CKEditor entegrasyonu — yüklenen görsel de kütüphaneye düşer ──
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CkUpload(IFormFile? upload, int CKEditorFuncNum)
    {
        try
        {
            if (upload is not { Length: > 0 }) throw new InvalidOperationException("Dosya bulunamadı.");
            var asset = await _media.UploadAsync(upload, "icerik");
            return CkCallback(CKEditorFuncNum, asset.Url, "");
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

    private async Task PopulateLanguagesAsync() =>
        ViewBag.Languages = await _db.Languages.Where(l => l.IsActive).OrderBy(l => l.SortOrder)
            .AsNoTracking().ToListAsync();
}
