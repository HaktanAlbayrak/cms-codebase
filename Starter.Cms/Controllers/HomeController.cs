using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Starter.Cms.Models;
using Starter.Cms.Services;

namespace Starter.Cms.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    /// <summary>UseExceptionHandler hedefi — beklenmeyen hatalar için lokalize, DB'siz sayfa.</summary>
    [Route("{culture}/Home/Error")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var culture = ErrorPresentation.CultureFromPath(feature?.Path) ?? CultureContext.Default;

        // Bilerek fırlatılan (gösterilebilir) hatalarda kullanıcı mesajını göster.
        var appEx = feature?.Error as AppException;
        var status = appEx?.StatusCode ?? StatusCodes.Status500InternalServerError;
        Response.StatusCode = status;

        var view = ErrorPresentation.For(status, culture, appEx?.UserMessage);
        return View("~/Views/Shared/_ErrorPage.cshtml", new ErrorViewModel
        {
            StatusCode = view.StatusCode,
            Title = view.Title,
            Message = view.Message,
            Culture = culture,
            HomeLabel = ErrorPresentation.HomeLabel(culture),
            RequestId = HttpContext.TraceIdentifier
        });
    }

    /// <summary>UseStatusCodePagesWithReExecute hedefi (404/403...).</summary>
    [Route("{culture}/Home/Status")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Status(int code, string culture)
    {
        // Yeniden çalıştırma route'tan varsayılan kültürü geçirir; gerçek kültürü
        // kullanıcının orijinal isteğinden (ör. /en/...) çöz, böylece 404 doğru dilde gelir.
        var reExec = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>();
        if (reExec is not null)
            culture = ErrorPresentation.CultureFromPath(reExec.OriginalPath);
        if (!CultureContext.IsSupported(culture)) culture = CultureContext.Default;
        var view = ErrorPresentation.For(code, culture);
        return View("~/Views/Shared/_ErrorPage.cshtml", new ErrorViewModel
        {
            StatusCode = view.StatusCode,
            Title = view.Title,
            Message = view.Message,
            Culture = culture,
            HomeLabel = ErrorPresentation.HomeLabel(culture)
        });
    }
}
