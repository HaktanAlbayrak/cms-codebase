using Microsoft.AspNetCore.Mvc;
using Starter.Cms.Services;
using Starter.Cms.ViewModels;

namespace Starter.Cms.Controllers;

public class ContactController : Controller
{
    private readonly IContactService _contact;
    private readonly IPageService _pages;

    public ContactController(IContactService contact, IPageService pages)
    {
        _contact = contact;
        _pages = pages;
    }

    public async Task<IActionResult> Index(string culture)
    {
        if (!CultureContext.IsSupported(culture)) culture = CultureContext.Current;
        ViewBag.Page = await _pages.GetBySlugAsync("contact", culture);
        return View(new ContactFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactFormModel form, string culture)
    {
        if (!CultureContext.IsSupported(culture)) culture = CultureContext.Current;

        if (!ModelState.IsValid)
        {
            ViewBag.Page = await _pages.GetBySlugAsync("contact", culture);
            return View(form);
        }

        await _contact.SaveMessageAsync(form);
        TempData["ContactSuccess"] = true;
        return RedirectToAction(nameof(Index), new { culture });
    }
}
