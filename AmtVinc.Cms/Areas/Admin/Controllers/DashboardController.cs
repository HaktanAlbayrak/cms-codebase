using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Areas.Admin.Models;
using AmtVinc.Cms.Data;

namespace AmtVinc.Cms.Areas.Admin.Controllers;

public class DashboardController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var vm = new DashboardVm
        {
            PageCount = await _db.Pages.CountAsync(),
            SlideCount = await _db.Slides.CountAsync(),
            LanguageCount = await _db.Languages.CountAsync(),
            MessageCount = await _db.ContactMessages.CountAsync(),
            UnreadMessageCount = await _db.ContactMessages.CountAsync(m => !m.IsRead),
            UserCount = await _db.Users.CountAsync(),
        };
        return View(vm);
    }
}
