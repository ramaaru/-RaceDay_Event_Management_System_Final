using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/routes")]
public class RoutesController(RaceDayDbContext db):SessionControllerBase
{
 [HttpGet("event/{eventId:int}")] public async Task<IActionResult> GetForEvent(int eventId){var x=await db.Routes.AsNoTracking().FirstOrDefaultAsync(x=>x.EventID==eventId);return x is null?NotFound():Ok(x);}
 [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id){var x=await db.Routes.AsNoTracking().FirstOrDefaultAsync(x=>x.RouteID==id);return x is null?NotFound():Ok(x);}
 [HttpPost("event/{eventId:int}")] public async Task<IActionResult> Create(int eventId,RouteRequest r){var g=RequireRole("Organiser");if(g is not null)return g;var e=await db.Events.FindAsync(eventId);if(e is null)return NotFound();if(e.OrganiserID!=CurrentUserId)return Forbid();if(await db.Routes.AnyAsync(x=>x.EventID==eventId))return Conflict(new{message="A route already exists for this event."});var x=new RaceDay.API.Models.Route{EventID=eventId,RouteName=r.RouteName,Distance=r.Distance,ElevationGain=r.ElevationGain,DifficultyLevel=r.DifficultyLevel,StartPoint=r.StartPoint,EndPoint=r.EndPoint,MapURL=r.MapURL,Description=r.Description};db.Routes.Add(x);await db.SaveChangesAsync();return CreatedAtAction(nameof(Get),new{id=x.RouteID},x);}
 [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,RouteRequest r){var g=RequireRole("Organiser");if(g is not null)return g;var x=await db.Routes.Include(x=>x.Event).FirstOrDefaultAsync(x=>x.RouteID==id);if(x is null)return NotFound();if(x.Event!.OrganiserID!=CurrentUserId)return Forbid();x.RouteName=r.RouteName;x.Distance=r.Distance;x.ElevationGain=r.ElevationGain;x.DifficultyLevel=r.DifficultyLevel;x.StartPoint=r.StartPoint;x.EndPoint=r.EndPoint;x.MapURL=r.MapURL;x.Description=r.Description;await db.SaveChangesAsync();return Ok(x);}
 [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id){var g=RequireRole("Organiser");if(g is not null)return g;var x=await db.Routes.Include(x=>x.Event).FirstOrDefaultAsync(x=>x.RouteID==id);if(x is null)return NotFound();if(x.Event!.OrganiserID!=CurrentUserId)return Forbid();db.Routes.Remove(x);await db.SaveChangesAsync();return NoContent();}
}
