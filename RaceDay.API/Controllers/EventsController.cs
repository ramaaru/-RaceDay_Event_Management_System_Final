using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/events")]
public class EventsController(RaceDayDbContext db, IBlobStorageService blobs) : SessionControllerBase
{
    private static EventResponse Map(Event e) => new(e.EventID,e.OrganiserID,e.EventName,e.Description,e.EventDate,e.StartTime,e.Location,e.Distance,e.EventType,e.StartLocation,e.FinishLocation,e.Status,e.CreatedAt,!string.IsNullOrWhiteSpace(e.BannerBlobName));

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await db.Events.AsNoTracking().OrderBy(e=>e.EventDate).Select(e=>new EventResponse(e.EventID,e.OrganiserID,e.EventName,e.Description,e.EventDate,e.StartTime,e.Location,e.Distance,e.EventType,e.StartLocation,e.FinishLocation,e.Status,e.CreatedAt,e.BannerBlobName != null)).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) { var e=await db.Events.AsNoTracking().FirstOrDefaultAsync(x=>x.EventID==id); return e is null?NotFound():Ok(Map(e)); }

    [HttpPost]
    public async Task<IActionResult> Create(EventCreateRequest r)
    {
        var guard=RequireRole("Organiser"); if(guard is not null)return guard;
        if(r.Distance<=0)return BadRequest(new{message="Distance must be greater than zero."});
        if(r.EventType is not("Run" or "Walk" or "Cycle"))return BadRequest(new{message="Event type must be Run, Walk or Cycle."});
        var e=new Event{OrganiserID=CurrentUserId!.Value,EventName=r.EventName,Description=r.Description,EventDate=r.EventDate,StartTime=r.StartTime,Location=r.Location,Distance=r.Distance,EventType=r.EventType,StartLocation=r.StartLocation,FinishLocation=r.FinishLocation};
        db.Events.Add(e);await db.SaveChangesAsync();
        var category = new Category { EventID = e.EventID, CategoryName = $"{e.EventType} Open", CategoryType = "Age", ActivityType = e.EventType, Distance = e.Distance, Description = "Default event category" };
        db.Categories.Add(category); await db.SaveChangesAsync();
        db.EventCategories.Add(new EventCategory { EventID = e.EventID, CategoryID = category.CategoryID, EntryFee = 0 }); await db.SaveChangesAsync();
        try { SharedStore.Events[e.EventID] = e; } catch { }
        return CreatedAtAction(nameof(Get),new{id=e.EventID},Map(e));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventUpdateRequest r)
    {
        var guard=RequireRole("Organiser");if(guard is not null)return guard;
        var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserID!=CurrentUserId)return Forbid();
        e.EventName=r.EventName;e.Description=r.Description;e.EventDate=r.EventDate;e.StartTime=r.StartTime;e.Location=r.Location;e.Distance=r.Distance;e.EventType=r.EventType;e.StartLocation=r.StartLocation;e.FinishLocation=r.FinishLocation;e.Status=r.Status;await db.SaveChangesAsync();return Ok(Map(e));
    }

    [HttpPost("{id:int}/banner")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file, CancellationToken cancellationToken)
    {
        var guard = RequireRole("Organiser"); if (guard is not null) return guard;
        var e = await db.Events.FindAsync(id);
        if (e is null) return NotFound();
        if (e.OrganiserID != CurrentUserId) return Forbid();
        if (file is null) return BadRequest(new { message = "Select an image to upload." });
        try
        {
            await using var stream = file.OpenReadStream();
            var upload = await blobs.UploadImageAsync(stream, file.Length, file.ContentType, $"events/{id}", cancellationToken);
            var previous = e.BannerBlobName;
            e.BannerBlobName = upload.BlobName;
            e.BannerContentType = upload.ContentType;
            await db.SaveChangesAsync(cancellationToken);
            await blobs.DeleteAsync(previous, cancellationToken);
            return Ok(new { imageUrl = $"/api/events/{id}/banner", e.BannerBlobName });
        }
        catch (InvalidDataException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/banner")]
    public async Task<IActionResult> Banner(int id, CancellationToken cancellationToken)
    {
        var e = await db.Events.AsNoTracking().FirstOrDefaultAsync(x => x.EventID == id, cancellationToken);
        if (e?.BannerBlobName is null || e.BannerContentType is null) return NotFound();
        try
        {
            var image = await blobs.OpenReadAsync(e.BannerBlobName, e.BannerContentType, cancellationToken);
            return image is null ? NotFound() : File(image.Value.Content, image.Value.ContentType);
        }
        catch (InvalidOperationException ex) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var guard=RequireRole("Organiser");if(guard is not null)return guard;
        var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserID!=CurrentUserId)return Forbid();

        var eventCategoryIds=await db.EventCategories.Where(x=>x.EventID==id).Select(x=>x.EventCategoryID).ToListAsync();
        var categoryIds=await db.EventCategories.Where(x=>x.EventID==id).Select(x=>x.CategoryID).Distinct().ToListAsync();
        var enrolments=await db.EventEnrolments.Where(x=>eventCategoryIds.Contains(x.EventCategoryID)).ToListAsync();
        var enrolmentIds=enrolments.Select(x=>x.EnrolmentID).ToList();
        var results=await db.Results.Where(x=>enrolmentIds.Contains(x.EnrolmentID)).ToListAsync();
        var eventCategories=await db.EventCategories.Where(x=>x.EventID==id).ToListAsync();
        var categories=await db.Categories.Where(x=>categoryIds.Contains(x.CategoryID)).ToListAsync();
        var routes=await db.Routes.Where(x=>x.EventID==id).ToListAsync();
        var weather=await db.Weather.Where(x=>x.EventID==id).ToListAsync();

        db.Results.RemoveRange(results);
        db.EventEnrolments.RemoveRange(enrolments);
        db.EventCategories.RemoveRange(eventCategories);
        db.Categories.RemoveRange(categories);
        db.Routes.RemoveRange(routes);
        db.Weather.RemoveRange(weather);
        await blobs.DeleteAsync(e.BannerBlobName);
        db.Events.Remove(e);
        await db.SaveChangesAsync();
        SharedStore.Events.TryRemove(id, out _);
        return NoContent();
    }

    [HttpGet("{id:int}/enrolments")]
    public async Task<IActionResult> Enrolments(int id)
    {
        var guard=RequireRole("Organiser");if(guard is not null)return guard;var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserID!=CurrentUserId)return Forbid();
        var rows=await db.EventEnrolments.Include(x=>x.User).Include(x=>x.EventCategory).ThenInclude(x=>x!.Category).Where(x=>x.EventCategory!.EventID==id).Select(x=>new{x.EnrolmentID,x.UserID,Participant=x.User!.FirstName+" "+x.User.LastName,x.EventCategoryID,Category=x.EventCategory!.Category!.CategoryName,x.EnrolmentDate,x.RaceNumber,x.EnrolmentStatus}).ToListAsync();return Ok(rows);
    }
}
