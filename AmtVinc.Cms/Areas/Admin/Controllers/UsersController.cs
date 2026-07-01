using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Areas.Admin.Models;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.Domain;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Areas.Admin.Controllers;

/// <summary>
/// Kullanıcı ve rol yönetimi — yalnızca Admin. Admin tüm kullanıcıları oluşturur, rollerini
/// (Admin / İçerik Yöneticisi) atar, aktif/pasif yapar ve şifrelerini sıfırlar. Sistemin
/// kilitlenmesini önleyen korumalar: kendi hesabını silememe/pasifleştirememe ve en az bir
/// aktif admin garantisi.
/// </summary>
public class UsersController : AdminOnlyControllerBase
{
    private readonly ApplicationDbContext _db;

    public UsersController(ApplicationDbContext db) => _db = db;

    private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public async Task<IActionResult> Index()
    {
        var users = await _db.Users.OrderByDescending(u => u.Role).ThenBy(u => u.Username)
            .AsNoTracking().ToListAsync();
        return View(new UsersVm { Users = users, CurrentUserId = CurrentUserId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int id, string username, string fullName, string email,
        UserRole role, bool isActive, string? password)
    {
        username = (username ?? "").Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            TempData["Error"] = AdminMessages.UsernameRequired;
            return RedirectToAction(nameof(Index));
        }

        if (await _db.Users.AnyAsync(u => u.Username == username && u.Id != id))
        {
            TempData["Error"] = AdminMessages.UsernameTaken;
            return RedirectToAction(nameof(Index));
        }

        var isNew = id == 0;
        var user = isNew ? new AppUser() : await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        // Kendi hesabında rol/aktiflik kısıtları (kendini kilitleyip yetkiyi kaybetme).
        if (!isNew && id == CurrentUserId)
        {
            if (role != user.Role) { TempData["Error"] = AdminMessages.CannotChangeOwnRole; return RedirectToAction(nameof(Index)); }
            if (!isActive) { TempData["Error"] = AdminMessages.CannotDeactivateSelf; return RedirectToAction(nameof(Index)); }
        }

        // Son aktif admin'i koru: mevcut bir admin'i pasifleştirme veya rol düşürmeyi engelle.
        if (!isNew && user.Role == UserRole.Admin && (role != UserRole.Admin || !isActive)
            && !await HasOtherActiveAdminAsync(user.Id))
        {
            TempData["Error"] = AdminMessages.LastAdminProtected;
            return RedirectToAction(nameof(Index));
        }

        if (isNew)
        {
            if (string.IsNullOrEmpty(password))
            {
                TempData["Error"] = AdminMessages.PasswordRequired;
                return RedirectToAction(nameof(Index));
            }
        }

        // Şifre verildiyse (yeni kullanıcı ya da değiştirme) politika uygula.
        if (!string.IsNullOrEmpty(password))
        {
            var err = PasswordPolicy.Validate(password);
            if (err is not null) { TempData["Error"] = err; return RedirectToAction(nameof(Index)); }
            user.PasswordHash = PasswordHasher.Hash(password);
        }

        user.Username = username;
        user.FullName = (fullName ?? "").Trim();
        user.Email = (email ?? "").Trim();
        user.Role = role;
        user.IsActive = isActive;
        if (isNew) _db.Users.Add(user);

        await _db.SaveChangesAsync();
        TempData["Success"] = isNew ? AdminMessages.UserCreated : AdminMessages.UserUpdated;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, string password)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        var err = PasswordPolicy.Validate(password);
        if (err is not null) { TempData["Error"] = err; return RedirectToAction(nameof(Index)); }

        user.PasswordHash = PasswordHasher.Hash(password);
        await _db.SaveChangesAsync();
        TempData["Success"] = AdminMessages.UserPasswordReset;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (id == CurrentUserId)
        {
            TempData["Error"] = AdminMessages.CannotDeleteSelf;
            return RedirectToAction(nameof(Index));
        }

        var user = await _db.Users.FindAsync(id);
        if (user is null) return RedirectToAction(nameof(Index));

        if (user.Role == UserRole.Admin && !await HasOtherActiveAdminAsync(user.Id))
        {
            TempData["Error"] = AdminMessages.LastAdminProtected;
            return RedirectToAction(nameof(Index));
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        TempData["Success"] = AdminMessages.UserDeleted;
        return RedirectToAction(nameof(Index));
    }

    private Task<bool> HasOtherActiveAdminAsync(int exceptId) =>
        _db.Users.AnyAsync(u => u.Id != exceptId && u.Role == UserRole.Admin && u.IsActive);
}
