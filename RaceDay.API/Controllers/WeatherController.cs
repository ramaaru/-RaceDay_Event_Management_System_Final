using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/weather")]
public class WeatherController(RaceDayDbContext db):SessionControllerBase
{
 [HttpGet("event/{eventId:int}")] public async Task<IActionResult> GetForEvent(int eventId)=>Ok(await db.Weather.AsNoTracking().Where(x=>x.EventID==eventId).OrderByDescending(x=>x.RecordedAt).ToListAsync());
 [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id){var x=await db.Weather.AsNoTracking().FirstOrDefaultAsync(x=>x.WeatherID==id);return x is null?NotFound():Ok(x);}
 [HttpPost("event/{eventId:int}")] public async Task<IActionResult> Create(int eventId,WeatherRequest r){var g=RequireRole("Organiser");if(g is not null)return g;var e=await db.Events.FindAsync(eventId);if(e is null)return NotFound();if(e.OrganiserID!=CurrentUserId)return Forbid();var x=new Weather{EventID=eventId,Temperature=r.Temperature,WeatherCondition=r.WeatherCondition,Humidity=r.Humidity,WindSpeed=r.WindSpeed,RainProbability=r.RainProbability};db.Weather.Add(x);await db.SaveChangesAsync();return CreatedAtAction(nameof(Get),new{id=x.WeatherID},x);}
 [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,WeatherRequest r){var g=RequireRole("Organiser");if(g is not null)return g;var x=await db.Weather.Include(x=>x.Event).FirstOrDefaultAsync(x=>x.WeatherID==id);if(x is null)return NotFound();if(x.Event!.OrganiserID!=CurrentUserId)return Forbid();x.Temperature=r.Temperature;x.WeatherCondition=r.WeatherCondition;x.Humidity=r.Humidity;x.WindSpeed=r.WindSpeed;x.RainProbability=r.RainProbability;x.RecordedAt=DateTime.UtcNow;await db.SaveChangesAsync();return Ok(x);}
}
