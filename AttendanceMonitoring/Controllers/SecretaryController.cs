using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoring.Controllers
{
    public class SecretaryController : Controller
    {
        public IActionResult SecretaryHome()
        {
            return View();
        }
    }
}
