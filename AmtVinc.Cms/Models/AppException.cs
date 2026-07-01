using Microsoft.AspNetCore.Http;

namespace AmtVinc.Cms.Models;

/// <summary>
/// Uygulamanın bilerek fırlattığı, mesajı son kullanıcıya gösterilebilecek hataların
/// temel sınıfı. Teknik/beklenmeyen hatalardan (DB, null, IO...) ayrışır.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>Son kullanıcıya gösterilebilecek, teknik detay içermeyen mesaj.</summary>
    public string UserMessage { get; }

    /// <summary>Bu hataya karşılık gelen HTTP durum kodu (404, 400, 409, 500...).</summary>
    public int StatusCode { get; }

    protected AppException(string userMessage, int statusCode, Exception? inner = null)
        : base(userMessage, inner)
    {
        UserMessage = userMessage;
        StatusCode = statusCode;
    }
}

/// <summary>İstenen kayıt/sayfa bulunamadı (HTTP 404).</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string userMessage = "Aradığınız içerik bulunamadı.")
        : base(userMessage, StatusCodes.Status404NotFound) { }
}

/// <summary>İş kuralı ihlali — kullanıcının düzeltebileceği bir durum (HTTP 409/400).</summary>
public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string userMessage, int statusCode = StatusCodes.Status409Conflict)
        : base(userMessage, statusCode) { }
}
