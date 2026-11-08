using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/users")]
public class UsersController(RaceDayDbContext db, IBlobStorageService blobs) : SessionControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var guard = RequireLogin(); if (guard is not null) return guard;
        var user = await db.Users.FindAsync(CurrentUserId!.Value); if (user is null) return NotFound();
        return Ok(new { user.UserID, user.FirstName, user.LastName, user.Email, user.Role, user.PhoneNumber, user.CreatedAt, HasProfileImage = user.ProfileImageBlobName is not null });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var guard = RequireLogin(); if (guard is not null) return guard;
        var user = await db.Users.FindAsync(CurrentUserId!.Value); if (user is null) return NotFound();
        user.FirstName = request.FirstName.Trim(); user.LastName = request.LastName.Trim(); user.PhoneNumber = request.PhoneNumber;
        await db.SaveChangesAsync();
        return Ok(new { user.UserID, user.FirstName, user.LastName, user.Email, user.Role, user.PhoneNumber, user.CreatedAt, HasProfileImage = user.ProfileImageBlobName is not null });
    }

    [HttpPost("me/profile-picture")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file, CancellationToken cancellationToken)
    {
        var guard = RequireLogin(); if (guard is not null) return guard;
        var user = await db.Users.FindAsync([CurrentUserId!.Value], cancellationToken); if (user is null) return NotFound();
        if (file is null) return BadRequest(new { message = "Select an image to upload." });
        try
        {
            await using var stream = file.OpenReadStream();
            var upload = await blobs.UploadImageAsync(stream, file.Length, file.ContentType, $"profiles/{user.UserID}", cancellationToken);
            var previous = user.ProfileImageBlobName;
            user.ProfileImageBlobName = upload.BlobName; user.ProfileImageContentType = upload.ContentType;
            await db.SaveChangesAsync(cancellationToken); await blobs.DeleteAsync(previous, cancellationToken);
            return Ok(new { imageUrl = "/api/users/me/profile-picture" });
        }
        catch (InvalidDataException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message }); }
    }

    [HttpGet("me/profile-picture")]
    public async Task<IActionResult> ProfilePicture(CancellationToken cancellationToken)
    {
        var guard = RequireLogin(); if (guard is not null) return guard;
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserID == CurrentUserId, cancellationToken);
        if (user?.ProfileImageBlobName is null || user.ProfileImageContentType is null) return NotFound();
        try
        {
            var image = await blobs.OpenReadAsync(user.ProfileImageBlobName, user.ProfileImageContentType, cancellationToken);
            return image is null ? NotFound() : File(image.Value.Content, image.Value.ContentType);
        }
        catch (InvalidOperationException ex) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message }); }
    }
}
