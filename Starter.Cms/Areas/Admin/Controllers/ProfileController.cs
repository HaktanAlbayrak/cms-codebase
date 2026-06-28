using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Starter.Cms.Data;
using Starter.Cms.Domain;
using Starter.Cms.Services;

namespace Starter.Cms.Areas.Admin.Controllers;

/// <summary>
/// "Profilim" — her oturum açmış kullanıcı kendi ad/e-postasını düzenler ve şifresini
/// değiştirir. Rol/aktiflik buradan değişmez (onlar Admin'in kullanıcı yönetimi yetkisinde).
/// </summary>
public class ProfileController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProfileController(ApplicationDbContext db) => _db = db;

    private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public async Task<IActionResult> Index()
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string fullName, string email)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();

        user.FullName = (fullName ?? "").Trim();
        user.Email = (email ?? "").Trim();
        await _db.SaveChangesAsync();
        TempData["Success"] = AdminMessages.ProfileUpdated;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();

        if (!PasswordHasher.Verify(currentPassword ?? "", user.PasswordHash))
        {
            TempData["Error"] = AdminMessages.CurrentPasswordWrong;
            return RedirectToAction(nameof(Index));
        }

        if (newPassword != confirmPassword)
        {
            TempData["Error"] = AdminMessages.PasswordsDoNotMatch;
            return RedirectToAction(nameof(Index));
        }

        var err = PasswordPolicy.Validate(newPassword);
        if (err is not null) { TempData["Error"] = err; return RedirectToAction(nameof(Index)); }

        user.PasswordHash = PasswordHasher.Hash(newPassword);
        await _db.SaveChangesAsync();
        TempData["Success"] = AdminMessages.PasswordChanged;
        return RedirectToAction(nameof(Index));
    }
}
