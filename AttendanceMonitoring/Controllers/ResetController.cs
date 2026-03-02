using Microsoft.AspNetCore.Mvc;
using AttendanceMonitoring.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AttendanceMonitoring.ViewModel.Reset;
using AttendanceMonitoring.Data;


namespace AttendanceMonitoring.Controllers
{
    public class ResetController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ResetController(UserManager<AppUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        public IActionResult VerifyId()
        {
            return View();
        }
        public IActionResult PasswordReset()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyId(VerifyIdViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.SchoolId == model.SchoolId);

            if(user != null)
            {
                return RedirectToAction("PasswordReset", "Reset", new { id = user.SchoolId });
            }
            else
            {
                ModelState.AddModelError("", "Id does not exist");
                return View(model);
            }

        }

        [HttpGet]
        public async Task<IActionResult> PasswordReset(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.SchoolId == id);

            if(user == null)
            {
                return RedirectToAction("VerifyId", "Reset");
            }

            var model = new PasswordResetViewModel()
            {
                SchoolId = user.SchoolId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PasswordReset(PasswordResetViewModel model)
        {

            if (!ModelState.IsValid)
            {
                return View();
            }

            var user =  await _userManager.Users.FirstOrDefaultAsync(u => u.SchoolId == model.SchoolId);

            if(user != null)
            {
                var result = await _userManager.RemovePasswordAsync(user);

                if (result.Succeeded)
                {
                    result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                    return RedirectToAction("Login", "Login");
                }
                else
                {
                    foreach(var errors in result.Errors)
                    {
                        ModelState.AddModelError("", errors.Description);
                    }

                    return View(model);
                }

            }
            ModelState.AddModelError("", "Something went wrong.");

            return View(model);
        }
    }
}
