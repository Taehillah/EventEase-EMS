using Microsoft.AspNetCore.Mvc;

namespace EventEase.EMS.Controllers;

public class HomeController : Controller
{
    [Route("Home/Error")]
    public IActionResult Error()
    {
        return View();
    }
}
