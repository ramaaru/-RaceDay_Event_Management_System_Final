using Microsoft.AspNetCore.Mvc; using RaceDay.MvC.Models; using RaceDay.MvC.Services;
namespace RaceDay.MvC.Controllers;
public class AccountController(RaceDayApiClient api):Controller {
 [HttpGet] public IActionResult Login()=>View(new LoginViewModel());
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Login(LoginViewModel m){if(!ModelState.IsValid)return View(m);HttpContext.Session.Remove("RaceDayApiCookie");var r=await api.Send<JsonAuth>(HttpMethod.Post,"api/auth/login",m);if(!r.Ok){ModelState.AddModelError("",r.Error??"Login failed.");return View(m);}if(r.Data is null){ModelState.AddModelError("","The API returned no user details.");return View(m);}HttpContext.Session.SetInt32("UserID",r.Data.UserID);HttpContext.Session.SetString("Role",r.Data.Role);HttpContext.Session.SetString("FullName",r.Data.FirstName+" "+r.Data.LastName);HttpContext.Session.SetString("Email",r.Data.Email);return RedirectToAction("Index",r.Data.Role=="Organiser"?"Organiser":"Participant");}
 [HttpGet] public IActionResult Register()=>View(new RegisterViewModel());
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Register(RegisterViewModel m){if(!ModelState.IsValid)return View(m);var r=await api.Send<JsonAuth>(HttpMethod.Post,"api/auth/register",m);if(!r.Ok){ModelState.AddModelError("",r.Error??"Registration failed.");return View(m);}TempData["Success"]="Registration successful. Please log in.";return RedirectToAction(nameof(Login));}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Logout(){await api.SendNoData(HttpMethod.Post,"api/auth/logout");HttpContext.Session.Clear();return RedirectToAction("Index","Home");}
 public record JsonAuth(int UserID,string FirstName,string LastName,string Email,string Role);
}