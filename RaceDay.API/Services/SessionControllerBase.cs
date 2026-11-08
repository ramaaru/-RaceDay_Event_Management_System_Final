using Microsoft.AspNetCore.Mvc;

namespace RaceDay.API.Services;

public abstract class SessionControllerBase : ControllerBase
{
    protected int? CurrentUserId => HttpContext.Session.GetInt32("UserID");
    protected string? CurrentRole => HttpContext.Session.GetString("Role");
    protected bool IsAuthenticated => CurrentUserId.HasValue;

    protected ActionResult? RequireLogin() =>
        IsAuthenticated ? null : Unauthorized(new { message = "Authentication required." });

    protected ActionResult? RequireRole(string role)
    {
        if (!IsAuthenticated) return Unauthorized(new { message = "Authentication required." });
        return string.Equals(CurrentRole, role, StringComparison.OrdinalIgnoreCase)
            ? null : StatusCode(403);
    }
}
