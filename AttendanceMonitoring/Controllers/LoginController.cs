using AttendanceMonitoring.Models;
using AttendanceMonitoring.Services;
using AttendanceMonitoring.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Claims;

namespace AttendanceMonitoring.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> signInManager;
        private readonly UserManager<AppUser> userManager;
        private readonly IActivityLogService _logService;
        

        //Dependency Injection
        public LoginController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, IActivityLogService logService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this._logService = logService;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]// para hind ma cache ng browser ang login page. Means makakatulong para pag nag back button tapos naka logged in is hindi pupunta ng login page
        public IActionResult Login()
        {

            if (User.Identity.IsAuthenticated) //this set session tracking like in php $_SESSION
            {
                if (User.IsInRole("Admin")) //role checking para sa session. so para kapag pinindot ni user yung back button, dahil sa session is direct lang ulit sya sa designated dashboard nya
                    return RedirectToAction("AdminHome", "Admin");
                if (User.IsInRole("Teacher"))
                    return RedirectToAction("TeacherHome", "Teacher");
                if (User.IsInRole("Secretary"))
                    return RedirectToAction("SecretaryHome", "Secretary");

                return RedirectToAction("Login", "Login");

            }
            return View();
        }
        //ETONG CODE NA ITO IS IF ANG USER AY EMAIL ANG GAMIT PANG LOGIN!

        //[HttpPost]
        //public async Task<IActionResult> Login(LoginViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var user = await userManager.FindByEmailAsync(model.Email);

        //        if (user != null)
        //        {                                                                                     //false yung isPersistent parameter kase di ako nag lagay ng remember me password
        //            var result = await signInManager.PasswordSignInAsync(user.UserName, model.Password, isPersistent: false, false); //PasswordSignInAsync create authentication cookies para hindi na mag set ng session manually

        //            if (result.Succeeded)
        //            {   //IsInRoleAsync is check nya if yung role is belong to a user. THen mag stop agad sya once na nakita nya yung role
        //                //GetRoleAsync naman is ichecheck nya or ifefetch nya lahat ng role,Good for if yung user is maraming role
        //                if (await userManager.IsInRoleAsync(user, "Admin"))
        //                    return RedirectToAction("AdminHome", "Admin");
        //                if (await userManager.IsInRoleAsync(user, "Teacher"))
        //                    return RedirectToAction("TeacherHome", "Teacher");
        //                if (await userManager.IsInRoleAsync(user, "Secretary"))
        //                    return RedirectToAction("SecretaryHome", "Secretary");

        //                //return sa index if walang Role yung account
        //                return RedirectToAction("Index", "Home");

        //            }

        //            ModelState.AddModelError("", "Email or password is incorrect!");
        //            return View(model);
        //        }

        //    }
        //    return View(model);
        //}


        //ETO NAMAN KAPAG MAY ERROR SA PAG CONVERT NG INT INTO STRING

        //[HttpPost]
        //public async Task<IActionResult> Login(LoginViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    if(!int.TryParse(model.SchoolId, out int schoolIdInt))
        //    {
        //        ModelState.AddModelError("", "Invalid School ID Format");
        //        return View(model);
        //    }

        //    var user = await userManager.Users.FirstOrDefaultAsync(u => u.SchoolId == schoolIdInt);

        //    if (user == null)
        //    {
        //        ModelState.AddModelError("", "School ID or Password is incorrect!");
        //        return View(model);
        //    }

        //    var result = await signInManager.PasswordSignInAsync(user.UserName, model.Password, isPersistent: false, false);

        //    if (result.Succeeded)
        //    {
        //        if (await userManager.IsInRoleAsync(user, "Admin"))
        //            return RedirectToAction("AdminHome", "Admin");
        //        if (await userManager.IsInRoleAsync(user, "Teacher"))
        //            return RedirectToAction("TeacherHome", "Teacher");
        //        if (await userManager.IsInRoleAsync(user, "Secretary"))
        //            return RedirectToAction("SecretaryHome", "Secretary");

        //        return RedirectToAction("Index", "Home");
        //    }

        //    ModelState.AddModelError("", "School ID or Password is incorrect!");
        //    return View(model);
        //}

        //DITO NAMAN IS YUNG LOGIC GUMAGAMIT NG MODELSTATE WITHOUT EXCLAMATION

        //[HttpPost]
        //public async Task<IActionResult> Login(LoginViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var user = await userManager.Users.FirstOrDefaultAsync(u => u.SchoolId == model.SchoolId);

        //        if (user != null)
        //        {
        //            var result = await signInManager.PasswordSignInAsync(user.UserName, model.Password, isPersistent: false, false);

        //            if (result.Succeeded)
        //            {
        //                if (await userManager.IsInRoleAsync(user, "Admin"))
        //                    return RedirectToAction("AdminHome", "Admin");
        //                if (await userManager.IsInRoleAsync(user, "Teacher"))
        //                    return RedirectToAction("TeacherHome", "Teacher");
        //                if (await userManager.IsInRoleAsync(user, "Secretary"))
        //                    return RedirectToAction("SecretaryHome", "Secretary");

        //                return RedirectToAction("Index", "Home");
        //            }

        //            ModelState.AddModelError("", "School ID or Password is incorrect!");
        //            return View(model);
        //        }
        //    }
        //    return View(model);
        //}

        //DITO NAMAN IS YUNG LOGIC GUMAGAMIT NG MODELSTATE WITH EXCLAMATION

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.Users.FirstOrDefaultAsync(u => u.SchoolId == model.SchoolId);

            if (user != null)
            {
                var result = await signInManager.PasswordSignInAsync(user.UserName, model.Password, isPersistent: false, false);

                if (result.Succeeded)
                {
                    //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var userId = user.Id;  // or User.FindFirstValue(ClaimTypes.NameIdentifier)
                    var username = user.UserName;
                    var schoolId = user.SchoolId;

                    await _logService.LogActivity(
                           actionType: "Login",  // Changed to "Login" since result.Succeeded
                           entityName: "User",
                           entityId: userId,
                           userId: userId,
                           schoolId: schoolId,
                           details: $"User {username} logged in successfully",
                           username: username
                    );

                    if (await userManager.IsInRoleAsync(user, "Admin"))
                        return RedirectToAction("AdminHome", "Admin");
                    if (await userManager.IsInRoleAsync(user, "Teacher"))
                        return RedirectToAction("TeacherHome", "Teacher");
                    if (await userManager.IsInRoleAsync(user, "Secretary"))
                        return RedirectToAction("SecretaryHome", "Secretary");

                                   
                    //return RedirectToAction("Index", "Login");
                    return RedirectToAction("Index", "Home");

                }

                ModelState.AddModelError("", "School ID or Password is incorrect!");
                return View(model);
            }

            ModelState.AddModelError("", "School ID or Password is null!");
            return View(model);
        }
    }
}
