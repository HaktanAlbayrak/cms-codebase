using System.Net;
using System.Net.Mail;

namespace Starter.Cms.Services;

/// <summary>SMTP ayarlarının anlık (DB'den okunmuş) görünümü.</summary>
public class SmtpOptions
{
    public bool Enabled { get; init; }
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string User { get; init; } = "";
    public string Password { get; init; } = "";
    public string FromEmail { get; init; } = "";
    public string FromName { get; init; } = "Starter";
    public string ToEmail { get; init; } = "";   // iletişim formu alıcısı

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromEmail);
}

public interface IEmailSender
{
    /// <summary>Belirtilen alıcıya (boşsa smtp.toEmail) HTML e-posta gönderir. SMTP kapalı/eksikse sessizce atlar (best-effort).</summary>
    Task SendAsync(string subject, string htmlBody, string? toEmail = null, string? replyTo = null);

    /// <summary>Test e-postası gönderir. Etkin bayrağını yok sayar; hata olursa <b>fırlatır</b> (panelde gösterilsin diye).</summary>
    Task SendTestAsync(string toEmail);
}

/// <summary>
/// SMTP ayarlarını veritabanından (SiteSettings · <c>smtp.*</c> anahtarları) okur.
/// Böylece e-posta yapılandırması appsettings.json yerine admin panelden yönetilir.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly ISiteSettingService _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(ISiteSettingService settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    private async Task<SmtpOptions> LoadAsync()
    {
        var s = await _settings.GetAllAsync();
        string G(string key, string fallback = "") =>
            s.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;
        bool B(string key) => G(key).Equals("true", StringComparison.OrdinalIgnoreCase) || G(key) == "on";

        return new SmtpOptions
        {
            Enabled = B("smtp.enabled"),
            Host = G("smtp.host"),
            Port = int.TryParse(G("smtp.port", "587"), out var p) ? p : 587,
            EnableSsl = B("smtp.ssl"),
            User = G("smtp.user"),
            Password = G("smtp.password"),
            FromEmail = G("smtp.fromEmail"),
            FromName = G("smtp.fromName", "Starter"),
            ToEmail = G("smtp.toEmail"),
        };
    }

    public async Task SendAsync(string subject, string htmlBody, string? toEmail = null, string? replyTo = null)
    {
        var opt = await LoadAsync();
        var to = string.IsNullOrWhiteSpace(toEmail) ? opt.ToEmail : toEmail;
        if (!opt.Enabled || !opt.IsConfigured || string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("SMTP devre dışı veya yapılandırılmamış; e-posta gönderilmedi ({Subject}).", subject);
            return;
        }
        await SendCoreAsync(opt, to, subject, htmlBody, replyTo);
    }

    public async Task SendTestAsync(string toEmail)
    {
        var opt = await LoadAsync();
        if (!opt.IsConfigured)
            throw new InvalidOperationException("SMTP sunucu (Host) ve gönderen e-posta (Kimden) alanları dolu olmalıdır.");
        var to = string.IsNullOrWhiteSpace(toEmail) ? opt.ToEmail : toEmail;
        if (string.IsNullOrWhiteSpace(to))
            throw new InvalidOperationException("Test e-postası için bir alıcı adresi gerekli.");

        var body = $"""
            <div style="font-family:Arial,sans-serif;font-size:14px;color:#333">
              <h2>✅ SMTP Test E-postası</h2>
              <p>Bu e-postayı görüyorsanız, e-posta ayarlarınız doğru çalışıyor.</p>
              <p style="color:#888">Gönderim zamanı: {DateTime.Now:dd.MM.yyyy HH:mm}</p>
            </div>
            """;
        await SendCoreAsync(opt, to, $"{opt.FromName} — SMTP Test E-postası", body, replyTo: null);
    }

    private static async Task SendCoreAsync(SmtpOptions opt, string to, string subject, string htmlBody, string? replyTo)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(opt.FromEmail, opt.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);
        if (!string.IsNullOrWhiteSpace(replyTo)) message.ReplyToList.Add(new MailAddress(replyTo));

        using var client = new SmtpClient(opt.Host, opt.Port)
        {
            EnableSsl = opt.EnableSsl,
            Credentials = string.IsNullOrWhiteSpace(opt.User)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(opt.User, opt.Password)
        };
        await client.SendMailAsync(message);
    }
}
