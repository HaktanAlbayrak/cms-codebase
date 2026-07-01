using Microsoft.AspNetCore.Mvc;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Areas.Admin.Controllers;

/// <summary>SMTP / e-posta ayarları (DB'den), mail şablonu ve test e-postası gönderme.</summary>
public class MailController : AdminOnlyControllerBase
{
    private readonly IBrandingService _branding;
    private readonly ISiteSettingService _settings;
    private readonly IEmailSender _email;

    public MailController(IBrandingService branding, ISiteSettingService settings, IEmailSender email)
    {
        _branding = branding;
        _settings = settings;
        _email = email;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Settings = await _settings.GetAllAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Dictionary<string, string> values)
    {
        values ??= new();
        // Checkbox işaretli değilse anahtar gelmez → açıkça "false" yaz.
        if (!values.ContainsKey("smtp.enabled")) values["smtp.enabled"] = "false";
        if (!values.ContainsKey("smtp.ssl")) values["smtp.ssl"] = "false";

        await _branding.SaveAsync(values);
        TempData["Success"] = AdminMessages.MailSettingsSaved;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTest(string? testEmail)
    {
        try
        {
            await _email.SendTestAsync(testEmail ?? "");
            TempData["Success"] = AdminMessages.MailTestSent(testEmail);
        }
        catch (Exception ex)
        {
            TempData["Error"] = AdminMessages.MailTestFailed(ex.Message);
        }
        return RedirectToAction(nameof(Index));
    }
}
