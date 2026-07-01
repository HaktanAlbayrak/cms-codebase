using System.Net;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.Domain;
using AmtVinc.Cms.ViewModels;

namespace AmtVinc.Cms.Services;

public interface IContactService
{
    Task SaveMessageAsync(ContactFormModel form);
}

public class ContactService : IContactService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly ISiteSettingService _settings;
    private readonly ILogger<ContactService> _logger;

    public ContactService(ApplicationDbContext db, IEmailSender email, ISiteSettingService settings, ILogger<ContactService> logger)
    {
        _db = db;
        _email = email;
        _settings = settings;
        _logger = logger;
    }

    public async Task SaveMessageAsync(ContactFormModel form)
    {
        var message = new ContactMessage
        {
            Name = form.Name.Trim(),
            Email = form.Email.Trim(),
            Phone = form.Phone?.Trim() ?? "",
            Subject = form.Subject.Trim(),
            Message = form.Message.Trim()
        };
        _db.ContactMessages.Add(message);
        await _db.SaveChangesAsync();

        // E-posta gönderimi mesaj kaydını engellememeli (best-effort).
        try
        {
            var settings = await _settings.GetAllAsync();
            var subjectTpl = settings.TryGetValue("mail.contactSubject", out var su) && !string.IsNullOrWhiteSpace(su)
                ? su : "[İletişim] {{subject}}";
            var bodyTpl = settings.TryGetValue("mail.contactTemplate", out var bt) && !string.IsNullOrWhiteSpace(bt)
                ? bt : "<p>{{message}}</p>";

            await _email.SendAsync(
                subject: Render(subjectTpl, message, htmlEncode: false),
                htmlBody: Render(bodyTpl, message, htmlEncode: true),
                replyTo: message.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İletişim formu e-postası gönderilemedi.");
        }
    }

    /// <summary>Şablondaki {{...}} yer tutucularını mesaj verisiyle doldurur.</summary>
    private static string Render(string template, ContactMessage m, bool htmlEncode)
    {
        string V(string s) => htmlEncode ? WebUtility.HtmlEncode(s ?? "") : (s ?? "");
        return template
            .Replace("{{name}}", V(m.Name))
            .Replace("{{email}}", V(m.Email))
            .Replace("{{phone}}", V(m.Phone))
            .Replace("{{subject}}", V(m.Subject))
            .Replace("{{message}}", htmlEncode ? WebUtility.HtmlEncode(m.Message ?? "").Replace("\n", "<br />") : (m.Message ?? ""))
            .Replace("{{date}}", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
    }
}
