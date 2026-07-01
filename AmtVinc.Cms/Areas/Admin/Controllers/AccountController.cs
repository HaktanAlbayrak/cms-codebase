using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Areas.Admin.Controllers;

[Area("Admin")]
public class AccountController : Controller
{
    private readonly IAdminAuthService _auth;

    public AccountController(IAdminAuthService auth) => _auth = auth;

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        if (await _auth.SignInAsync(HttpContext, username ?? "", password ?? ""))
            return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/Admin/Dashboard" : returnUrl);

        TempData["Error"] = AdminMessages.InvalidCredentials;
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _auth.SignOutAsync(HttpContext);
        return RedirectToAction(nameof(Login));
    }

    // Content Manager, admin'e özel bir sayfaya eriştiğinde (403) buraya yönlenir.
    [HttpGet]
    public IActionResult Denied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
