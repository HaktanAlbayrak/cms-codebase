using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Cms.Data;

namespace Starter.Cms.Areas.Admin.Controllers;

/// <summary>İletişim formundan gelen mesajları görüntüler, okundu işaretler ve siler.</summary>
public class MessagesController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;

    public MessagesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var messages = await _db.ContactMessages
            .OrderByDescending(m => m.CreatedDate)
            .AsNoTracking().ToListAsync();
        return View(messages);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var message = await _db.ContactMessages.FindAsync(id);
        if (message is null) return NotFound();

        if (!message.IsRead)
        {
            message.IsRead = true;
            await _db.SaveChangesAsync();
        }
        return View(message);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var message = await _db.ContactMessages.FindAsync(id);
        if (message is not null)
        {
            _db.ContactMessages.Remove(message);
            await _db.SaveChangesAsync();
            TempData["Success"] = AdminMessages.ContactMessageDeleted;
        }
        return RedirectToAction(nameof(Index));
    }
}
