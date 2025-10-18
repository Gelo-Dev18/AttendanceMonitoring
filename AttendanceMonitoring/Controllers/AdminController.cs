using AttendanceMonitoring.Data;
using AttendanceMonitoring.Models;
using AttendanceMonitoring.ViewModel;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NuGet.DependencyResolver;
using System.Data;
using System.Net.NetworkInformation;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static NuGet.Packaging.PackagingConstants;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AttendanceMonitoring.Controllers
{
    
    public class AdminController : Controller
    {
        //fields         //type of class: generic class. class type parin sila
                                       //<> means generic parang placeholder. Sinasabi mo: "SignInManager for AppUser type"
                                                //variable
        private readonly SignInManager<AppUser> signInManager;
        private readonly UserManager<AppUser> userManager;
                         //Hindi generic kase walang <>
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment; //Accessing Static Files: Use WebRootPath to locate static files like images, CSS, or JavaScript stored in the wwwroot directory.

        //private readonly UserManager<IdentityUser> _userManager;


        //constructor           //parameters
                                //Dependency Injection
                                                      //eto mismo yung parameter: signInManager, tapos object dn sya pero yung laman nya, example if yung parameter is name then ang object is 'Juan'
        public AdminController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            // so you can use them in any method inside the controller.
            // eto nayung ininject sa conrstructor
            //These four lines assign the injected parameters to the class fields

            this.signInManager = signInManager;
            this.userManager = userManager;
            this.context = context;
            this.environment = environment;

            //this._userManager = _userManager;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // disabled caching para kapag pinindot back button sa isang browser at naka logged out na eh hindi na babalik sa specific user dashboard
        [Authorize(Roles = "Admin")]
        public IActionResult AdminHome()
        {
            return View();
        }
        public IActionResult TeacherList()//string TeacherRole
        {

            //var teacherRoleId = context.Roles
            //    .Where(r => r.Name == "Teacher")
            //    .Select(r => r.Id)
            //    .FirstOrDefault();
            //var teacher = context.Users
            //            .Where(user => context.UserRoles
            //            .Any(ur => ur.UserId == user.Id && ur.RoleId == teacherRoleId))
            //            .ToList();

            var teacher = context.Users
                .Where(user => context.UserRoles
                .Any(ur => ur.UserId == user.Id && context.Roles
                .Any(r => r.Id == ur.RoleId && r.Name == "Teacher")))
                .ToList();

            return View(teacher);
        }

        [HttpGet]
        public IActionResult EditTeacher(string id)
        {
            var teacher = context.Users.Find(id);

            if (teacher == null)
            {
                return RedirectToAction("TeacherList", "Admin");
            }

            var model = new EditTeacherViewModel()
            {
                Email = teacher.Email,
                //UserName = teacher.Email,
                SchoolId = teacher.SchoolId,
                EmployeeId = teacher.EmployeeId,
                FirstName = teacher.FirstName,
                MiddleName = teacher.MiddleName,
                LastName = teacher.LastName,
                Sex = teacher.Sex,
                positionTitle = teacher.positionTitle,
            };

            ViewData["imageFileData"] = teacher.imageFileData;
            ViewData["imageFilePath"] = teacher.imageFilePath;
            ViewData["CreatedAt"] = teacher.CreatedAt.ToString("MM/dd/yyyy");

            return PartialView("_EditTeacherPartial", model);
        }

        [HttpGet]
        public IActionResult ViewTeacher(string id)
        {
            var teacher = context.Users.Find(id);

            if (teacher == null)
            {
                return RedirectToAction("TeacherList", "Admin");
            }

            var model = new EditTeacherViewModel()
            {
                Email = teacher.Email,
                //UserName = teacher.Email,
                SchoolId = teacher.SchoolId,
                EmployeeId = teacher.EmployeeId,
                FirstName = teacher.FirstName,
                MiddleName = teacher.MiddleName,
                LastName = teacher.LastName,
                Sex = teacher.Sex,
                positionTitle = teacher.positionTitle,
                imageFilePath = teacher.imageFilePath,
            };

            ViewData["imageFileData"] = teacher.imageFileData;
            //ViewData["imageFilePath"] = teacher.imageFilePath;
            ViewData["CreatedAt"] = teacher.CreatedAt.ToString("MM/dd/yyyy");

            return PartialView("_ViewTeacherPartial", model);
        }
        public IActionResult StudentList()
        {
            return View();
        }

        public IActionResult SecretaryList()
        {
            return View();
        }
        public IActionResult AddTeacher()
        {
            return PartialView("_AddTeacherPartial");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher(TeacherViewModel model)
        {
            bool teacherFirstLastNameExist = await context.Users.AnyAsync(t => t.FirstName == model.FirstName && t.MiddleName == model.MiddleName && t.LastName == model.LastName);

            if (teacherFirstLastNameExist)
            {
                ModelState.AddModelError("FirstName", "A Teacher with this Full name already exists");
                ModelState.AddModelError("MiddleName", "");
                ModelState.AddModelError("LastName", "");
            }

            bool schoolIdExisted = await context.Users.AnyAsync(s => s.SchoolId == model.SchoolId);

            if (schoolIdExisted)
            {
                ModelState.AddModelError("SchoolId", "School Id is already taken!");
            }

            //Gagamitin to kapag gusto kong gumawa ng sarili kong validation sa Email existed kase may sariling validation si userManager.AnyAsync() about sa email exist
            //bool EmailIsExisted = await context.Users.AnyAsync(e => e.Email == model.Email);

            //if (EmailIsExisted)
            //{
            //    ModelState.AddModelError("Email", "Email is Existed!");
            //}
            if (ModelState.IsValid)
            {
                string? saveImagePath = null;
                byte[]? saveImageData = null;

                if (model.imageFile != null)
                {
                    //In this code, it creates a unique file name for the image using date and time
                    string newFile = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    newFile += Path.GetExtension(model.imageFile.FileName);
                    //create physical path ng image kung saan masesave yung image ex. sa webrooth file named ProfilePic
                    string imageFullPath = environment.WebRootPath + "/ProfilePic/" + newFile;
                    //Sine - save yung actual image file sa wwwroot/ ProfilePic / folder
                    using (var stream = System.IO.File.Create(imageFullPath))
                    {
                        await model.imageFile.CopyToAsync(stream);
                    }
                    saveImagePath = newFile;

                    using (var inputStream = model.imageFile.OpenReadStream())
   
                    //I-convert yung file sa byte array //Using() statement is used for files, database connection etc.
                    using (var memoryStream = new MemoryStream())
                    {
                        await inputStream.CopyToAsync(memoryStream);
                        //await model.imageFile.CopyToAsync(memoryStream);
                        saveImageData = memoryStream.ToArray();
                    }

                }

                AppUser teacher = new AppUser()
                {
                    Email = model.Email,
                    UserName = model.Email,
                    SchoolId = model.SchoolId,
                    EmployeeId = model.EmployeeId,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    LastName = model.LastName,
                    Sex = model.Sex,
                    positionTitle = model.positionTitle,
                    imageFileData = saveImageData,
                    imageFilePath = saveImagePath,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(teacher, model.Password); //.CreateAsync has a build in validation so if email existed it will return an error

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(teacher, "Teacher"); //Assign Teacher role when registered!
                    return Json(new { success = true, message = "Teacher Added Successfully" }); //transfer a message to client side from server side 
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        //eto gagamitin if aasa ako sa built in valdiation ni userManager.CreateAsync(); para ang lalabas is eto ModelState.AddModelError("Email", "Email is already used!");
                        if (error.Code == "DuplicateUserName")
                        {
                            ModelState.AddModelError("Email", "Email is already used!");
                        }else if (error.Description.Contains("Password"))
                        {
                            ModelState.AddModelError("Password", error.Description);
                        }
                        else
                        {
                            ModelState.AddModelError("", error.Description); //general error at isesesnd kay asp-validation-summary
                        }


                        //Eto ang gagamitin ko kapag may sarili akong validation if email is existed!
                        //ModelState.AddModelError("", error.Description); //general error at isesesnd kay asp-validation-summary

                    }
                    var errors = ModelState.ToDictionary(
                                            kvp => kvp.Key,
                                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                                        );

                    return Json(new { success = false, errors = errors });
                }

                //return PartialView("TeacherList", teacher);

            }
            else
            {
                var errors = ModelState.ToDictionary(
                                                       kvp => kvp.Key,
                                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                                                   );

                return Json(new { success = false, errors = errors });
            }
            //return PartialView("_AddTeacherPartial", model);

            //to see actual error in devtools
            //try
            //{
            //}
            //catch (Exception ex)
            //{ 
            //    return Json(new { success = false, message = $"Error: {ex.Message}", stackTrace = ex.StackTrace });
            //}
        }

        //[HttpPut] // ginagamit lang sa mga restful api 
        [HttpPost] //ginagamit parin ang post kagit sa pag update sa mvc kase ang form is only support post and get
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(string id, EditTeacherViewModel model)
        {
            var editTeacher = await context.Users.FindAsync(id);

            if(editTeacher == null)
            {
                return Json(new { success = false, message = "Teacher not found" });
            }
            //check for email duplication
            bool sameEmail = await context.Users.AnyAsync(e => e.Email == model.Email && e.Id != id);

            if (sameEmail)
            {
                ModelState.AddModelError("Email", "Email is already used!");
            }

            //duplicate check excluding self
            //Dito gumamit ng s.Id != id para pag nag check ng id is hindi isasama yung current id sa pag hahanap
            //check for Schoold Id Duplication
            bool schoolIdExisted = await context.Users.AnyAsync(s => s.SchoolId == model.SchoolId && s.Id != id);

            if (schoolIdExisted)
            {
                ModelState.AddModelError("SchoolId", "School Id is already taken!");
            }

            //Check for Employee Id duplication
            bool employeeNoExisted = await context.Users.AnyAsync(e => e.EmployeeId == model.EmployeeId && e.Id != id);
            if (employeeNoExisted)
            {
                ModelState.AddModelError("EmployeeId", "Employee Id is already taken!");
            }

            //Check if Full name duplication
            bool FullNameExisted = await context.Users.AnyAsync(f => f.FirstName == model.FirstName && f.MiddleName == model.MiddleName && f.LastName == model.LastName && f.Id != id);
            if (FullNameExisted)
            {
                ModelState.AddModelError("FirstName", "A teacher with this Full name is already existed");
                ModelState.AddModelError("MiddleName", " ");
                ModelState.AddModelError("LastName", " ");
            }

            if (!ModelState.IsValid)
            {
                ViewData["imageFileData"] = editTeacher.imageFileData;
                ViewData["imageFilePath"] = editTeacher.imageFilePath;
                ViewData["CreatedAt"] = editTeacher.CreatedAt.ToString("MM/dd/yyyy");

                var errors = ModelState.ToDictionary(
                                                                       kvp => kvp.Key,
                                                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                                                                   );

                return Json(new { success = false, errors = errors });
            }

            string? saveImagePath = null;
            byte[]? saveImageData = null;

            if(model.imageFile != null)
            {
                string newFile = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                newFile += Path.GetExtension(model.imageFile.FileName);

                string imageFullPath = environment.WebRootPath + "/ProfilePic/" + newFile;

                using(var stream = System.IO.File.Create(imageFullPath))
                {
                    await model.imageFile.CopyToAsync(stream);
                }

                string oldImageFullPath = environment.WebRootPath + "/ProfilePic/" + editTeacher.imageFilePath;
                System.IO.File.Delete(oldImageFullPath);

                saveImagePath = newFile;

                using(var inputStream = model.imageFile.OpenReadStream())
                using(var memoryStream = new MemoryStream())
                {
                    await inputStream.CopyToAsync(memoryStream);
                    saveImageData = memoryStream.ToArray();
                }
                //IMPORTANT: Assign to editTeacher
                editTeacher.imageFilePath = saveImagePath;
                editTeacher.imageFileData = saveImageData;
            }

            editTeacher.Email = model.Email;
            editTeacher.SchoolId = model.SchoolId;
            editTeacher.EmployeeId = model.EmployeeId;
            editTeacher.FirstName = model.FirstName;
            editTeacher.MiddleName = model.MiddleName;
            editTeacher.LastName = model.LastName;
            editTeacher.Sex = model.Sex;
            editTeacher.positionTitle = model.positionTitle;

            var result = await userManager.UpdateAsync(editTeacher);

            if (!result.Succeeded)
            {
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                //return PartialView("_EditTeacherPartial", model);
                var errors = ModelState.ToDictionary(
                                                                       kvp => kvp.Key,
                                                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                                                                   );

                return Json(new { success = false, errors = errors });
            }

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var removePassword = await userManager.RemovePasswordAsync(editTeacher);
                if (removePassword.Succeeded)
                {
                    var addPassword = await userManager.AddPasswordAsync(editTeacher, model.NewPassword);
                    if (!addPassword.Succeeded)
                    {
                        foreach(var error in addPassword.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        //parehas silang babalik sa form , ang pinagkaibahan lang is:
                        //return View(model); // eto babalik sa same page 
                        //return PartialView("_EditTeacherPartial", model); // eto babalik sa same form kase naka modal yung form
                        var errors = ModelState.ToDictionary(
                                                       kvp => kvp.Key,
                                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                                                   );

                        return Json(new { success = false, errors = errors });
                    }
                }
            }

            //No need ng gamitin ang SaveChangesAsync() kase Ang UserManager.UpdateAsync(), RemovePasswordAsync(), at AddPasswordAsync() ay automatically nag-save na sa database.
            //await context.SaveChangesAsync();

            return Json(new { success = true, message = "User Updated Successfully!" });// babalik na sa teacher list table kase walang error and success na sya!

        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            
            var teacher = await context.Users.FindAsync(id);

            if(teacher == null)
            {
                return RedirectToAction("TeacherList", "Admin");
            }
            //check kung may laman yung image yung user
            if (!string.IsNullOrEmpty(teacher.imageFilePath))
            {
                //string ImagePath = environment.WebRootPath + "/ProfilePic/" + teacher.imageFilePath;
                                   //Path.Combine, static method within System.IO.Path
                string ImagePath = Path.Combine(environment.WebRootPath, "ProfilePic", teacher.imageFilePath);// si Path.Combine is gumagamit ng correct directorty seprator para imbis na "/ProfilePic/ anggamitin is sya na mismo ang bahala kase minsan may mga dobleng slash, kaya pwedeng mag error!
                //check if existing  paba talaga sa ProfilePic yung file
                if (System.IO.File.Exists(ImagePath))
                {
                    System.IO.File.Delete(ImagePath);
                }
            }

            context.Users.Remove(teacher);
            await context.SaveChangesAsync();

            //return RedirectToAction("TeacherList", "Admin");
            return Json(new { success = true, message = "Teacher has been Deleted successfully" }); //JSON store and transport data from server side to client side

        }
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Login");

        }
    }

    
}
