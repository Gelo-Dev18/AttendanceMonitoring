using AttendanceMonitoring.Models;
using AttendanceMonitoring.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AttendanceMonitoring.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> signInManager;
        private readonly UserManager<AppUser> userManager;

                                //Dependency Injection
        public LoginController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
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

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(model.Email);

                if(user != null)
                {                                                                                     //false yung isPersistent parameter kase di ako nag lagay ng remember me password
                    var result = await signInManager.PasswordSignInAsync(user.UserName, model.Password, isPersistent: false, false); //PasswordSignInAsync create authentication cookies para hindi na mag set ng session manually

                    if (result.Succeeded)
                    {   //IsInRoleAsync is check nya if yung role is belong to a user. THen mag stop agad sya once na nakita nya yung role
                        //GetRoleAsync naman is ichecheck nya or ifefetch nya lahat ng role,Good for if yung user is maraming role
                        if(await userManager.IsInRoleAsync(user, "Admin"))
                            return RedirectToAction("AdminHome", "Admin");
                        if (await userManager.IsInRoleAsync(user, "Teacher"))
                            return RedirectToAction("TeacherHome", "Teacher");
                        if (await userManager.IsInRoleAsync(user, "Secretary"))
                            return RedirectToAction("SecretaryHome", "Secretary");

                        //return sa index if walang Role yung account
                        return RedirectToAction("Index", "Home");
                        
                    }
                    
                    ModelState.AddModelError("", "Email or password is incorrect!");
                    return View(model);
                }

            }
            return View(model);
        }
        
    }
}
