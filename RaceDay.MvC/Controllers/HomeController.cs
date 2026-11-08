using Microsoft.AspNetCore.Mvc;
namespace RaceDay.MvC.Controllers;
public class HomeController : Controller { public IActionResult Index()=>View(); public IActionResult Error()=>View(); }