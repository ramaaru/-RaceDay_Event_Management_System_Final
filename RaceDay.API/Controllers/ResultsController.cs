using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/results")]
public class ResultsController(RaceDayDbContext db):SessionControllerBase
{
 [HttpPost] public async Task<IActionResult> Create(ResultRequest r){var g=RequireRole("Organiser");if(g is not null)return g;var e=await db.EventEnrolments.Include(x=>x.EventCategory).ThenInclude(x=>x!.Event).FirstOrDefaultAsync(x=>x.EnrolmentID==r.EnrolmentID);if(e is null)return NotFound("Enrolment not found.");if(e.EventCategory!.Event!.OrganiserID!=CurrentUserId)return Forbid();if(await db.Results.AnyAsync(x=>x.EnrolmentID==r.EnrolmentID))return Conflict(new{message="Result already exists."});var x=new Result{EnrolmentID=r.EnrolmentID,FinishTime=r.FinishTime,Position=r.Position,CategoryPosition=r.CategoryPosition,Pace=r.Pace,ResultStatus=r.ResultStatus};db.Results.Add(x);e.EnrolmentStatus="Completed";await db.SaveChangesAsync();return CreatedAtAction(nameof(Get),new{id=x.ResultID},x);}
 [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,ResultRequest r){var g=RequireRole("Organiser");if(g is not null)return g;var x=await db.Results.Include(x=>x.Enrolment).ThenInclude(x=>x!.EventCategory).ThenInclude(x=>x!.Event).FirstOrDefaultAsync(x=>x.ResultID==id);if(x is null)return NotFound();if(x.Enrolment!.EventCategory!.Event!.OrganiserID!=CurrentUserId)return Forbid();x.FinishTime=r.FinishTime;x.Position=r.Position;x.CategoryPosition=r.CategoryPosition;x.Pace=r.Pace;x.ResultStatus=r.ResultStatus;await db.SaveChangesAsync();return Ok(x);}
 [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id){var g=RequireLogin();if(g is not null)return g;var x=await db.Results.Include(x=>x.Enrolment).ThenInclude(x=>x!.User).FirstOrDefaultAsync(x=>x.ResultID==id);if(x is null)return NotFound();if(CurrentRole=="Participant"&&x.Enrolment!.UserID!=CurrentUserId)return Forbid();if(CurrentRole=="Organiser"){var ok=await db.Results.Where(r=>r.ResultID==id).Select(r=>r.Enrolment!.EventCategory!.Event!.OrganiserID).FirstOrDefaultAsync();if(ok!=CurrentUserId)return Forbid();}return Ok(x);}
 [HttpGet("me")] public async Task<IActionResult> Mine(){var g=RequireRole("Participant");if(g is not null)return g;return Ok(await db.Results.Include(x=>x.Enrolment).ThenInclude(x=>x!.EventCategory).ThenInclude(x=>x!.Event).Where(x=>x.Enrolment!.UserID==CurrentUserId).ToListAsync());}
}
