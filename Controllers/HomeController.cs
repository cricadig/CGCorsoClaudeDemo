using Microsoft.AspNetCore.Mvc;

namespace CavagnaDemo.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
