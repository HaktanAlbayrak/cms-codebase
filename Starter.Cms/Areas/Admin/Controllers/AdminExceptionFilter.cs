using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Starter.Cms.Areas.Admin.Controllers;

/// <summary>
/// Admin alanındaki yakalanmamış hataları nazikçe ele alır: hatayı loglar, kullanıcıyı
/// public hata sayfasına fırlatmak yerine geldiği sayfada tutar ve üstte kırmızı bir
/// uyarı banner'ı (<c>TempData["Error"]</c>) gösterir.
/// </summary>
public sealed class AdminExceptionFilter : IExceptionFilter
{
    private readonly ILogger<AdminExceptionFilter> _logger;
    private readonly ITempDataDictionaryFactory _tempDataFactory;

    public AdminExceptionFilter(ILogger<AdminExceptionFilter> logger, ITempDataDictionaryFactory tempDataFactory)
    {
        _logger = logger;
        _tempDataFactory = tempDataFactory;
    }

    public void OnException(ExceptionContext context)
    {
        var request = context.HttpContext.Request;
        _logger.LogError(context.Exception,
            "Admin işlemi sırasında hata oluştu: {Method} {Path}", request.Method, request.Path);

        // Doğrulama/iş kuralı türü hataların mesajı gösterilebilir; teknik hatalarda genel mesaj.
        var friendly = context.Exception is InvalidOperationException or ArgumentException
            ? context.Exception.Message
            : AdminMessages.UnexpectedError;

        var tempData = _tempDataFactory.GetTempData(context.HttpContext);
        tempData["Error"] = friendly;

        var referer = request.Headers.Referer.ToString();
        var target = !string.IsNullOrWhiteSpace(referer) && Uri.IsWellFormedUriString(referer, UriKind.RelativeOrAbsolute)
            ? referer
            : "/Admin/Dashboard";

        context.Result = new RedirectResult(target);
        context.ExceptionHandled = true;
    }
}
