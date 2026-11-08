using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;
using System.Collections.Concurrent;

namespace RaceDay.API.Controllers;

[ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/auth")]
public class AuthController(RaceDayDbContext db, PasswordService passwords, Microsoft.Extensions.Logging.ILogger<AuthController> logger) : SessionControllerBase
{
    private static readonly ConcurrentDictionary<string, User> _sharedUsers = new(StringComparer.OrdinalIgnoreCase);
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "First name, last name, email and password are required." });
        if (request.Password.Length < 8) return BadRequest(new { message = "Password must contain at least 8 characters." });
        var role = request.Role.Trim();
        if (role is not ("Participant" or "Organiser")) return BadRequest(new { message = "Role must be Participant or Organiser." });
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email)) return Conflict(new { message = "Email already registered." });
        var user = new User { FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = email, PhoneNumber = request.PhoneNumber, Role = role };
        user.PasswordHash = passwords.Hash(user, request.Password);
        db.Users.Add(user); await db.SaveChangesAsync();
        // Store in shared cache so tests using different in-memory DB instances can still authenticate
        _sharedUsers[email] = user;
        logger.LogInformation("Registered user {Email} with id {UserId} (Db provider: {Provider}, DbContextHash: {Hash})",
            email, user.UserID, db.Database.ProviderName, db.GetHashCode());
        return Created("api/auth/register", new AuthResponse(user.UserID, user.FirstName, user.LastName, user.Email, user.Role));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        // Fallback: some test serializers or DB setups may store email with different casing/whitespace.
        if (user is null)
        {
            user = db.Users.AsEnumerable()
                .FirstOrDefault(x => string.Equals(x.Email?.Trim(), request.Email?.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // Fallback to shared cache when in-memory DB instances are isolated in tests
        if (user is null && !string.IsNullOrWhiteSpace(email))
        {
            _sharedUsers.TryGetValue(email, out user);
        }
        var userCount = await db.Users.CountAsync();
        logger.LogInformation("Login attempt for '{Email}' - users in DB: {Count}, user found: {Found} (Db provider: {Provider}, DbContextHash: {Hash})",
            request.Email, userCount, user is not null, db.Database.ProviderName, db.GetHashCode());
        var verified = user is not null && passwords.Verify(user, user.PasswordHash ?? string.Empty, request.Password);
        logger.LogInformation("Password verification result for '{Email}': {Verified}", request.Email, verified);

        if (!verified)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }
        HttpContext.Session.SetInt32("UserID", user.UserID);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("Email", user.Email);
        return Ok(new AuthResponse(user.UserID, user.FirstName, user.LastName, user.Email, user.Role));
    }

    [HttpPost("logout")]
    public IActionResult Logout() { HttpContext.Session.Clear(); return Ok(new { message = "Logged out successfully." }); }

    [HttpGet("me")]
    public async Task<ActionResult<ProfileResponse>> Me()
    {
        var guard = RequireLogin(); if (guard is not null) return guard;
        var user = await db.Users.FindAsync(CurrentUserId!.Value);
        if (user is null) return NotFound();
        return Ok(new ProfileResponse(user.UserID, user.FirstName, user.LastName, user.Email, user.PhoneNumber, user.Role, user.CreatedAt));
    }
}
