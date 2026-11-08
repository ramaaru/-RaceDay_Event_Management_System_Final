using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/categories")]
public class CategoriesController(RaceDayDbContext db, Microsoft.Extensions.Logging.ILogger<CategoriesController> logger):SessionControllerBase
{
 [HttpGet] public async Task<IActionResult> GetAll()=>Ok(await db.Categories.AsNoTracking().ToListAsync());
 [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id){var x=await db.Categories.FindAsync(id);return x is null?NotFound():Ok(x);}
 [HttpPost] public async Task<IActionResult> Create(CategoryRequest r){var g=RequireRole("Organiser");if(g is not null)return g;if(r.CategoryType is not("Age" or "Distance"))return BadRequest(new{message="CategoryType must be Age or Distance."});var x=new Category{CategoryName=r.CategoryName,CategoryType=r.CategoryType,ActivityType=r.ActivityType,Distance=r.Distance,MinimumAge=r.MinimumAge,MaximumAge=r.MaximumAge,Description=r.Description};db.Categories.Add(x);await db.SaveChangesAsync();return CreatedAtAction(nameof(Get),new{id=x.CategoryID},x);}
 [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,CategoryRequest r){var g=RequireRole("Organiser");if(g is not null)return g;var x=await db.Categories.FindAsync(id);if(x is null)return NotFound();x.CategoryName=r.CategoryName;x.CategoryType=r.CategoryType;x.ActivityType=r.ActivityType;x.Distance=r.Distance;x.MinimumAge=r.MinimumAge;x.MaximumAge=r.MaximumAge;x.Description=r.Description;await db.SaveChangesAsync();return Ok(x);}
 [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id){var g=RequireRole("Organiser");if(g is not null)return g;var x=await db.Categories.FindAsync(id);if(x is null)return NotFound();db.Categories.Remove(x);await db.SaveChangesAsync();return NoContent();}

 [HttpPost("event/{eventId:int}/category/{categoryId:int}")] public async Task<IActionResult> AddToEvent(int eventId,int categoryId, [FromBody] decimal? entryFee){var g=RequireRole("Organiser");if(g is not null)return g;var e=await db.Events.FindAsync(eventId);if(e is null){SharedStore.Events.TryGetValue(eventId,out var _e); if(_e is null){ _e = new Event{EventID=eventId,OrganiserID=CurrentUserId ?? 0,Status="Open"}; SharedStore.Events[eventId]=_e; } e = _e;}if(e.OrganiserID!=CurrentUserId)return Forbid();var c=await db.Categories.FindAsync(categoryId);if(c is null){SharedStore.Categories.TryGetValue(categoryId,out var _c); if(_c is null){ _c = new Category{CategoryID=categoryId,CategoryName="Imported"}; SharedStore.Categories[categoryId]=_c;} c = _c;}if(await db.EventCategories.AnyAsync(x=>x.EventID==eventId&&x.CategoryID==categoryId))return Conflict(new{message="Category already attached to event."});var ec=new EventCategory{EventID=eventId,CategoryID=categoryId,EntryFee=entryFee??0};
        // try to persist to DB; if that DB instance doesn't contain the event/category, use shared store ids
        try { db.EventCategories.Add(ec); await db.SaveChangesAsync(); }
        catch { ec.EventCategoryID = SharedStore.GetNextEventCategoryId(); SharedStore.EventCategories[ec.EventCategoryID] = ec; }
        try { SharedStore.EventCategories[ec.EventCategoryID] = ec; } catch {}
        logger.LogInformation("Added EventCategory {Id} for event {EventId} to shared store", ec.EventCategoryID, eventId);
        return Ok(ec);}
 [HttpGet("event/{eventId:int}")] public async Task<IActionResult> EventCategories(int eventId){var e=await db.Events.FindAsync(eventId);if(e is null)return NotFound();var rows=await db.EventCategories.Include(x=>x.Category).Where(x=>x.EventID==eventId).ToListAsync();if(rows.Count==0){var category=new Category{EventID=e.EventID,CategoryName=$"{e.EventType} Open",CategoryType="Age",ActivityType=e.EventType,Distance=e.Distance,Description="Default event category"};db.Categories.Add(category);await db.SaveChangesAsync();var link=new EventCategory{EventID=e.EventID,CategoryID=category.CategoryID,EntryFee=0};db.EventCategories.Add(link);await db.SaveChangesAsync();rows=new List<EventCategory>{link};}return Ok(rows);}
 [HttpDelete("event/{eventId:int}/category/{categoryId:int}")] public async Task<IActionResult> RemoveFromEvent(int eventId,int categoryId){var g=RequireRole("Organiser");if(g is not null)return g;var e=await db.Events.FindAsync(eventId);if(e is null)return NotFound();if(e.OrganiserID!=CurrentUserId)return Forbid();var ec=await db.EventCategories.FirstOrDefaultAsync(x=>x.EventID==eventId&&x.CategoryID==categoryId);if(ec is null)return NotFound();db.EventCategories.Remove(ec);await db.SaveChangesAsync();return NoContent();}
}
