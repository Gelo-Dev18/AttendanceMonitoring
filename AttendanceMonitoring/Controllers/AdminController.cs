using AttendanceMonitoring.Contracts;
using AttendanceMonitoring.Data;
using AttendanceMonitoring.Helper;
using AttendanceMonitoring.Models;
using AttendanceMonitoring.Services;
using AttendanceMonitoring.ViewModel;
using Dapper;
using Dapper;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering; // para sa SelectListItem
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using NuGet.DependencyResolver;
using NuGet.Packaging.Signing;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static NuGet.Packaging.PackagingConstants;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AttendanceMonitoring.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // disabled caching para kapag pinindot back button sa isang browser at naka logged out na eh hindi na babalik sa specific user dashboard
    [Authorize(Roles = "Admin")]
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

        //For Backup And Restore Feature
        private readonly DatabaseBackupService backupService;
        private readonly ILogger<AdminController> logger;

        private readonly IActivityLogService logService;
        private readonly IActivityLogRepository _repo; //For Pagination
        //private readonly UserManager<IdentityUser> _userManager;

        // **Dependency Injection = How the service is provided** to your controller
        //constructor           //parameters
        //Dependency Injection
        //eto mismo yung parameter: signInManager, tapos object dn sya pero yung laman nya, example if yung parameter is name then ang object is 'Juan'
        public AdminController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment,
                                DatabaseBackupService backupService, ILogger<AdminController> logger, IActivityLogService logService, IActivityLogRepository repo)
        {
            // so you can use them in any method inside the controller.
            // eto nayung ininject sa conrstructor
            //These four lines assign the injected parameters to the class fields

            this.signInManager = signInManager;
            this.userManager = userManager;
            this.context = context;
            this.environment = environment;

            //For Backup And Restore Feature
            this.backupService = backupService;
            this.logger = logger;
            this.logService = logService;
            this._repo = repo;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userId = GetCurrentUserId();
            var user = await userManager.FindByIdAsync(userId);

            //Automatic na set this viewbag to all methods/actions
            ViewBag.UserLastName = user?.LastName ?? "User";
            ViewBag.UserProfilePic = user?.imageFilePath ?? "default-avatar.png";

            await next(); // continue to the actual action
        }
        /// <summary>
        /// HELPER METHODS
        /// </summary>  
        /// <returns></returns>

        protected string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        protected string GetCurrentUsername()
        {
            return User.Identity.Name;
        }

        protected async Task<string> GetCurrentUserSchoolId()
        {
            var userId = GetCurrentUserId();

            var user = await userManager.FindByIdAsync(userId);

            return user.SchoolId;
        }

        protected async Task<(string userId, string username, string schoolId)> GetCurrentUserInfo()
        {
            var userId = GetCurrentUserId();

            var user = await userManager.FindByIdAsync(userId);

            return (userId, user.UserName, user.SchoolId);
        }

        protected async Task<int> GetCurrentAcademicPeriodId()
        {
            var defaultYear = await context.AcademicPeriods.FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            return defaultYear.Id;
        }
        //[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // disabled caching para kapag pinindot back button sa isang browser at naka logged out na eh hindi na babalik sa specific user dashboard
        //[Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminHome()
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return Json(new { success = false, message = "Cannot find user" });
            }

            var studentList = await context.Students.ToListAsync();
            var teacherList = await userManager.GetUsersInRoleAsync("Teacher");
            var subjectList = await context.Subjects.ToListAsync();
            var secretaryList = await userManager.GetUsersInRoleAsync("Secretary");



            var adminHome = new AdminHomeViewModel
            {
                StudentCount = studentList.Count,
                TeacherCount = teacherList.Count,
                SecretaryCount = secretaryList.Count,
                SubjectCount = subjectList.Count,
                LastName = user.LastName,
                imageFilePath = user.imageFilePath
            };

            return View(adminHome);
        }

        [HttpGet]
        public async Task<IActionResult> ManageAccount()
        {
            //var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return Json(new { success = false, message = "Cannot find user" });
            }

            var model = new ManageAccountViewModel()
            {
                LRN = user.SchoolId,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                imageFilePath = user.imageFilePath

            };

            return PartialView("_ManageAccount", model);
        }



        [HttpPost] //Query Parameter
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageAccount(string id, ManageAccountViewModel model)
        {
            var editedUser = await context.Users.FindAsync(id);

            if(editedUser == null)
            {
                return Json(new { success = false, message = "User id could not found!" });
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );
            }

            try
            {

                string? saveImagePath = null;
                byte[]? saveImageData = null;

                if (model.imageFile != null)
                {
                    string newFile = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    newFile += Path.GetExtension(model.imageFile.FileName);

                    string imageFullPath = environment.WebRootPath + "/ProfilePic/" + newFile;

                    using (var stream = System.IO.File.Create(imageFullPath))
                    {
                        await model.imageFile.CopyToAsync(stream);
                    }

                    if (!string.IsNullOrEmpty(editedUser.imageFilePath))
                    {
                        string oldImageFullPath = environment.WebRootPath + "/ProfilePic" + editedUser.imageFilePath;

                        if (oldImageFullPath != null)
                        {
                            System.IO.File.Delete(oldImageFullPath);
                        }
                    }

                    saveImagePath = newFile;

                    using(var inputStream = model.imageFile.OpenReadStream())
                    using(var memoryStream = new MemoryStream())
                    {
                        await inputStream.CopyToAsync(memoryStream);
                        saveImageData = memoryStream.ToArray();
                    }

                    editedUser.imageFilePath = saveImagePath;
                    editedUser.imageFileData = saveImageData;
                }

                //Capitalize every firsy letter
                TextInfo textinfo = CultureInfo.CurrentCulture.TextInfo;

                string formattedFirstName = textinfo.ToTitleCase(model.FirstName.ToLower());
                string formattedMiddleName = textinfo.ToTitleCase(model.MiddleName?.ToLower() ?? "");
                string formattedLastName = textinfo.ToTitleCase(model.LastName.ToLower());


                //editedUser.LRN = model.LRN;
                editedUser.SchoolId = model.LRN;
                editedUser.UserName = model.LRN.ToString();
                editedUser.FirstName = formattedFirstName;
                editedUser.MiddleName = formattedMiddleName;
                editedUser.LastName = formattedLastName;

                var result = await userManager.UpdateAsync(editedUser);

                //Log Activity
                var userInfo = await GetCurrentUserInfo();

                await logService.LogActivity(
                    actionType: "Edit",
                    entityName: "User",
                    entityId: editedUser.Id.ToString(),
                    userId: userInfo.userId,
                    schoolId: userInfo.schoolId,
                    details: $"User {userInfo.username} edited admin user {editedUser.FirstName} {editedUser.MiddleName} {editedUser.LastName}, School Id: {editedUser.SchoolId}",
                    username: userInfo.username
                );

                if (!result.Succeeded)
                {
                    foreach(var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    var errors = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    );

                    return Json(new { success = false, errors = errors });
                }

                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    var removePassword = await userManager.RemovePasswordAsync(editedUser);
                    if (removePassword.Succeeded)
                    {
                        var addPassword = await userManager.AddPasswordAsync(editedUser, model.NewPassword);
                        if (!addPassword.Succeeded)
                        {
                            foreach(var error in addPassword.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }

                            var errors = ModelState.ToDictionary(
                                 kvp => kvp.Key,
                                 kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                            );

                            return Json(new { success = false, errors = errors });
                        }
                    }
                }

                return Json(new { success = true, message = "Admin Updated Successfully!" });

            }
            catch
            {
                return Json(new { success = false, message = "Something went wrong" });
            }

        }

        public async Task<IActionResult> AcademicPeriodList()
        {
            var AcademicPeriodList = await context.AcademicPeriods
                                    .OrderBy(ap => ap.Year)
                                    .ToListAsync();
            return View(AcademicPeriodList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAcademicPeriod(AcademicPeriodViewModel model)
        {
            bool yearExisted = await context.AcademicPeriods.AnyAsync(ap => ap.Year == model.Year && ap.GradingPeriod == model.GradingPeriod);

            if (yearExisted)
            {
                ModelState.AddModelError("Year", "Academic Year is already existed with the same grading Period");
                ModelState.AddModelError("GradingPeriod","");

            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            var AcademicPeriod = new AcademicPeriod()
            {
                Year = model.Year,
                GradingPeriod = model.GradingPeriod,
                CreatedAt = DateTime.Now,
                IsDefault = model.IsDefault = 0, //default to 0 . 0 = NO, 1 = YES
                Status = model.Status = 0, ////default to 0 . 0 = NOT YET STARTED, 1 = STARTED, 2 = CLOSED
            };

            await context.AddAsync(AcademicPeriod);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Added",
                entityName: "AcademicPeriod",
                entityId: AcademicPeriod.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} Added new academic period",
                username: userInfo.username
            );

            return Json(new { success = true, message = "Academic Period successfully added!" });
        }

        [HttpPost]
        public async Task<IActionResult> SetDefaultAcademic (int id)
        {
            var setDefaultAcademic = await context.AcademicPeriods.FindAsync(id);

            if (setDefaultAcademic == null)
            {
                return Json(new { success = false, error = "Id Not Found!" });
            }

            // Use a database transaction to ensure both updates succeed or fail together
            //READ Boiler plate and documentation reviewer.txt for more information about try catch and database transaction
            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    //Find the current default item and change its status to 0( 0 is equals to NO)
                    var currentDefault = await context.AcademicPeriods.FirstOrDefaultAsync(ap => ap.IsDefault == 1);
                    if (currentDefault != null)
                    {
                        currentDefault.IsDefault = 0;
                        context.AcademicPeriods.Update(currentDefault);
                    }
                    //After Setting the default, set the status to be close
                    var currentStatus = await context.AcademicPeriods.FirstOrDefaultAsync(ap => ap.Status == 1);
                    if (currentStatus != null)
                    {
                        currentStatus.Status = 2;
                        context.AcademicPeriods.Update(currentStatus);
                    }
                    //Find the Academic Period to be the new default and set its status to 1 (1 is equals to YES)
                    var newDefault = await context.AcademicPeriods.FirstOrDefaultAsync(ap => ap.Id == id);
                    if (newDefault != null)
                    {
                        newDefault.IsDefault = 1;
                        context.AcademicPeriods.Update(newDefault);
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();// All good → commit transaction // <— this makes the changes permanent // Para syang save button 

                    var userInfo = await GetCurrentUserInfo();

                    await logService.LogActivity(
                        actionType: "Set Default",
                        entityName: "AcademicPeriod",
                        entityId: setDefaultAcademic.Id.ToString(),
                        userId: userInfo.userId,
                        schoolId: userInfo.schoolId,
                        details: $"User {userInfo.username} set new default academic period",
                        username: userInfo.username
                    );

                    return Json(new { success = true, message = "Academic Period set default!" });
                }
                //catch(Exception ex) //use Exception General — kahit anong error, kahit hindi database
                catch (DbUpdateException dbEx) // use DbUpdatedException for specific error na galing sa database
                                              // dbEx is variable. Optional sya. ginagamit lang kung gustong  basahin details ng error.
                {
                    await transaction.RollbackAsync(); // Something failed → rollback
                    return Json(new { success = false, error = "An error occurred while updating the default Academic Period" });

                }
            }
            
        }

        [HttpGet]
        public async Task<IActionResult> EditAcademicPeriod(int id)
        {
            var academicPeriod = await context.AcademicPeriods.FindAsync(id);

            if (academicPeriod == null)
            {
                return Json(new { success = false, error = "Id Not Found!" });

            }



            var model = new EditAcademicPeriodViewModel()
            {
                GradingPeriod = academicPeriod.GradingPeriod,
                Year = academicPeriod.Year,
                Status = academicPeriod.Status,
                IsDefault = academicPeriod.IsDefault
            };

            return PartialView("_EditAcademicPeriodPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAcademicPeriod(int id, EditAcademicPeriodViewModel model)
        {
            var editacademicPeriod = await context.AcademicPeriods.FindAsync(id);

            if (editacademicPeriod == null)
            {
                return Json(new { success = false, error = "Id Not Found!" });

            }

            bool yearExisted = await context.AcademicPeriods.AnyAsync(ap => ap.Year == model.Year && ap.GradingPeriod == model.GradingPeriod && ap.Id != id);

            if (yearExisted)
            {
                ModelState.AddModelError("Year", "Academic Year is already existed with the same grading Period");
                ModelState.AddModelError("GradingPeriod", "");

            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            try
            {
                editacademicPeriod.GradingPeriod = model.GradingPeriod;
                editacademicPeriod.Year = model.Year;
                editacademicPeriod.Status = model.Status;

                context.AcademicPeriods.Update(editacademicPeriod);
                await context.SaveChangesAsync();

                var userInfo = await GetCurrentUserInfo();

                await logService.LogActivity(
                    actionType: "Edit",
                    entityName: "AcademicPeriod",
                    entityId: editacademicPeriod.Id.ToString(),
                    userId: userInfo.userId,
                    schoolId: userInfo.schoolId,
                    details: $"User {userInfo.username} edited {editacademicPeriod.Year} academic period",
                    username: userInfo.username
                );

                return Json(new { success = true, message = "Academic Period Edited Successfully!" });


            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Something went wrong" });
            }

        }
        [HttpDelete]
        public async Task<IActionResult> DeleteAcademicPeriod(int id)
        {
            var AcademicId = await context.AcademicPeriods.FindAsync(id);

            if(AcademicId == null)
            {
                return Json(new { success = false, error = "Id Not Found!" });

            }
            
            var isDefault = await context.AcademicPeriods
                            .Where(ap => ap.IsDefault == 1)
                            .AnyAsync();

            if (isDefault)
            {
               return Json(new { success = false, message = "Cannot delete academic year when set to default!" });
            }

            //If walang Soft delete
            //var hasRecord = await context.AcademicPeriods.AnyAsync(ap => ap.Id == id);

            //if (hasRecord)
            //{
            //    return Json(new { success = false, message = "Cannot Delete Academic year when has already a record!" });
            //}
            //context.AcademicPeriods.Remove(AcademicId);
            //await context.SaveChangesAsync();

            //Using Soft delete
            AcademicId.IsDeleted = true;
            AcademicId.DeletedAt = DateTime.UtcNow;

            context.AcademicPeriods.Update(AcademicId);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Delete",
                entityName: "AcademicPeriod",
                entityId: AcademicId.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} deleted {AcademicId.Year} - {AcademicId.GradingPeriod} academic period",
                username: userInfo.username
            );

            return Json(new { success = true, message = "Academic Period Deleted Successfully!" });

        }

        [HttpGet]
        public async Task<IActionResult> RestoreAcademicPeriod()
        {
            var deletedAcademicPeriod = await context.AcademicPeriods
                .IgnoreQueryFilters()
                .Where(ac => ac.IsDeleted == true)
                .OrderBy(ac => ac.Year)
                .ToListAsync();

            return PartialView("_RestoreAcademicPeriodPartial", deletedAcademicPeriod);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreAcademicPeriod(int academicId)
        {
            var deletedAcademicPeriod = await context.AcademicPeriods
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(ac => ac.Id == academicId);

            if (deletedAcademicPeriod == null)
            {
                return Json(new { success = false, message = "Id could not found" });
            }

            deletedAcademicPeriod.IsDeleted = false;
            deletedAcademicPeriod.DeletedAt = null;

            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Restore",
                entityName: "Academic Period",
                entityId: deletedAcademicPeriod.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Admin {userInfo.username} restore academic period: {deletedAcademicPeriod.Year} {deletedAcademicPeriod.GradingPeriod}",
                username: userInfo.username
            );

            var remainingDeletedAcademic = await context.AcademicPeriods
                .IgnoreQueryFilters()
                .Where(ac => ac.IsDeleted == true)
                .OrderBy(ac => ac.Year)
                .ToListAsync();

            return PartialView("_RestoreAcademicPeriodPartial", remainingDeletedAcademic);
        }
        public async Task<IActionResult> SubjectList()
        {
            var subjectList = await context.Subjects
                .OrderBy(s => s.SubjectDescription)
                .ToListAsync();

            return View(subjectList);
        }

        public async Task<IActionResult> GradeList()
        {
            var GradeList = await context.Grades
                .OrderBy(s => s.GradeLevel)
                .ToListAsync();

            return View(GradeList);
        }

        public async Task<IActionResult> AddAcademicPeriod()
        {
            return PartialView("_AddAcademicPeriodPartial");
        }
        public async Task<IActionResult> AddSubject()
        {
            return PartialView("_AddSubjectPartial");
        }

        [HttpPost]
        [ValidateAntiForgeryToken] //protecting applications from CSRF attacks. //ValidateAntiForgeryToken is recommended for Form submission
        public async Task<IActionResult> AddSubject(SubjectViewModel model)
        {
            bool subjectDescriptionExisted = await context.Subjects.AnyAsync(s => s.SubjectDescription == model.SubjectDescription);
            
            if (subjectDescriptionExisted)
            {
                ModelState.AddModelError("SubjectDescription", "Subject Description is already existed!");
            }
            
            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            var Subject = new Subject()
            {
                SubjectDescription = model.SubjectDescription,
                Category = model.Category,
                CreatedAt = DateTime.Now
            }; 

            await context.Subjects.AddAsync(Subject);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Added",
                entityName: "Subject",
                entityId: Subject.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} added subject successfully",
                username: userInfo.username
            );

            return Json(new { success = true, message = "Subject Added Successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> EditSubject(int id)
        {
            var Subject = await context.Subjects.FindAsync(id);

            if(Subject == null)
            {
                return Json(new { success = false, error = "Subject Not Found!" });
            }

            var model = new EditSubjectViewModel()
            {
                SubjectDescription = Subject.SubjectDescription,
                Category = Subject.Category,

            };

            return PartialView("_EditSubjectPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubject(int id, EditSubjectViewModel model)
        {
            var EditSubject = await context.Subjects.FindAsync(id);

            if (EditSubject == null)
            {
                return Json(new { success = false, error = "Subject Not Found!" });
            }

            bool subjectDescriptionExisted = await context.Subjects.AnyAsync(s => s.SubjectDescription == model.SubjectDescription && s.Id != id);

            if (subjectDescriptionExisted)
            {
                ModelState.AddModelError("SubjectDescription", "Subject Description is already existed!");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            EditSubject.SubjectDescription = model.SubjectDescription;
            EditSubject.Category = model.Category;

            //if (model.Category != "TVL")
            //{
            //    EditSubject.TVLProgram = null;
            //}
            //else
            //{
            //    EditSubject.TVLProgram = model.TVLProgram;
            //}

            context.Subjects.Update(EditSubject);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Edit",
                entityName: "Subject",
                entityId: EditSubject.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} edited subject {EditSubject.SubjectDescription}, {EditSubject.Category} Category",
                username: userInfo.username
            );

            return Json(new { success = true, message = "Subject Edited Successfully!" });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            //var DeletedSubject = await context.Subjects.FindAsync(id);
            var DeletedSubject = await context.Subjects
                .Include(ss => ss.SectionSubjects)
                    .ThenInclude(ta => ta.TeacherAssignments)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (DeletedSubject == null)
            {
                return Json(new { success = false, error = "Subject Not Found!" });
            }

            //var isAssigned = await context.SectionSubjects.AnyAsync(s => s.SubjectId == id);

            //if (isAssigned)
            //{
            //    return Json(new { success = false, message = "Cannot delete Subject when already Assigned!" });

            //}

            //context.Subjects.Remove(DeleteSubject);

            var time = DateTime.UtcNow;

            DeletedSubject.IsDeleted = true;
            DeletedSubject.DeletedAt = time;

            foreach(var sectionSubjects in DeletedSubject.SectionSubjects)
            {
                sectionSubjects.IsDeleted = true;
                sectionSubjects.DeletedAt = time;

                foreach(var ta in sectionSubjects.TeacherAssignments)
                {
                    ta.IsDeleted = true;
                    ta.DeletedAt = time;
                }
            }

            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Delete",
                entityName: "Subject",
                entityId: DeletedSubject.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Admin {userInfo.username} deleted subject {DeletedSubject.SubjectDescription}, {DeletedSubject.Category} Category",
                username: userInfo.username
            );

            return Json(new { success = true, message = "Subject Deleted Successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> RestoreSubject()
        {
            var deletedSubjects = await context.Subjects
                .IgnoreQueryFilters()
                .Where(s => s.IsDeleted == true)
                .ToListAsync();

            return PartialView("_RestoreSubjectPartial", deletedSubjects);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreSubject(int subjectId)
        {
            var deletedSubjects = await context.Subjects
                .IgnoreQueryFilters()
                .Include(ss => ss.SectionSubjects)
                    .ThenInclude(ta => ta.TeacherAssignments)
                .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (deletedSubjects == null)
            {
                return Json(new { success = false, message = "Id could not found" });
            }

            deletedSubjects.IsDeleted = false;
            deletedSubjects.DeletedAt = null;

            foreach (var sectionSubjects in deletedSubjects.SectionSubjects)
            {
                sectionSubjects.IsDeleted = false;
                sectionSubjects.DeletedAt = null;

                foreach (var ta in sectionSubjects.TeacherAssignments)
                {
                    ta.IsDeleted = false;
                    ta.DeletedAt = null;
                }
            }

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Restore",
                entityName: "Subject",
                entityId: deletedSubjects.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Admin {userInfo.username} restore grade {deletedSubjects.SubjectDescription}",
                username: userInfo.username
            );

            var remainingdeletedSection = await context.Subjects
                .IgnoreQueryFilters()
                .Where(g => g.IsDeleted == true)
                .ToListAsync();

            return PartialView("_RestoreSubjectPartial", remainingdeletedSection);
        }


        //public async Task<IActionResult> ManageSectionSubject()
        //{
        //    var assignedSubjectList = await context.SectionSubjects
        //                                           .OrderBy(a => a.SectionId)
        //                                           .ToListAsync();

        //    return View(assignedSubjectList);
        //}

        [HttpGet]
        public async Task<IActionResult> ManageSectionSubject(int id)
        {
            //Displays assigned subjects on an specific Grade and section
            var assignedSubjectList = await context.SectionSubjects
                                                   .Include(ss => ss.Subject) //para sa Razor page is maaccess yun SubjectDescription
                                                   .Where(ss => ss.SectionId == id) //Where SectionId (FK) in SectionSubjects Db is equals to int db
                                                   .ToListAsync();

            var GradeSection = await context.Sections
                .Include(g => g.Grade) // dahil sa Nav.Property/Lazy loading na nakadeclare sa Section.cs kaya gumana ang .Include
                .FirstOrDefaultAsync(g => g.Id == id); //Retrieve single data
                                                       //.TolistAsync(); //Retrieve all data

            var sectionWithSameGrade = await context.Sections
                                                    .Include(s => s.Grade)
                                                    .Where(s => s.GradesId == GradeSection.GradesId && s.Track == GradeSection.Track && s.TVLProgram == GradeSection.TVLProgram && s.Id != id)
                                                    //.Select(s => s.SectionName)
                                                    .ToListAsync();

            var model = new ManageSectionSubjectViewModel
            {
                Section = GradeSection,
                otherSectionWithSameGrade = sectionWithSameGrade,
                assignedList = assignedSubjectList,

                DataCount = assignedSubjectList.Count
            };

            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
                                                   //id of section where to copy from(sang section galing)
                                                                           //Which sections to copy To(kaya nakaList)
        public async Task<IActionResult> CopySubjects(int sourceSectionId, List<int> targetSectionIds)
        {
            //Gets all subjects under the source section
            var sourceSubjects = await context.SectionSubjects
                                              .Where(ss => ss.SectionId == sourceSectionId)
                                              .ToListAsync();
            int copiedCount = 0;

            //Outer loop: Copy subject to each target sections
            foreach(var targetSectionId in targetSectionIds) //<- Where to Copy
            {
                //check if yung sections is existing
                var targetSection = await context.Sections.FindAsync(targetSectionId);
                //Inner Loop: Copying each subject
                foreach(var sourceSubject in sourceSubjects) //<- What to copy
                {
                    //Check if the subject is already exist on the target section
                    var exists = await context.SectionSubjects
                                              .AnyAsync(ss => ss.SectionId == targetSectionId && ss.SubjectId == sourceSubject.SubjectId);
                    //If subject does not already exists, it adds it
                    if (!exists)
                    {
                        context.SectionSubjects.Add(new SectionSubject
                        {
                            SectionId = targetSectionId,
                            SubjectId = sourceSubject.SubjectId,
                            CreatedAt = DateTime.Now,
                        });
                        copiedCount++;
                    }
                }
            }

            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Copy",
                entityName: "Subject",
                entityId: sourceSectionId.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} copied {copiedCount} subject(s) from section {sourceSectionId} to {targetSectionIds.Count} target section(s)",
                username: userInfo.username
            );

            return Json(new { success = true, message = $"Successfully copied {copiedCount} subjects!" });
        }

        //[HttpGet]
        //public async Task<IActionResult> AssignSubject(string searchString)
        //{
        //    var subjectList = await context.Subjects
        //        .OrderBy(s => s.SubjectDescription)
        //        .Take(10)
        //        .ToListAsync();

        //    var model = new ManageSectionSubjectViewModel()
        //    {
        //        //SubjectDescription = subjectList.SubjectDesription //Ginagamit kapag single string value lang
        //        AvailableSubject = subjectList //Uses for list

        //    };

        //    if (!string.IsNullOrEmpty(searchString))
        //    {
        //        subjectList = await context.Subjects
        //                     .Where(s => s.SubjectDescription.Contains(searchString));

        //    }
        //    return PartialView("_AssignSubjectPartial", model);
        //}

        [HttpGet]
        public async Task<IActionResult> AssignSubject(int sectionId, int subjectId, string searchString, string category)
        {
            var assignedSubject = await context.SectionSubjects
                                               .Where(s => s.SectionId == sectionId) //This excluded the assigned subject by an specific section only
                                               .Select(s => s.SubjectId)
                                               .ToListAsync();

            //var availableSubject = await context.Subjects
            //                                    .Where(s => s.Category == "JHS" && !assignedSubject.Contains(s.Id))
            //                                    .ToListAsync();

            //Used for Dynamic filtereing. Hindi agad gagana yung query unless tinawag na yung ToList
            IQueryable<Subject> subjectQuery = context.Subjects
                                                      .Where(s => !assignedSubject.Contains(s.Id))
                                                      .OrderBy(s => s.SubjectDescription);

            if (!string.IsNullOrEmpty(searchString))
            {
                subjectQuery = subjectQuery.Where(s => s.SubjectDescription.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(category))
            {
                subjectQuery = subjectQuery.Where(c => c.Category == category);
            }

            var subjectList = await subjectQuery.ToListAsync();


            var model = new AssignSubjectViewModel()
            {
                SectionId = sectionId,
                AvailableSubject = subjectList,
            };

            ViewData["searchString"] = searchString;
            ViewData["category"] = category;

            return PartialView("_AssignSubjectPartial", model);

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSubject(int sectionId, List<int> SelectedSubjects)
        {
            //var section = await context.Sections.FindAsync(sectionId);
            //bago
            var section = await context.Sections
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == sectionId);

            if ( section == null)
            {
                return Json(new { success = false, errors = "Section Id Not Found!" });
            }

            if(SelectedSubjects == null || !SelectedSubjects.Any())
            {
                return Json(new { success = false, errors = "Please select atleast 1 subject" });
            }
            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }
            //bagp
            var subjectsToAssign = await context.Subjects
                .Where(s => SelectedSubjects.Contains(s.Id))
                .Select(s => new { s.Id, s.SubjectDescription })
                .ToListAsync();
            //bago
            var sectionInfo = $"Grade {section.Grade.GradeLevel} - {section.SectionName} {section.Track} {section.TVLProgram}";
            var addedSubjects = new List<string>();

            foreach (var subjectId in SelectedSubjects)
            {
                //var subject = await context.Subjects.FindAsync(subjectId);
                var alreadyAssigned = await context.SectionSubjects
                    .AnyAsync(ss => ss.SectionId == sectionId && ss.SubjectId == subjectId);

                if (alreadyAssigned != null)
                {
                    var assigned = new SectionSubject()
                    {
                        SectionId = sectionId,
                        SubjectId = subjectId,
                        CreatedAt = DateTime.Now

                    };
                    await context.SectionSubjects.AddAsync(assigned);

                    // Get the subject name// bago
                    var subjectName = subjectsToAssign.FirstOrDefault(s => s.Id == subjectId)?.SubjectDescription;
                    if (subjectName != null)
                    {
                        addedSubjects.Add(subjectName);
                    }
                }
            }

            // Create detailed log message
            // bago
            var subjectList = addedSubjects.Any()
                ? string.Join(", ", addedSubjects)
                : "no new subjects (already assigned)";

            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Assign Subject",
                entityName: "SectionSubject",
                entityId: sectionId.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} assigned {addedSubjects.Count} subject(s) [{subjectList}] to {sectionInfo}",
                username: userInfo.username
            );

            return Json(new { success = true, message = "Subject Assigned Successfully!" });
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveAssignedSubject(int id)
        {
            try
            {
                var subject = await context.SectionSubjects
                    .Include(ss => ss.Section)
                        .ThenInclude(s => s.Grade)
                    .Include(ss => ss.Subject)
                    .FirstOrDefaultAsync(ss => ss.Id == id);

                if (subject == null)
                {
                    return Json(new { success = false, error = "Id not Found!" });
                }

                var sectionInf0 = $"Grade {subject.Section.Grade.GradeLevel} - {subject.Section.SectionName} {subject.Section.Track} {subject.Section.TVLProgram}";
                var subjectName = subject.Subject.SubjectDescription;

                context.SectionSubjects.Remove(subject);
                await context.SaveChangesAsync();

                var userInfo = await GetCurrentUserInfo();

                await logService.LogActivity(
                    actionType: "Delete",
                    entityName: "SectionSubject",
                    entityId: id.ToString(),
                    userId: userInfo.userId,
                    schoolId: userInfo.schoolId,
                    details: $"User {userInfo.username} unassigned subject '{subjectName}' from {sectionInf0}",
                    username: userInfo.username
                );

                return Json(new { success = true, message = "Subject Removed!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing subject: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while removing the subject" });
            }

            //var subject = await context.SectionSubjects.FindAsync(id);

            //if (subject == null)
            //{
            //    return Json(new { success = true, error = "Id not Found!" });
            //}


        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AssignSubject()
        //{
        //    return Json(new { success = true, message = "Subject assigned successfully!" });
        //}
        //public async Task<IActionResult> GradeAndSectionList()
        //{
        //    var GradesSection = await context.AcademicClasses
        //        .OrderBy(s => s.GradeLevel)
        //        .ThenBy(s => s.SectionName)
        //        .ToListAsync();

        //    //var groupdSections = await context.AcademicClasses
        //    //    .GroupBy(g => g.GradeLevel) //group the class by gradeLevel
        //    //    .Select(group => new GradeAndSectionViewModel // transform each group into object
        //    //    {
        //    //        GradeLevel = group.Key, // the gradelevel (grouping key)
        //    //        SectionName = string.Join(", ",  //Join Section into one string and seperate them using comma
        //    //            group.Select(s => s.SectionName))//Get all section names in the group
        //    //    })
        //    //    .OrderBy(g => g.GradeLevel) //sort by grade level. Pag kakasunod 
        //    //    .ToListAsync(); //Execure query and return result into list


        //    return View(GradesSection);
        //}

        public IActionResult AddGrade()
        {
            return PartialView("_AddGradePartial");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGrade(GradeViewModel model)
        {

            var gradeExisted = await context.Grades
                .IgnoreQueryFilters()
                .AnyAsync(g => g.GradeLevel == model.GradeLevel && g.IsDeleted == true);

            if (gradeExisted)
            {
                ModelState.AddModelError("GradeLevel", "Grade Level is already Existed. Check restore table if grade is softly deleted!");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            var grade = new Grade()
            {
                GradeLevel = model.GradeLevel,
                Category = model.Category,
                CreatedAt = DateTime.Now
            };

            await context.Grades.AddAsync(grade);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Add",
                entityName: "Grade",
                entityId: grade.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} added a new subject",
                username: userInfo.username
            );

            return Json(new { success = true, message = "Grade Level Added Successfully!" });
        }

        
        [HttpGet]
        public async Task<IActionResult> EditGrade(int id)
        {
            var grade = await context.Grades.FindAsync(id);

            if (grade == null)
            {
                return Json(new { success = false, error = "Not Found" });// always gamitin ang json lalo na kapag ajax/modal. Standard para sa ajax ang json
            }

            var model = new GradeViewModel()
            {
                GradeLevel = grade.GradeLevel,
                Category = grade.Category

            };


            return PartialView("_EditGradePartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGrade(int id, GradeViewModel model)
        {
            var editGrade = await context.Grades.FindAsync(id);

            if(editGrade == null)
            {
                return Json(new { success = false, message = "Grade not found" });
            }

            bool GradeLevelExisted = await context.Grades.AnyAsync(g => g.GradeLevel == model.GradeLevel && g.Id != id);

            if (GradeLevelExisted)
            {
                ModelState.AddModelError("GradeLevel", "Grade Level already existed!");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            editGrade.GradeLevel = model.GradeLevel;
            editGrade.Category = model.Category;

            context.Grades.Update(editGrade);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Edit",
                entityName: "Grade",
                entityId: editGrade.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} edited Grade {editGrade.GradeLevel}",
                username: userInfo.username
            );


            return Json(new { success = true, message = "Grade Successfully Edited!" });

        }

        [HttpDelete]
        public async Task<IActionResult> DeleteGrade(int id)
        {
            var grade = await context.Grades.FindAsync(id);
            //var grade = await context.Grades
            //    .Include(s => s.Sections)
            //        .ThenInclude(ss => ss.SectionSubjects)
            //            .ThenInclude(ta => ta.TeacherAssignments)
            //    .Include(s => s.Sections)
            //        .ThenInclude(sa => sa.StudentAssignments)
            //    .Include(s => s.Sections)
            //        .ThenInclude(sa => sa.SecretaryAssignments)   
            //    .FirstOrDefaultAsync(g => g.Id == id);

            if (grade == null)
            {
                return Json(new { success = false, error = "Grade Level does not found" });
            }

            var hasSection = await context.Sections.AnyAsync(s => s.GradesId == id);

            if (hasSection)
            {
                return Json(new { success = false, message = "Cannot delete Grade when contain sections" });
            }

            //context.Grades.Remove(grade);

            var time = DateTime.UtcNow;

            grade.IsDeleted = true;
            grade.DeletedAt = time;

            //foreach(var section in grade.Sections)
            //{
            //    section.IsDeleted = true;
            //    section.DeletedAt = time;

            //    foreach(var ss in section.SectionSubjects)
            //    {
            //        ss.IsDeleted = true;
            //        ss.DeletedAt = time;

            //        foreach (var ta in ss.TeacherAssignments)
            //        {
            //            ta.IsDeleted = true;
            //            ta.DeletedAt = time;
            //        }

            //    }

            //    foreach (var ssa in section.StudentAssignments)
            //    {
            //        ssa.IsDeleted = true;
            //        ssa.DeletedAt = time;
            //    }

            //    foreach(var sa in section.SecretaryAssignments)
            //    {
            //        sa.IsDeleted = true;
            //        sa.DeletedAt = time;
            //    }

            //}
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Deleted",
                entityName: "Grade",
                entityId: grade.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Admin {userInfo.username} deleted grade {grade.GradeLevel}",
                username: userInfo.username
            );


            return Json(new { success = true, message = "Grade Successfully Deleted!" });

        }

        [HttpGet]
        public async Task<IActionResult> RestoreGrade()
        {

            var IsDeletedGrade = await context.Grades
                .IgnoreQueryFilters()
                .Where(g => g.IsDeleted == true)
                .OrderBy(g => g.GradeLevel)
                .ToListAsync();

            return PartialView("_RestoreGradePartial", IsDeletedGrade);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreGrade(int gradeId)
        {
            var deletedGrade = await context.Grades
                .IgnoreQueryFilters()
                .Include(g => g.Sections)
                    .ThenInclude(ss => ss.SectionSubjects)
                        .ThenInclude(ta => ta.TeacherAssignments)
                .Include(s => s.Sections)
                    .ThenInclude(sa => sa.StudentAssignments)
                .Include(s => s.Sections)
                    .ThenInclude(sa => sa.SecretaryAssignments)
                .FirstOrDefaultAsync(g => g.Id == gradeId);

            if(deletedGrade == null)
            {
                return Json(new { success = false, message = "Id could not found" });
            }

            deletedGrade.IsDeleted = false;
            deletedGrade.DeletedAt = null;

            foreach(var section in deletedGrade.Sections)
            {
                section.IsDeleted = false;
                section.DeletedAt = null;

                foreach(var ss in section.SectionSubjects)
                {
                    ss.IsDeleted = false;
                    ss.DeletedAt = null;

                    foreach(var ta in ss.TeacherAssignments)
                    {
                        ta.IsDeleted = false;
                        ta.DeletedAt = null;
                    }
                }

                foreach(var sa in section.StudentAssignments)
                {
                    sa.IsDeleted = false;
                    sa.DeletedAt = null;
                }

                foreach(var sta in section.SecretaryAssignments)
                {
                    sta.IsDeleted = false;
                    sta.DeletedAt = null;
                }
            }

            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Restore",
                entityName: "Grade",
                entityId: deletedGrade.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Admin {userInfo.username} restore grade {deletedGrade.GradeLevel}",
                username: userInfo.username
            );

            var remainingdeletedGrade = await context.Grades
                .IgnoreQueryFilters()
                .Where(g => g.IsDeleted == true)
                .OrderBy(g => g.GradeLevel)
                .ToListAsync();

            return PartialView("_RestoreGradePartial", remainingdeletedGrade);

            //return Json(new { success = true, message = "Deleted grade restore successfully!" });
        }
        public async Task<IActionResult> SectionList()
        {
            var sectionList = await context.Sections
                .Include(g => g.Grade) // dahil sa Nav.Property/Lazy loading na nakadeclare sa Section.cs kaya gumana ang .Include
                .OrderBy(s => s.Grade.GradeLevel)
                .ToListAsync();

            return View(sectionList);
        }

        [HttpGet]
        public async Task<IActionResult> AddSection()
        {
            // With 'new' (pulls only what we need):
            var availableGrades = await context.Grades
                .Select(g => new { g.Id, g.GradeLevel }) //Only gets id and GradeLevel
                .OrderBy(g => g.GradeLevel)
                .ToListAsync();

                
            var model = new SectionViewModel
            {
                //this create a dropdown
                AvailableGrades = availableGrades.Select(g => new SelectListItem //SelectListItem design for creating dropdown list
                {
                    Value = g.Id.ToString(), // what gets sent to the server when selected
                    Text = $"Grade {g.GradeLevel}" // eto yung makikita ng user
                }).ToList()

            };

            return PartialView("_AddSectionPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSection(CreateSectionViewModel model)
        {    
            var sectionNames = model.SectionName
                .Split(',') //Divide string into an array
                .Select(s => s.Trim()) //remove extra spaces
                .Where(s => !string.IsNullOrEmpty(s))//remove empty entries
                .Distinct() //remove duplicates
                .ToList();

            if (!sectionNames.Any())
            {
                ModelState.AddModelError("SectionName", "Please provide atleast 1 section name");
            }

            //Check if Section is already Existed on a specific Grade level sa iinput na bagong section
            var sectionExisted = await context.Sections
                .Where(s => s.GradesId == model.GradesId && s.Track == model.Track
                        && sectionNames.Contains(s.SectionName))
                .Select(s => s.SectionName)
                .ToListAsync();

            if (sectionExisted.Any())
            {
                ModelState.AddModelError("SectionName", "Section Name is Already Existed");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                   kvp => kvp.Key,
                   kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            //var Section = new Section()
            //{
            //    GradesId = model.GradesId,
            //    SectionName = model.SectionName,
            //    Track = model.Track,
            //    CreatedAt = DateTime.Now
            //};

            var grade = await context.Grades.FindAsync(model.GradesId);
            
            //pag ganito ibig sabihin may data na multiple ang iinsert na data   
            var Sections = sectionNames.Select(name => new Section
            {
                GradesId = model.GradesId,
                SectionName = name,
                Track = model.Track,
                TVLProgram = model.TVLProgram,
                CreatedAt = DateTime.Now
            }).ToList();

            // Build section list for logging
            var sectionList = string.Join(", ", sectionNames);
            var gradeInfo = grade != null ? $"Grade {grade.GradeLevel}" : "";
            var trackInfo = !string.IsNullOrEmpty(model.Track) ? $" - {model.Track}" : "";
            var tvlInfo = !string.IsNullOrEmpty(model.TVLProgram) ? $" ({model.TVLProgram})" : "";

            await context.Sections.AddRangeAsync(Sections);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Add",
                entityName: "Section",
                entityId: Sections.First().Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} added {sectionNames.Count()} sections(s) [{sectionList}] to {gradeInfo} {trackInfo} {tvlInfo}",
                username: userInfo.username
            );


            return Json(new { success = true, message = "Section Added Succesfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> EditSection(int id)
        {
            var section = await context.Sections.FindAsync(id);

            if(section == null)
            {
                return Json(new { success = false, message = "Section not found" });
            }

            // With 'new' (pulls only what we need):
            var availableGrades = await context.Grades
                .Select(g => new { g.Id, g.GradeLevel, g.Category }) //Only gets id and GradeLevel
                .ToListAsync();

            var model = new EditSectionViewModel()
            {
                AvailableGrades = availableGrades.Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(),
                    Text = $"Grade {g.GradeLevel}"
                }).ToList(),

                GradesId = section.GradesId,
                SectionName = section.SectionName,
                Track = section.Track,
                TVLProgram = section.TVLProgram,

            };


            return PartialView("_EditSectionPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSection(int id, EditSectionViewModel model)
        {
            var editSection = await context.Sections.FindAsync(id);

            if (editSection == null)
            {
                return Json(new { success = false, message = "Section not found" });
            }

            //Gamitin ang FirstOrDefaultAsync If you need to do something with the existing section data. Pag kailangan mo ng actual data
            //var sectionExisted = await context.Sections
            //    .FirstOrDefaultAsync(s => s.GradesId == model.GradesId
            //            && s.SectionName == model.SectionName && s.Id != id);

            //Gamitin ang Any kapag more on validation checking lang
            var sectionExisted = await context.Sections
                .AnyAsync(s => s.GradesId == model.GradesId && s.Track == model.Track
                        && s.SectionName == model.SectionName && s.Id != id);        

            if (sectionExisted)
            {
                ModelState.AddModelError("SectionName", "Section Name is already Existed");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            editSection.GradesId = model.GradesId;
            editSection.SectionName = model.SectionName;
            editSection.Track = model.Track;
            editSection.TVLProgram = model.TVLProgram;

            var grades = await context.Grades.FindAsync(model.GradesId);
            context.Sections.Update(editSection);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Edit",
                entityName: "Section",
                entityId: editSection.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} edited section {model.SectionName} of Grade {grades.GradeLevel}",
                username: userInfo.username
            );


            return Json( new {success = true, message = "Section Edited Succcessfully!"});
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSection(int id)
        {
            //var Section = await context.Sections.FindAsync(id);
            var Section = await context.Sections
                    .Include(ss => ss.SectionSubjects)
                        .ThenInclude(ta => ta.TeacherAssignments)
                    .Include(sa => sa.StudentAssignments)
                    .Include(ssa => ssa.SecretaryAssignments)
                    .FirstOrDefaultAsync(s => s.Id == id);

            var grade = await context.Grades.FindAsync(Section.GradesId);

            var trackInfo = !string.IsNullOrEmpty(Section.Track) ? $" - {Section.Track}" : "";
            var tvlInfo = !string.IsNullOrEmpty(Section.TVLProgram) ? $" ({Section.TVLProgram})" : "";

            if (Section == null)
            {
                return Json(new { success = false, error = "Section does not exist!" });
            }

            //var hasStudentAssigned = await context.StudentSectionAssignments.AnyAsync(ssa => ssa.SectionId == id);
            //var hasSubjectAssigned = await context.SectionSubjects.AnyAsync(ss => ss.SectionId == id);
            //if (hasSubjectAssigned)
            //{
            //    return Json(new { success = false, message = "Cannot delete section if class already have a subject" });

            //}

            //context.Sections.Remove(Section);

            var time = DateTime.UtcNow;

            Section.IsDeleted = true;
            Section.DeletedAt = time;

            foreach(var sectionSubject in Section.SectionSubjects)
            {
                sectionSubject.IsDeleted = true;
                sectionSubject.DeletedAt = time;

                foreach (var ta in sectionSubject.TeacherAssignments)
                {
                    ta.IsDeleted = true;
                    ta.DeletedAt = time;
                }
                
            }
            foreach (var sa in Section.StudentAssignments)
            {
                sa.IsDeleted = true;
                sa.DeletedAt = time;
            }

            foreach (var sta in Section.SecretaryAssignments)
            {
                sta.IsDeleted = true;
                sta.DeletedAt = time;
            }

            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Deleted",
                entityName: "Section",
                entityId: Section.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Admin {userInfo.username} deleted section '{Section.SectionName}' of Grade {grade.GradeLevel} {trackInfo} {tvlInfo}",
                username: userInfo.username
            );

            return Json(new { success = true, message = "Section Successfully deleted!" });
        }

        [HttpGet]
        public async Task<IActionResult> RestoreSection()
        {
            var isDeletedSection = await context.Sections
                .IgnoreQueryFilters()
                .Include(g => g.Grade)
                .Where(s => s.IsDeleted == true)
                .OrderBy(s => s.Grade.GradeLevel)
                .ToListAsync();

            return PartialView("_RestoreSectionPartial", isDeletedSection);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreSection(int sectionId)
        {
            var deletedSection = await context.Sections
                    .IgnoreQueryFilters()
                    .Include(ss => ss.SectionSubjects)
                        .ThenInclude(ta => ta.TeacherAssignments)
                    .Include(sa => sa.StudentAssignments)
                    .Include(ssa => ssa.SecretaryAssignments)
                    .FirstOrDefaultAsync(s => s.Id == sectionId);

            if(deletedSection == null)
            {
                return Json(new { success = false, message = "Id could not found" });
            }

            deletedSection.IsDeleted = false;
            deletedSection.DeletedAt = null;

            foreach (var sectionSubject in deletedSection.SectionSubjects)
            {
                sectionSubject.IsDeleted = false;
                sectionSubject.DeletedAt = null;

                foreach (var ta in sectionSubject.TeacherAssignments)
                {
                    ta.IsDeleted = false;
                    ta.DeletedAt = null;
                }

            }
            foreach (var sa in deletedSection.StudentAssignments)
            {
                sa.IsDeleted = false;
                sa.DeletedAt = null;
            }

            foreach (var sta in deletedSection.SecretaryAssignments)
            {
                sta.IsDeleted = false;
                sta.DeletedAt = null;
            }

            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Restore",
                entityName: "Section",
                entityId: deletedSection.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Admin {userInfo.username} restore section {deletedSection.SectionName}",
                username: userInfo.username
            );

            var remainingDeletedSection = await context.Sections
                .IgnoreQueryFilters()
                .Include(g => g.Grade)
                .Where(s => s.IsDeleted == true)
                .OrderBy(s => s.Grade.GradeLevel)
                .ToListAsync();

            return PartialView("_RestoreSectionPartial", remainingDeletedSection);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AddGradeAndSection(GradeAndSectionViewModel model)
        //{
        //    //bool gradeLevel = await context.AcademicClasses.AnyAsync(g => g.GradeLevel == model.GradeLevel);
        //    //if (gradeLevel)
        //    //{
        //    //    ModelState.AddModelError("GradeLevel", "Grade Level is already existed!");
        //    //}

        //    //divides section name input if the section has two or more entries
        //    //split section names   
        //    var sectionNames = model.SectionName
        //        .Split(',')//divides a string into an array
        //        .Select(s => s.Trim()) // remove extra spaces
        //        .Where(s => !string.IsNullOrEmpty(s))//remove empty entries
        //        .Distinct()//remove duplicates
        //        .ToList();

        //    if (!sectionNames.Any())
        //    {
        //        ModelState.AddModelError("SectionName", "Please provide atleast one section names");
        //        //return View(model);
        //    }

        //    //Check if Section is already Existed on a specific Grade level
        //    //LINQ Query Syntax
        //    //var sectionExisted = from s in context.AcademicClasses
        //    //                     where s.GradeLevel == model.GradeLevel && sectionNames.Contains(s.SectionName)
        //    //                     select s.SectionName;

        //    //LINQ Method Syntax
        //    //var sectionExisted = await context.AcademicClasses
        //    //    .Where(s => s.GradeLevel == model.GradeLevel
        //    //            && sectionNames.Contains(s.SectionName))
        //    //    .Select(s => s.SectionName)
        //    //    .ToListAsync();


        //    //if (sectionExisted.Any())
        //    //{
        //    //    ModelState.AddModelError("SectionName", "Section Name is Already Existed!");
        //    //}

        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState.ToDictionary(
        //                                   kvp => kvp.Key,
        //                                   kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
        //                               );

        //        return Json(new { success = false, errors = errors });
        //    }

        //    //LINQ using loop //shortcut to na naka forloop
        //    //pa ganito ibig sabihin may data na multiple ang iinsert na data   
        //    //var GradeSection = sectionNames.Select(name => new GradeLevel
        //    //{
        //    //    GradeLevel = model.GradeLevel,
        //    //    SectionName = name, // yung name is represent mismo ng section names kase naka array na sya dahil by batch ang add. Kase kapag ang gamit is model.SectionName is string sya and isang variable lang
        //    //    CreatedAt = DateTime.Now
        //    //});

        //    //await context.AcademicClasses.AddRangeAsync(GradeSection); //AddRangeAsync ang ginamit kase by batch ang iadd, means mulitiple data
        //    //await context.SaveChangesAsync();

        //    //return Json(new { success = true, message = "Grade and Section Added!" });
        //}

        //[HttpGet]
        //public async Task<IActionResult> EditGradeAndSection(int id)
        //{
        //    var GradeSection = await context.AcademicClasses.FindAsync(id);

        //    if(GradeSection == null)
        //    {
        //        return Json(new { success = false, error = "Grade and Section does not exist!" });
        //    }

        //    var model = new EditGradeAndSectionViewModel()
        //    {
        //        GradeLevel = GradeSection.GradeLevel,
        //        SectionName = GradeSection.SectionName
        //    };

        //    return PartialView("_EditGradeAndSectionPartial", model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> EditGradeAndSection(int id, EditGradeAndSectionViewModel model)
        //{
        //    var GradeSection = await context.AcademicClasses.FindAsync(id);

        //    if(GradeSection == null)
        //    {
        //        return Json(new { success = false, error = "Grade and Section does not exist!" });
        //    }

        //    bool sectionExisted = await context.AcademicClasses
        //        .AnyAsync(s => s.GradeLevel == model.GradeLevel 
        //            && s.SectionName == model.SectionName 
        //            && s.Id != id);

        //    if (sectionExisted)
        //    {
        //        ModelState.AddModelError("SectionName", "Section Name is already used!");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState.ToDictionary(
        //                        kvp => kvp.Key,
        //                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
        //                    );
        //        return Json(new { success = false, errors = errors });
        //    }

        //    GradeSection.GradeLevel = model.GradeLevel;
        //    GradeSection.SectionName = model.SectionName;

        //    context.Update(GradeSection);
        //    await context.SaveChangesAsync();

        //    return Json(new { success = true, message = "Grade & Section Successfully Edited!" });
        //}

        //[HttpDelete]
        //public async Task<IActionResult> DeleteGradeAndSection(int id)
        //{
        //    var GradeAndSection = await context.AcademicClasses.FindAsync(id);

        //    if (GradeAndSection == null)
        //    {
        //        return Json(new { success = false, error = "Grade And section does not exist!" });
        //    }

        //    context.AcademicClasses.Remove(GradeAndSection);
        //    await context.SaveChangesAsync();

        //    return Json(new { success = true, message = "Grade and Section Successfully deleted!" });
        //}
        public async Task<IActionResult> TeacherList()//string TeacherRole
        {

            var teacher = await userManager.GetUsersInRoleAsync("Teacher");
            var list = teacher.OrderBy(t => t.LastName).ToList();

            //var teacherIds = teacher.OrderBy(t => t.LastName).Select(t => t.Id).ToList();



            return View(list);// return view dahil full page ang nirereload
            //return PartialView();// kapag maliit or more on modal ang rereload
        }

        public async Task<IActionResult> SecretaryList()
        {

            var secretary = await userManager.GetUsersInRoleAsync("Secretary");
            var secretaryIds = secretary.Select(s => s.Id).ToList();

            var secretariesAssignGradeSection = await context.Users
                .Where(u => secretaryIds.Contains(u.Id))
                .Include(sa => sa.SecretariesAssignments)
                    .ThenInclude(sn => sn.Section)
                        .ThenInclude(g => g.Grade)
                .OrderBy(s => s.LastName)
                .ToListAsync();


            return View(secretariesAssignGradeSection);
        }

        [HttpGet] //Entity → ViewModel(for display/edit)
        public async Task<IActionResult> EditTeacher(string id)
        {
            // Get Entity From database
            var teacher = await context.Users.FindAsync(id);
            //var teacher = userManager.FindByIdAsync(id);

            if (teacher == null)
            {   // Hindi pwede mag-RedirectToAction sa PartialView. Unless full page load, eh naka modal
                //return RedirectToAction("TeacherList", "Admin");
                return Json(new { success = false, error = "Not Found" });// always gamitin ang json lalo na kapag ajax/modal. Standard para sa ajax ang json
            }
            //Map From entity to view model
            var model = new EditTeacherViewModel()
            {
                //Email = teacher.Email, //from entity
                //UserName = teacher.Email,
                SchoolId = teacher.SchoolId,
                //EmployeeId = teacher.EmployeeId,
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
        public async Task<IActionResult> ViewTeacher(string id)
        {
            var teacher = await context.Users.FindAsync(id);

            if (teacher == null)
            {
                return RedirectToAction("TeacherList", "Admin");
            }

            var currentDefaultYear = await context.AcademicPeriods
                .FirstOrDefaultAsync(ap => ap.IsDefault == 1);
            ///<summary>
            /// TWO QUERIES
            /// </summary>
            //// Get the IDs of SectionSubjects that are assigned to this teacher
            //var teacherAssignment = await context.TeacherAssignments
            //                        .Where(t => t.TeacherId == id)
            //                        .ToListAsync();
            //var sectionSubjectIds = teacherAssignment.Select(ta => ta.SectionSubjectId).ToList();

            //// Fetch the complete SectionSubject details (with Subject, Section, Grade) 
            //// that are CURRENTLY ASSIGNED to this teacher
            //var sectionSubjectQuery = await context.SectionSubjects
            //                        .Include(ss => ss.Subject)
            //                        .Include(s => s.Section)
            //                            .ThenInclude(g => g.Grade)
            //                        .Where(ss => sectionSubjectIds.Contains(ss.Id))
            //                        .OrderBy(ss => ss.SectionId)
            //                        .ToListAsync();

            ///<summary>
            /// COMBINE QUERY FOR teacherAssignment and sectionSubjectQuery
            /// </summary>
            //Get all SectionSubjects assigned to this teacher with complete details
            var teacherAssignment = await context.TeacherAssignments
                                    .Include(ta => ta.SectionSubject)
                                        .ThenInclude(ss => ss.Subject)
                                    .Include(ta => ta.SectionSubject.Section)
                                        .ThenInclude(s => s.Grade)
                                    .Where(ta => ta.TeacherId == id && ta.AcademicPeriod == currentDefaultYear)
                                    .OrderBy(ta => ta.SectionSubject.SectionId)
                                    .ToListAsync();

            //Manual mapping
            var model = new ViewTeacherViewModel()
            {
                //Email = teacher.Email,
                //UserName = teacher.Email,
                SchoolId = teacher.SchoolId,
                //EmployeeId = teacher.EmployeeId,
                FirstName = teacher.FirstName,
                MiddleName = teacher.MiddleName,
                LastName = teacher.LastName,
                Sex = teacher.Sex,
                positionTitle = teacher.positionTitle,
                imageFilePath = teacher.imageFilePath,
                currentAcademicYear = currentDefaultYear.Year,
                currentPeriod = currentDefaultYear.GradingPeriod,

                teacherAssignments = teacherAssignment
            };

            ViewData["imageFileData"] = teacher.imageFileData;
            //ViewData["imageFilePath"] = teacher.imageFilePath;
            ViewData["CreatedAt"] = teacher.CreatedAt.ToString("MM/dd/yyyy");

            return PartialView("_ViewTeacherPartial", model);
        }

        

        [HttpGet]
        public async Task<IActionResult> AddSecretary()
        {
            var secretary = await userManager.GetUsersInRoleAsync("Secretary");

            var secretaryId = secretary.Select(s => s.Id)
                .ToList();

            var assginedClass = await context.SecretaryAssignments
                .Where(sa => secretaryId.Contains(sa.SecretaryId))
                .Select(sa => sa.SectionId)
                .ToListAsync();

            var availableGradeSection = await context.Sections
                                                     .Include(g => g.Grade)
                                                     .Where(s => !assginedClass.Contains(s.Id))
                                                     .OrderBy(ga => ga.Grade.GradeLevel)
                                                     .Select(ags => new { ags.Id, ags.Grade.GradeLevel, ags.SectionName, ags.Track, ags.TVLProgram })
                                                     .ToListAsync();

            var model = new SecretaryViewModel
            {
                AvailableGradeSection = availableGradeSection.Select(ags => new SelectListItem
                {
                    Value = ags.Id.ToString(),

                    Text = ags.TVLProgram == null //condition
                           ? $"Grade {ags.GradeLevel} - {ags.SectionName}, {ags.Track}" //if result is True
                           : $"Grade {ags.GradeLevel} - {ags.SectionName}, {ags.Track} - {ags.TVLProgram}" //Else False

                })
                //.Take(10)
                .ToList()
            };

            return PartialView("_AddSecretaryPartial", model);
        }

        public IActionResult AddTeacher()
        {
            return PartialView("_AddTeacherPartial");
        }

        [HttpPost] //ViewModel → Entity (for saving to database)
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher(TeacherViewModel model)
        {
            bool teacherFirstLastNameExist = await context.Users
                .IgnoreQueryFilters()
                .AnyAsync(t => t.FirstName == model.FirstName && t.MiddleName == model.MiddleName && t.LastName == model.LastName);

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

            //bool employeeIdExisted = await context.Users.AnyAsync(e => e.EmployeeId == model.EmployeeId);

            //if (employeeIdExisted)
            //{
            //    ModelState.AddModelError("EmployeeId", "Employee Id is already taken!");
            //}

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
                //To Capitalize every first letter of word when inserting data
                TextInfo textinfo = CultureInfo.CurrentCulture.TextInfo;

                string formattedFirstName = textinfo.ToTitleCase(model.FirstName.ToLower());
                string formattedMiddleName = textinfo.ToTitleCase(model.MiddleName?.ToLower() ?? "");
                string formattedLastName = textinfo.ToTitleCase(model.LastName.ToLower());

                //Map from viewmodel to entity
                AppUser teacher = new AppUser()
                {
                    //Email = model.Email,
                    SchoolId = model.SchoolId,
                    UserName = model.SchoolId.ToString(),
                    //EmployeeId = model.EmployeeId,
                    FirstName = formattedFirstName,
                    MiddleName = formattedMiddleName,
                    LastName = formattedLastName,
                    Sex = model.Sex,
                    positionTitle = model.positionTitle,
                    imageFileData = saveImageData,
                    imageFilePath = saveImagePath,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(teacher, model.Password); //.CreateAsync has a build in validation so if email existed it will return an error

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(teacher, "Teacher"); //Assign Teacher role when registered!

                    var userInfo = await GetCurrentUserInfo();

                    await logService.LogActivity(
                        actionType: "Add",
                        entityName: "User",
                        entityId: teacher.Id.ToString(),
                        userId: userInfo.userId,
                        schoolId: userInfo.schoolId,
                        details: $"User {userInfo.username} added new teacher : {teacher.FirstName} {teacher.MiddleName} {teacher.LastName}, Employee No.: {teacher.SchoolId}",
                        username: userInfo.username
                    );

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
        public async Task<IActionResult> EditTeacher(string id, EditTeacherViewModel model)//After Edit - Submit(ViewModel → Entity) :

        {
            //var editTeacher = await context.Users.FindAsync(id);
            var editTeacher = await userManager.FindByIdAsync(id.ToString()); //get entity from database

            if(editTeacher == null)
            {
                return Json(new { success = false, message = "Teacher not found" });
            }
            //check for email duplication
            //bool sameEmail = await context.Users.AnyAsync(e => e.Email == model.Email && e.Id != id);

            //if (sameEmail)
            //{
            //    ModelState.AddModelError("Email", "Email is already used!");
            //}

            //duplicate check excluding self
            //Dito gumamit ng s.Id != id para pag nag check ng id is hindi isasama yung current id sa pag hahanap
            //check for Schoold Id Duplication
            bool schoolIdExisted = await context.Users.AnyAsync(s => s.SchoolId == model.SchoolId && s.Id != id);

            if (schoolIdExisted)
            {
                ModelState.AddModelError("SchoolId", "School Id is already taken!");
            }

            //Check for Employee Id duplication
            //bool employeeNoExisted = await context.Users.AnyAsync(e => e.EmployeeId == model.EmployeeId && e.Id != id);
            //if (employeeNoExisted)
            //{
            //    ModelState.AddModelError("EmployeeId", "Employee Id is already taken!");
            //}

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

                //check muna sa database if may laman ba yung image ng user
                if (!string.IsNullOrEmpty(editTeacher.imageFilePath))
                {
                    //if may laman saka palang bubuuin ang filepath
                    string oldImageFullPath = environment.WebRootPath + "/ProfilePic/" + editTeacher.imageFilePath;
                    //tapos kapag may laman nga, dun palang mag delete
                    if (oldImageFullPath != null)
                    {
                        //then mag execute to!
                        System.IO.File.Delete(oldImageFullPath);
                    }

                }

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

            //To Capitalize every first letter of word when inserting data
            TextInfo textinfo = CultureInfo.CurrentCulture.TextInfo;

            string formattedFirstName = textinfo.ToTitleCase(model.FirstName.ToLower());
            string formattedMiddleName = textinfo.ToTitleCase(model.MiddleName?.ToLower() ?? "");
            string formattedLastName = textinfo.ToTitleCase(model.LastName.ToLower());

            //Map ViewModel -> Entity(update existing entity)
            //editTeacher.Email = model.Email; //From ViewModel To Entity
            editTeacher.SchoolId = model.SchoolId;
            editTeacher.UserName = model.SchoolId.ToString();
            //editTeacher.EmployeeId = model.EmployeeId;
            editTeacher.FirstName = formattedFirstName;
            editTeacher.MiddleName = formattedMiddleName;
            editTeacher.LastName = formattedLastName;
            editTeacher.Sex = model.Sex;
            editTeacher.positionTitle = model.positionTitle;

            var result = await userManager.UpdateAsync(editTeacher);

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Edit",
                entityName: "User",
                entityId: editTeacher.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} edited teacher {editTeacher.FirstName} {editTeacher.MiddleName} {editTeacher.LastName}, Employee No.: {editTeacher.SchoolId}",
                username: userInfo.username
            );

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
        public async Task<IActionResult> DeleteTeacher(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { success = false, message = "ID is required" });
                }
                var teacher = await context.Users.FindAsync(id);
                //var teacher = await context.Users
                //    .Include(u => u.TeachingAssignments)
                //    .FirstOrDefaultAsync(t => t.Id == id);

                if (teacher == null)
                {
                    //return RedirectToAction("TeacherList", "Admin");
                    logger.LogWarning("Teacher not found with Id : {TeacherId}", id);
                    return Json(new { success = false, error = "Teacer does not found" });
                }

                var isAssigned = await context.TeacherAssignments.AnyAsync(ia => ia.TeacherId == id);

                if (isAssigned)
                {
                    return Json(new { success = false, message = "Cannot delete teacher when already Assigned!" });
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

                //context.Users.Remove(teacher);

                var time = DateTime.UtcNow;

                teacher.IsDeleted = true;
                teacher.DeletedAt = time;

                //foreach(var teacherAssignments in teacher.TeachingAssignments)
                //{
                //    teacherAssignments.IsDeleted = true;
                //    teacherAssignments.DeletedAt = time;
                //}

                await context.SaveChangesAsync();

                var userInfo = await GetCurrentUserInfo();

                await logService.LogActivity(
                    actionType: "Delete",
                    entityName: "User",
                    entityId: teacher.Id.ToString(),
                    userId: userInfo.userId,
                    schoolId: userInfo.schoolId,
                    details: $"User {userInfo.username} deleted teacher {teacher.FirstName} {teacher.MiddleName} {teacher.LastName}, Employee No.: {teacher.SchoolId}",
                    username: userInfo.username
                );

                logger.LogInformation("Teacher {TeacherId} successfully sofly deleted by {username}",
                                        id, userInfo.username);

                //return RedirectToAction("TeacherList", "Admin");
                return Json(new { success = true, message = "Teacher has been Deleted successfully" }); //JSON store and transport data from server side to client side
            }
            catch(DbUpdateException ex)
            {
                logger.LogError(ex, "Database error deleting teacher {TeacherId}", id);
                return Json(new { success = false, message = "Database Error" }); //JSON store and transport data from server side to client side

            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Unexpected error restoring deleting {TeacherId}", id);
                return Json(new { success = false, message = "Something went wrong" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> AssignTeacher(string teacherId)
        {
            var currentAcademicPeriod = await GetCurrentAcademicPeriodId();

            //This excluded the assigned sectonsubject to a teacher by an specific section only
            var assignedToTeacher = await context.TeacherAssignments
                                    .Where(t => t.TeacherId == teacherId)
                                    .Select(ss => ss.SectionSubjectId)
                                    .ToListAsync();

            var assignedSubject = await context.TeacherAssignments
                                    .IgnoreQueryFilters()
                                    .Where(ss => ss.AcademicPeriodId == currentAcademicPeriod)
                                    .Select(ss => ss.SectionSubjectId)
                                    .Distinct() // Remove Duplicates
                                    .ToListAsync();

            var sectionSubjectQuery = await context.SectionSubjects
                                    .Include(ss => ss.Subject)
                                    .Include(s => s.Section)
                                        .ThenInclude(g => g.Grade)
                                    .Where(ss => !assignedSubject.Contains(ss.Id))
                                    .OrderBy(ss => ss.Section.Grade.GradeLevel)
                                    .ToListAsync();

            var model = new AssignTeacherViewModel()
            {
                TeacherId = teacherId,
                SectionSubjects = sectionSubjectQuery,
            };

            return PartialView("_AssignTeacherPartial", model);

            //OLD QUERY
            //var assignedToTeacher = await context.TeacherAssignments
            //                        .Where(t => t.TeacherId == teacherId)
            //                        .Select(ss => ss.SectionSubjectId)
            //                        .ToListAsync();

            //var sectionSubjectQuery = await context.SectionSubjects
            //                        .Include(ss => ss.Subject)
            //                        .Include(s => s.Section)
            //                            .ThenInclude(g => g.Grade)
            //                        .Where(ss => !assignedToTeacher.Contains(ss.Id))
            //                        .OrderBy(ss => ss.Section.Grade.GradeLevel)
            //                        .ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> AssignTeacher(string teacherId, int sectionSubjectId)
        {
            var teacher = await context.Users.FindAsync(teacherId);

            var currentAcademicPeriod = await GetCurrentAcademicPeriodId();

            if (teacher == null)
            {
                return Json(new { success = false, message = "Teacher Id Not Found!" });
            }

            //var defaultAcademic = await context.AcademicPeriods
            //      .FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            var defaultAcademic = await GetCurrentAcademicPeriodId();


            var assigned = new TeacherAssignment()
            {
                TeacherId = teacherId,
                SectionSubjectId = sectionSubjectId,
                AcademicPeriodId = defaultAcademic,
                CreatedAt = DateTime.Now,
            };

            await context.TeacherAssignments.AddAsync(assigned);
            await context.SaveChangesAsync();

            //Need natin to for activity log kase  afte ng .SaveChangesAsync, hindi pa loaded sa database yung SectionSubject Property
            //Kaya magkakaerror sa var gradeinfo, etc kase null pa si sectionsubject kaya need natin si assigned = await context.TeacherAssignments para iload yung data sa database
            //dahil sa object natin na, var assigned = new TeacherAssignment() is naka load lang yung id hind actual na object or data

            assigned = await context.TeacherAssignments
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Section)
                    .ThenInclude(s => s.Grade)
                .FirstOrDefaultAsync(ta => ta.Id == assigned.Id);

            var gradeInfo = $"Grade {assigned.SectionSubject.Section.Grade.GradeLevel}";
            var sectionInfo = $"{ assigned.SectionSubject.Section.SectionName }";
            var trackInfo = !string.IsNullOrEmpty(assigned.SectionSubject.Section.Track) ? $" - {assigned.SectionSubject.Section.Track}" : "";
            var tvlInfo = !string.IsNullOrEmpty(assigned.SectionSubject.Section.TVLProgram) ? $" ({assigned.SectionSubject.Section.TVLProgram})" : "";

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Assign Teacher",
                entityName: "TeacherAssignment",
                entityId: teacher.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} assigned to {gradeInfo} - {sectionInfo} {trackInfo} {tvlInfo}",
                username: userInfo.username
            );

            //This excluded the assigned sectonsubject to a teacher by an specific section only
            var assignedToTeacher = await context.TeacherAssignments
                                    .Where(t => t.TeacherId == teacherId)
                                    .Select(ss => ss.SectionSubjectId)
                                    .ToListAsync();

            var assignedSubject = await context.TeacherAssignments
                                    .IgnoreQueryFilters()
                                    .Where(ss => ss.AcademicPeriodId == currentAcademicPeriod)
                                    .Select(ss => ss.SectionSubjectId)
                                    .Distinct() // Remove Duplicates
                                    .ToListAsync();

            var sectionSubjectQuery = await context.SectionSubjects
                                    .Include(ss => ss.Subject)
                                    .Include(s => s.Section)
                                        .ThenInclude(g => g.Grade)
                                    .Where(ss => !assignedSubject.Contains(ss.Id))
                                    //.OrderBy(ss => ss.SectionId)
                                    .OrderBy(ss => ss.Section.Grade.GradeLevel)
                                    .ToListAsync();

            var model = new AssignTeacherViewModel()
            {
                TeacherId = teacherId,
                SectionSubjects = sectionSubjectQuery,
            };

            return PartialView("_AssignTeacherPartial", model);

        }

        [HttpDelete]
        public async Task<IActionResult> RemoveAssignedToTeacher(int id)
        {
            //var teacherAssigned = await context.TeacherAssignments.FindAsync(id);
            //Kunin muna yung existing na record na idedelete para sa activity log is marecord kung ano yun kaya may mga nakainclude
            var teacherAssigned = await context.TeacherAssignments
                .IgnoreQueryFilters()
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Section)
                        .ThenInclude(s => s.Grade)
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(s => s.Subject)
                .FirstOrDefaultAsync(ta => ta.Id == id);

            if (teacherAssigned == null)
            {
                return Json(new { success = false, error = "Id not Found!" });
            }

            var teacherId = teacherAssigned.TeacherId;

            var time = DateTime.UtcNow;

            teacherAssigned.IsDeleted = true;
            teacherAssigned.DeletedAt = time;


            //context.TeacherAssignments.Remove(teacherAssigned);
            await context.SaveChangesAsync();

            var teacher = await context.Users.FindAsync(teacherId);

            var gradeInfo = $"Grade {teacherAssigned.SectionSubject.Section.Grade.GradeLevel}";
            var sectionInfo = $"{teacherAssigned.SectionSubject.Section.SectionName}";
            var trackInfo = !string.IsNullOrEmpty(teacherAssigned.SectionSubject.Section.Track) ? $" - {teacherAssigned.SectionSubject.Section.Track}" : "";
            var tvlInfo = !string.IsNullOrEmpty(teacherAssigned.SectionSubject.Section.TVLProgram) ? $" ({teacherAssigned.SectionSubject.Section.TVLProgram})" : "";
            var subjectAssign = $"{teacherAssigned.SectionSubject.Subject.SubjectDescription}";

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Remove Assignment",
                entityName: "TeacherAssignment",
                entityId: teacher.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} remove assignment {gradeInfo} - {sectionInfo} {trackInfo} {tvlInfo}, Subject: {subjectAssign} for Teacher: {teacher.FirstName} {teacher.MiddleName} {teacher.LastName} Employee No.: {teacher.SchoolId}",
                username: userInfo.username
            );

            var remainingAssignments = await context.TeacherAssignments
                                       .Include(ta => ta.SectionSubject)
                                            .ThenInclude(ss => ss.Section)
                                                .ThenInclude(s => s.Grade)
                                       .Include(ta => ta.SectionSubject.Subject)
                                       .Where(ta => ta.TeacherId == teacherId)
                                       .ToListAsync();

            var model = new ViewTeacherViewModel()
            {
                //Email = teacher.Email,
                //UserName = teacher.Email,
                SchoolId = teacher.SchoolId,
                //EmployeeId = teacher.EmployeeId,
                FirstName = teacher.FirstName,
                MiddleName = teacher.MiddleName,
                LastName = teacher.LastName,
                Sex = teacher.Sex,
                positionTitle = teacher.positionTitle,
                imageFilePath = teacher.imageFilePath,

                teacherAssignments = remainingAssignments
            };

            ViewData["imageFileData"] = teacher.imageFileData;
            //ViewData["imageFilePath"] = teacher.imageFilePath;
            ViewData["CreatedAt"] = teacher.CreatedAt.ToString("MM/dd/yyyy");

            return PartialView("_ViewTeacherPartial", model);
        }

        [HttpGet]
        public async Task<IActionResult> RestoreTeacherAssignment()
        {
            var currentAcademicPeriod = await GetCurrentAcademicPeriodId();

            var currentPeriod = await context.AcademicPeriods
                                .Where(ap => ap.IsDefault == 1)
                                .FirstOrDefaultAsync();
            var year = currentPeriod.Year;
            var period = currentPeriod.GradingPeriod;

            
            ViewBag.Year = year;
            ViewBag.Period = period;

            var isDeletedTAssignments = await context.TeacherAssignments
                                .IgnoreQueryFilters()
                                .Include(ta => ta.Teacher)
                                .Include(ta => ta.SectionSubject)
                                    .ThenInclude(ss => ss.Subject)
                                .Include(ta => ta.SectionSubject)
                                    .ThenInclude(ss => ss.Section)
                                        .ThenInclude(s => s.Grade)
                                .Where(ta => ta.IsDeleted == true && ta.AcademicPeriodId == currentAcademicPeriod)
                                .OrderBy(ta => ta.SectionSubject.Section.Grade.GradeLevel)
                                .ToListAsync();

            return PartialView("_RestoreTeacherAssignmentPartial", isDeletedTAssignments);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreTeacherAssignment(int assignmentId)
        {
            var unAssignedTeacherAssignments = await context.TeacherAssignments
                .IgnoreQueryFilters()
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Section)
                        .ThenInclude(s => s.Grade)
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(s => s.Subject)
                .FirstOrDefaultAsync(ta => ta.Id == assignmentId);

            if (unAssignedTeacherAssignments == null)
            {
                return Json(new { success = false, message = "Id could not found" });
            }

            var currentAcademicPeriod = await GetCurrentAcademicPeriodId();
            var teacherId = unAssignedTeacherAssignments.TeacherId;

            unAssignedTeacherAssignments.IsDeleted = false;
            unAssignedTeacherAssignments.DeletedAt = null;

            await context.SaveChangesAsync();

            var teacher = await context.Users.FindAsync(teacherId);

            var gradeInfo = $"Grade {unAssignedTeacherAssignments.SectionSubject.Section.Grade.GradeLevel}";
            var sectionInfo = $"{unAssignedTeacherAssignments.SectionSubject.Section.SectionName}";
            var trackInfo = !string.IsNullOrEmpty(unAssignedTeacherAssignments.SectionSubject.Section.Track) ? $" - {unAssignedTeacherAssignments.SectionSubject.Section.Track}" : "";
            var tvlInfo = !string.IsNullOrEmpty(unAssignedTeacherAssignments.SectionSubject.Section.TVLProgram) ? $" ({unAssignedTeacherAssignments.SectionSubject.Section.TVLProgram})" : "";
            var subjectAssign = $"{unAssignedTeacherAssignments.SectionSubject.Subject.SubjectDescription}";

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Restore",
                entityName: "Section",
                entityId: unAssignedTeacherAssignments.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Admin {userInfo.username} restore Teacher Assignment {gradeInfo} - {sectionInfo} {trackInfo} {tvlInfo}, Subject: {subjectAssign} for Teacher: {teacher.FirstName} {teacher.MiddleName} {teacher.LastName} Employee No.: {teacher.SchoolId}",
                username: userInfo.username
            );

            var remainingAssignments = await context.TeacherAssignments
                                .IgnoreQueryFilters()
                                .Include(ta => ta.Teacher)
                                .Include(ta => ta.SectionSubject)
                                    .ThenInclude(ss => ss.Subject)
                                .Include(ta => ta.SectionSubject)
                                    .ThenInclude(ss => ss.Section)
                                        .ThenInclude(s => s.Grade)
                                .Where(ta => ta.IsDeleted == true && ta.AcademicPeriodId == currentAcademicPeriod)
                                .OrderBy(ta => ta.SectionSubject.Section.Grade.GradeLevel)
                                .ToListAsync();

            return PartialView("_RestoreTeacherAssignmentPartial", remainingAssignments);
        }

        [HttpGet]
        public async Task<IActionResult> RestoreDeletedTeacher()
        {
            //var deletedTeacher = await userManager.GetUsersInRoleAsync("Teacher");

            //var filteredTeacher = deletedTeacher
            //    .Where(dt => dt.IsDeleted == true)
            //    .ToList();

            var filteredTeacher = await context.Users
                .IgnoreQueryFilters()
                .Where(u => u.IsDeleted == true)
                .ToListAsync();

            return PartialView("_RestoreDeletedTeacher", filteredTeacher);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreDeletedTeacher(string teacherId)
        {
            ///FOR DEBUGGING
            //Debug.WriteLine($"=== RESTORE TEACHER DEBUG ===");
            //Debug.WriteLine($"Received teacherId: {teacherId}");
            //Debug.WriteLine($"IsNullOrEmpty: {string.IsNullOrEmpty(teacherId)}");

            //var role = await context.Roles.Where(r => r.Name == "Teacher").FirstOrDefaultAsync();

            //var userRoleId = await context.UserRoles.Where(ur => ur.RoleId == role.Id).FirstOrDefaultAsync();

            //var deletedTeacher = await context.Users
            //    .IgnoreQueryFilters()
            //    .Where(u => u.Id == userRoleId.UserId)
            //    .FirstOrDefaultAsync(u => u.Id == teacherId);

            try
            {
                if (string.IsNullOrEmpty(teacherId))
                {
                    return Json(new { success = false, message = "ID required" });
                }

                var deletedTeacher = await context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == teacherId && u.IsDeleted == true);
                if (deletedTeacher == null)
                {
                    ///NOTE: Logger always use for production
                    logger.LogWarning("Teacher not found with ID: {TeacherId}", teacherId);

                    ///NOTE: Use debug.writeline for quick debugging
                    //Debug.WriteLine($"=== TEACHER DEBUG ===");
                    //Debug.WriteLine($"Teacher not found with ID:{teacherId}.");
                    return Json(new { success = false, message = "Id could not find" });
                }

                deletedTeacher.IsDeleted = false;
                deletedTeacher.DeletedAt = null;

                await context.SaveChangesAsync();

                var userInfo = await GetCurrentUserInfo();

                await logService.LogActivity(
                    actionType: "Restore",
                    entityName: "Teacher",
                    entityId: deletedTeacher.Id.ToString(),
                    userId: userInfo.userId,
                    schoolId: userInfo.schoolId,
                    details: $"Admin {userInfo.username} restore Teacher {deletedTeacher.FirstName} {deletedTeacher.LastName}. LRN: {deletedTeacher.SchoolId}",
                    username: userInfo.username
                );

                logger.LogInformation("Teacher {TeacherId} restored successfully by {username}",
                                    teacherId, userInfo.username);

                var remainingDeletedTeacher = await context.Users
                    .IgnoreQueryFilters()
                    .Where(dt => dt.IsDeleted == true)
                    .ToListAsync();

                return PartialView("_RestoreDeletedTeacher", remainingDeletedTeacher);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error restoring teacher {TeacherId}", teacherId);
                return Json(new { success = false, message = "Database Error" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error restoring teacher {TeacherId}", teacherId);
                return Json(new { success = false, message = "Something went wrong" });
            }
            
        }
        public async Task<IActionResult> StudentList()
        {
            try
            {
                var Students = await context.Students
                                .Include(sa => sa.SectionAssignments)
                                    .ThenInclude(sn => sn.Section)
                                        .ThenInclude(g => g.Grade)
                                .OrderBy(s => s.LastName)
                                .AsNoTracking() // bagong add
                                .ToListAsync();

                return View(Students);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error getting students");
                return Json(new { success = false, message = "Error occured" });
            }
            
        }

        [HttpGet]
        public async Task<IActionResult> AddStudent()
        {
            var availableGradeSection = await context.Sections
                                                     .Include(g => g.Grade)
                                                     .OrderBy(ga => ga.Grade.GradeLevel)
                                                     .Select(ags => new { ags.Id, ags.Grade.GradeLevel, ags.SectionName, ags.Track, ags.TVLProgram })
                                                     .ToListAsync();
            var model = new StudentViewModel
            {
                AvailableGradeSection = availableGradeSection.Select(ags => new SelectListItem
                {
                    Value = ags.Id.ToString(),

                    Text = ags.TVLProgram == null //condition
                           ? $"Grade {ags.GradeLevel} - {ags.SectionName}, {ags.Track}" //if result is True
                           : $"Grade {ags.GradeLevel} - {ags.SectionName}, {ags.Track} - {ags.TVLProgram}" //Else False

                })

                //.Take(10)
                .ToList()
            };

            return PartialView("_AddStudentPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(StudentViewModel model)
        {
            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    bool studentFirstLastNameExist = await context.Students
                        .IgnoreQueryFilters()
                        .AnyAsync(t => t.FirstName == model.FirstName && t.MiddelName == model.MiddelName && t.LastName == model.LastName);

                    if (studentFirstLastNameExist)
                    {
                        ModelState.AddModelError("FirstName", "A Student with this Full name already exists");
                        ModelState.AddModelError("MiddelName", "");
                        ModelState.AddModelError("LastName", "");
                    }

                    bool LRNExisted = await context.Students.AnyAsync(s => s.LRN == model.LRN);

                    if (LRNExisted)
                    {
                        ModelState.AddModelError("LRN", "LRN is already taken!");
                    }

                    if (!ModelState.IsValid)
                    {
                        var overallErrors = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                        );

                        return Json(new { success = false, errors = overallErrors });
                    }

                    string? saveImagePath = null;
                    //saveImageData is yung actual data ng image, na mag sesave sa imageFileData row sa database
                    byte[]? saveImageData = null; //ginagamit ang byte para magsave ng files tulad ng images, pdf, etc. sa loob ng database

                    if (model.imageFile != null)
                    {
                        //Konektado ito Sa AppUser na object para talagang magsave

                        //create filename
                        string newFile = DateTime.Now.ToString("yyyyMMddHHmmssff");
                        newFile += Path.GetExtension(model.imageFile.FileName);
                        //create physical path for the image
                        string imageFullPath = environment.WebRootPath + "/ProfilePic/" + newFile;
                        //save the actual image to ProfilePic file na naka declare sa variable na imageFullPath
                        using (var stream = System.IO.File.Create(imageFullPath))
                        {
                            await model.imageFile.CopyToAsync(stream);
                        }
                        //eto yung part na pag kasave nung actual image na (ex. image.jpg) eh pupunta na sya database sa imageFilePath na row
                        saveImagePath = newFile;

                        //Itong buong code na ito gang baba, dito kukunin yung mismong data ng image para iconvert sa byte para i-save na sa database
                        //Kase diba sa viewmodel ko is IFormFile gamit ko dun, si ang code na ito is iconvert ang iFormFile to byte na iinput ng user
                        using (var inputStream = model.imageFile.OpenReadStream())//para basahin yung upload file

                        using (var memoryStream = new MemoryStream())// maging temporary kolektor o lalagyan ng data
                        {
                            await inputStream.CopyToAsync(memoryStream);// eto na yung may hawak ng raw data
                            saveImageData = memoryStream.ToArray();// ngayon after makollect ng mismong data na naprocess na, dun na icoconvert sa byte
                        }
                    }

                    //To Capitalize every first letter of word when inserting data
                    TextInfo textinfo = CultureInfo.CurrentCulture.TextInfo;

                    string formattedFirstName = textinfo.ToTitleCase(model.FirstName.ToLower());
                    string formattedMiddleName = textinfo.ToTitleCase(model.MiddelName?.ToLower() ?? "");
                    string formattedLastName = textinfo.ToTitleCase(model.LastName.ToLower());

                    var student = new Student
                    {
                        LRN = model.LRN,
                        FirstName = formattedFirstName,
                        MiddelName = formattedMiddleName,
                        LastName = formattedLastName,
                        Sex = model.Sex,
                        imageFileData = saveImageData,
                        imageFilePath = saveImagePath,
                        CreatedAt = DateTime.Now
                    };


                    context.Students.Add(student);
                    await context.SaveChangesAsync();

                    var userInfo = await GetCurrentUserInfo();

                    await logService.LogActivity(
                        actionType: "Add",
                        entityName: "Student",
                        entityId: student.Id.ToString(),
                        userId: userInfo.userId,
                        schoolId: userInfo.schoolId,
                        details: $"User {userInfo.username} added {model.FirstName} {model.MiddelName} {model.LastName}, new student",
                        username: userInfo.username
                    );

                    var defaultAcademic = await GetCurrentAcademicPeriodId();

                    var sectionAssignment = new StudentSectionAssignment
                    {
                        StudentId = student.Id,
                        SectionId = model.SectionId,
                        //AcademicPeriodId = defaultAcademic,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.StudentSectionAssignments.Add(sectionAssignment);
                    await context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return Json(new { success = true, message = "Student Added Successfully" });
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();

                    logger.LogError(ex, "Database error adding student");
                    return Json(new { success = false, message = "Database Error" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error adding student");
                    return Json(new { success = false, message = "Something went wrong" });
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditStudent(int id)
        {
            var student = await context.Students.FindAsync(id);
                
            if (student == null)
            {
                return Json(new { success = false, message = "Student does not exist" });
            }
            //Retrieve all available Sections
            var allSection = await context.Sections
                    .Include(g => g.Grade)
                .Select(ags => new { ags.Id, ags.Grade.GradeLevel, ags.SectionName, ags.Track, ags.TVLProgram })
                .ToListAsync();

            //Retrieve current student's assigned Grade and Section
            var studentsGradeSection = await context.StudentSectionAssignments
                .Where(si => si.StudentId == id)
                .Select(s => s.SectionId)
                .FirstOrDefaultAsync();
          

            var model = new EditStudentViewModel()
            {
                AvailableGradeSection = allSection.Select(gs => new SelectListItem
                {
                    Value = gs.Id.ToString(),
                    Text = $"Grade {gs.GradeLevel} {gs.SectionName} {gs.Track} {gs.TVLProgram}",
                }).ToList(),
                    
                SectionId = studentsGradeSection,
                LRN = student.LRN,
                FirstName = student.FirstName,
                MiddelName = student.MiddelName,
                LastName = student.LastName,
                Sex = student.Sex,
                imageFilePath = student.imageFilePath,
                CreatedAt = student.CreatedAt,

            };

            return PartialView("_EditStudentPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(int id, EditStudentViewModel model)
        {
            //var editStudent = await context.Students.FindAsync(id);
            var editStudent = await context.Students.
                            Include(s => s.SectionAssignments)
                            .FirstOrDefaultAsync(s => s.Id == id);

            if(editStudent == null)
            {
                return Json(new { success = false, message = "Student id does not found" });

            }

            var studentGradeSectionAssigned = await context.StudentSectionAssignments
                                            .IgnoreQueryFilters()
                                            .Where(ssa => ssa.StudentId == id)
                                            .FirstOrDefaultAsync();

            bool studentFirstLastNameExist = await context.Students.AnyAsync(t => t.FirstName == model.FirstName && t.MiddelName == model.MiddelName && t.LastName == model.LastName && t.Id != id);

            if (studentFirstLastNameExist)
            {
                ModelState.AddModelError("FirstName", "A Student with this Full name already exists");
                ModelState.AddModelError("MiddelName", "");
                ModelState.AddModelError("LastName", "");
            }

            bool LRNExisted = await context.Students.AnyAsync(s => s.LRN == model.LRN && s.Id != id);

            if (LRNExisted)
            {
                ModelState.AddModelError("LRN", "LRN is already taken!");
            }

            if (!ModelState.IsValid)
            {

                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );
                return Json(new { success = false, errors = errors });
            }

            string? saveImagePath = null;
            byte[]? saveImageData = null;

            if (model.imageFile != null)
            {
                string newFile = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                newFile += Path.GetExtension(model.imageFile.FileName);

                string imageFullPath = environment.WebRootPath + "/ProfilePic/" + newFile;

                using (var stream = System.IO.File.Create(imageFullPath))
                {
                    await model.imageFile.CopyToAsync(stream);
                }

                //check muna sa database if may laman ba yung image ng user
                if (!string.IsNullOrEmpty(editStudent.imageFilePath))
                {
                    //if may laman saka palang bubuuin ang filepath
                    string oldImageFullPath = environment.WebRootPath + "/ProfilePic/" + editStudent.imageFilePath;
                    //tapos kapag may laman nga, dun palang mag delete
                    if (oldImageFullPath != null)
                    {
                        //then mag execute to!
                        System.IO.File.Delete(oldImageFullPath);
                    }

                }

                saveImagePath = newFile;

                using (var inputStream = model.imageFile.OpenReadStream())
                using (var memoryStream = new MemoryStream())
                {
                    await inputStream.CopyToAsync(memoryStream);
                    saveImageData = memoryStream.ToArray();
                }
                //IMPORTANT: Assign to editTeacher
                editStudent.imageFilePath = saveImagePath;
                editStudent.imageFileData = saveImageData;
            }


                //To Capitalize every first letter of word when inserting data
                TextInfo textinfo = CultureInfo.CurrentCulture.TextInfo;

            string formattedFirstName = textinfo.ToTitleCase(model.FirstName.ToLower());
            string formattedMiddleName = textinfo.ToTitleCase(model.MiddelName?.ToLower() ?? "");
            string formattedLastName = textinfo.ToTitleCase(model.LastName.ToLower());

            editStudent.LRN = model.LRN; 
            editStudent.FirstName = formattedFirstName;
            editStudent.MiddelName = formattedMiddleName;
            editStudent.LastName = formattedLastName;
            editStudent.Sex = model.Sex;

            context.Students.Update(editStudent);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Edit",
                entityName: "Student",
                entityId: editStudent.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} editted student {editStudent.FirstName} {editStudent.MiddelName} {editStudent.LastName}. LRN: {editStudent.LRN}",
                username: userInfo.username
            );

            //var hasstudentAssignment = await context.StudentSectionAssignments
            //                        .Where(ssa => ssa.StudentId == id)
            //                        .FirstOrDefaultAsync();

            if (studentGradeSectionAssigned == null)
            {
                var newAssignment = new StudentSectionAssignment()
                {
                    StudentId = id,
                    SectionId = model.SectionId,
                    AcademicPeriodId = model.AcademicPeriodId,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                context.StudentSectionAssignments.Add(newAssignment);
            }
            else if (studentGradeSectionAssigned.IsDeleted == true)
            {
                studentGradeSectionAssigned.IsDeleted = false;
                studentGradeSectionAssigned.DeletedAt = null;

                studentGradeSectionAssigned.SectionId = model.SectionId;
            }
            else
            {
                //redundant no need to assign again
                // studentGradeSectionAssigned.StudentId is already equal to id (which is editStudent.Id)

                //studentGradeSectionAssigned.StudentId = editStudent.Id;
                studentGradeSectionAssigned.SectionId = model.SectionId;
            }

            //context.StudentSectionAssignments.Update(studentGradeSectionAssigned);
            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Student Successfully Edited!" });
        }

        [HttpGet]
        public async Task<IActionResult> ViewStudent(int id)
        {
            var students = await context.Students.FindAsync(id);

            if (students == null)
            {
                return Json(new { success = false, error = "Student Not Found!" });
            }

            //1. Nakuha na yung data sa database
            var studentsClass = await context.StudentSectionAssignments
                .Include(s => s.Section)
                    .ThenInclude(g => g.Grade)
                .Where(sc => sc.StudentId == id)
                .FirstOrDefaultAsync();

            var model = new EditStudentViewModel()
            {
                LRN = students.LRN,
                FirstName = students.FirstName,
                MiddelName = students.MiddelName,
                LastName = students.LastName,
                Sex = students.Sex,
                imageFilePath = students.imageFilePath,
                studentClass = studentsClass, //2. Ilagay sa viewmodel object para maaccess sa razor page 
            };
            return PartialView("_ViewStudentPartial", model); //3. yung model ang magiging view ex. @Model.studentClass.Id 

        }

        [HttpDelete]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            //var Student = await context.Students.FindAsync(id);

            var Student = await context.Students
                .Include(ssa => ssa.SectionAssignments)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (Student == null)
            {
                return Json(new { success = false, error = "Student does not exist!" });
            }

            //check kung may laman yung image yung user
            if (!string.IsNullOrEmpty(Student.imageFilePath))
            {
                //string ImagePath = environment.WebRootPath + "/ProfilePic/" + teacher.imageFilePath;
                //Path.Combine, static method within System.IO.Path
                string ImagePath = Path.Combine(environment.WebRootPath, "ProfilePic", Student.imageFilePath);// si Path.Combine is gumagamit ng correct directorty seprator para imbis na "/ProfilePic/ anggamitin is sya na mismo ang bahala kase minsan may mga dobleng slash, kaya pwedeng mag error!
                //check if existing  paba talaga sa ProfilePic yung file
                if (System.IO.File.Exists(ImagePath))
                {
                    System.IO.File.Delete(ImagePath);
                }
            }

            //context.Students.Remove(Student);

            var hasAttendance = await context.Attendances
                .AnyAsync(a => a.StudentSectionAssignmentId != null && a.StudentSectionAssignment.StudentId == id);

            var time = DateTime.UtcNow;

            if (hasAttendance)
            {
                Student.IsDeleted = true;
                Student.DeletedAt = time;

                foreach(var assignment in Student.SectionAssignments)
                {
                    assignment.IsDeleted = true;
                    assignment.DeletedAt = time;
                }

                await context.SaveChangesAsync();

                return Json(new { success = true, message = "Student deleted! Attendance data is archived for history data!" });
            }

            Student.IsDeleted = true;
            Student.DeletedAt = time;

            foreach (var assignment in Student.SectionAssignments)
            {
                assignment.IsDeleted = true;
                assignment.DeletedAt = time;
            }
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Delete",
                entityName: "Student",
                entityId: Student.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Admin{userInfo.username} deleted student {Student.FirstName} {Student.MiddelName} {Student.LastName}. LRN: {Student.LRN}",
                username: userInfo.username
            );

            return Json(new { success = true, message = "Student Successfully deleted!" });
        }

        [HttpGet]
        public async Task<IActionResult> RestoreDeletedStudent()
        {
            var deletedStudent = await context.Students
                .IgnoreQueryFilters()
                .Include(s => s.SectionAssignments)
                    .ThenInclude(sa => sa.Section)
                        .ThenInclude(sec => sec.Grade)
                .Where(s => s.IsDeleted == true)
                .ToListAsync();

            return PartialView("_RestoreDeletedStudent", deletedStudent);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreDeletedStudent(int studentId)
        {
            if (studentId == null)
            {
                return Json(new { success = false, message = "Teacher ID is required" });
            }

            var deletedStudent = await context.Students
                    .IgnoreQueryFilters()
                    .Include(ssa => ssa.SectionAssignments)
                        .ThenInclude(s => s.Section)
                            .ThenInclude(g => g.Grade)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

            if (deletedStudent == null)
            {
                return Json(new { success = false, message = "Id could not found" });
            }

            var hasAttendance = await context.Attendances
                .Where(a => a.StudentSectionAssignmentId != null && a.StudentSectionAssignment.StudentId == deletedStudent.Id)
                .FirstOrDefaultAsync();
                          
            deletedStudent.IsDeleted = false;
            deletedStudent.DeletedAt = null;

            foreach(var assignment in deletedStudent.SectionAssignments)
            {
                assignment.IsDeleted = false;
            }

            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Restore",
                entityName: "Student",
                entityId: deletedStudent.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Admin {userInfo.username} restore student {deletedStudent.FirstName} {deletedStudent.MiddelName} {deletedStudent.LastName}. LRN: {deletedStudent.LRN}",
                username: userInfo.username
            );

            var remainingDeletedStudent = await context.Students
                .IgnoreQueryFilters()
                .Include(sa => sa.SectionAssignments)
                .Where(s => s.IsDeleted == true)
                .ToListAsync();

            return PartialView("_RestoreDeletedStudent", remainingDeletedStudent);

        }

        //NO NEED NA LALO NA INDUSTRY STANDARD. GAMITIN NALANG IF MISMONG CLIENT ANG NAG SUGGEST!
        //[HttpDelete]
        //public async Task<IActionResult> PermanentDeleteStudent(int id)
        //{
        //    try
        //    {

        //        var studentId = await context.Students
        //                           .IgnoreQueryFilters()
        //                           .FirstOrDefaultAsync(u => u.Id == id);

        //        if (studentId == null)
        //        {
        //            logger.LogWarning("Student not found with the Id of {StudentId}", id);
        //            return Json(new { success = false, message = "Student id is Null" });
        //        }

        //        var studentSectionAssignment = await context.StudentSectionAssignments
        //                                    .IgnoreQueryFilters()
        //                                    .Where(ssa => ssa.StudentId == studentId.Id)
        //                                    .FirstOrDefaultAsync();

        //        if (!string.IsNullOrEmpty(studentId.imageFilePath))
        //        {
        //            string ImagePath = Path.Combine(environment.WebRootPath, "ProfilePic", studentId.imageFilePath);
        //            if (System.IO.File.Exists(ImagePath))
        //            {
        //                System.IO.File.Delete(ImagePath);
        //            }
        //        }

        //        //if (studentSectionAssignment != null)
        //        //{
        //        //    //context.Remove(studentSectionAssignment);
        //        //    studentSectionAssignment.IsDeleted = true;
        //        //    studentSectionAssignment.DeletedAt = DateTime.UtcNow;
        //        //    context.Update(studentSectionAssignment);
        //        //}

        //        context.Remove(studentId);
        //        await context.SaveChangesAsync();

        //        var userInfo = await GetCurrentUserInfo();

        //        await logService.LogActivity(
        //            actionType: "Delete",
        //            entityName: "User",
        //            entityId: studentId.Id.ToString(),
        //            userId: userInfo.userId,
        //            schoolId: userInfo.schoolId,
        //            details: $"Admin {userInfo.username} deleted student {studentId.FirstName} {studentId.MiddelName} {studentId.LastName}. LRN: {studentId.LRN}",
        //            username: userInfo.username
        //        );

        //        var remainingDeletedStudent = await context.Students
        //                            .IgnoreQueryFilters()
        //                            .Include(sa => sa.SectionAssignments)
        //                            .Where(s => s.IsDeleted == true)
        //                            .ToListAsync();

        //        logger.LogInformation("Student successfully deleted with the ID: {StudentId}", id);

        //        return PartialView("_RestoreDeletedStudent", remainingDeletedStudent);

        //    }
        //    //catch (DbUpdateException ex)
        //    //{
        //    //    logger.LogError(ex, "Database error restoring student : {StudentId}", id);
        //    //    return Json(new { success = false, message = "Database Error" });
        //    //}

        //    //Pag debug
        //    catch (DbUpdateException ex)
        //    {
        //        logger.LogError(ex, "Database error: {Inner}", ex.InnerException?.Message);
        //        return Json(new { success = false, message = ex.InnerException?.Message ?? "Database Error" });
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, "Unexpected error: {Message} | Inner: {Inner}",
        //            ex.Message,
        //            ex.InnerException?.Message);
        //        return Json(new { success = false, message = ex.Message + " | " + ex.InnerException?.Message });
        //    }
        //    //catch (Exception ex)
        //    //{
        //    //    logger.LogError(ex, "Unexpected error restoring student {StudentId}", id);
        //    //    return Json(new { success = false, message = "Something went wrong" });
        //    //}
        //}
        [HttpGet]
        public async Task<IActionResult> BulkPromoteStudent(string id)
        {

            //Query for student
            var studentIds = id.Split(',')
                .Select(x => int.Parse(x))
                .ToList();

            var students = await context.StudentSectionAssignments
                .Include(ssa => ssa.Student)
                .Include(ssa => ssa.Section)
                    .ThenInclude(s => s.Grade)
                .Where(ssa => ssa.StudentId.HasValue && studentIds.Contains(ssa.StudentId.Value))
                .ToListAsync();

            var currentAssignments = await context.StudentSectionAssignments
                .Include(ssa => ssa.Section)
                    .ThenInclude(s => s.Grade)
                .Where(ssa => ssa.StudentId.HasValue && studentIds.Contains(ssa.StudentId.Value))
                .FirstOrDefaultAsync();
            //.ToListAsync();

            var currentGrade = currentAssignments.Section.Grade.GradeLevel;
            var currentSection = currentAssignments.SectionId;

            var currentSections = students
                .Select(s => $"Grade {s.Section.Grade.GradeLevel} - {s.Section.SectionName} {s.Section.Track} {s.Section.TVLProgram}")
                .Distinct()
                .ToList();

            var alreadyAssinged = await context.StudentSectionAssignments
                .Where(ssa => ssa.StudentId.HasValue && studentIds.Contains(ssa.StudentId.Value))
                .Select(ssa => ssa.SectionId)
                .ToListAsync();

            //Display all classes
            var allClasses = await context.Sections
                .Include(s => s.Grade)
                .Where(g => g.Grade.GradeLevel > currentGrade)
                //.Where(s => s.Id != currentSection)
                .Where(s => !alreadyAssinged.Contains(s.Id))
                .OrderBy(s => s.Grade.GradeLevel)
                .Select(s => new { s.Id, s.Grade.GradeLevel, s.SectionName, s.Track, s.TVLProgram })
                .ToListAsync();

            var model = new BulkPromoteViewModel()
            {
                AvailableGradeSection = allClasses.Select(ac => new SelectListItem
                {
                    Value = ac.Id.ToString(),
                    Text = ac.TVLProgram == null
                            ? $"Grade {ac.GradeLevel} - {ac.SectionName}, {ac.Track}" //if result is True
                           : $"Grade {ac.GradeLevel} - {ac.SectionName}, {ac.Track} - {ac.TVLProgram}" //Else False
                }).ToList(),

                StudentIds = studentIds,
                Students = students,
                //currentSections = string.Join("• ", currentSections)
                currentSections = currentSections
            };

            return PartialView("_BulkPromoteStudentPartial", model);
        }

        [HttpPost]
        public async Task<IActionResult> BulkPromoteStudent(BulkPromoteViewModel model)
        {
            try
            {
                var students = await context.StudentSectionAssignments
                    .Where(sa => model.StudentIds.Contains(sa.StudentId.Value))
                    .ToListAsync();


                foreach (var assignment in students)
                {
                    assignment.SectionId = model.SectionId;
                    assignment.UpdatedAt = DateTime.UtcNow;
                }

                var userInfo = await GetCurrentUserInfo();

                await logService.LogActivity(
                    actionType: "Bulk Promoted",
                    entityName: "Students",
                    entityId: string.Join(",", model.StudentIds),
                    userId: userInfo.userId,
                    schoolId: userInfo.schoolId,
                    details: $"Admin {userInfo.username} bulk promoted {students.Count} students to Section {model.SectionId}",
                    username: userInfo.username
                );

                await context.SaveChangesAsync();

                logger.LogInformation("Bulk promoted {Students.Count} students to Section {SectionId}", students.Count, model.SectionId);

                return Json(new { success = true, message = $"Successfully promoted {students.Count} students" });
            }
            catch (DbUpdateException ex)
            {

                logger.LogError(ex, "Database error promoting students");
                return Json(new { success = false, message = "Database Error" });
            }
            catch (Exception ex)
            {

                logger.LogError(ex, "Unexpected error promoting students");
                return Json(new { success = false, message = "Something went wrong" });
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSecretary(SecretaryViewModel model)
        {
            bool schoolIdExisted = await context.Users.AnyAsync(s => s.SchoolId == model.SchoolId);
            if (schoolIdExisted)
            {
                ModelState.AddModelError("SchoolId", "School Id is already taken!");
            }

            bool fullNameExisted = await context.Users
                .IgnoreQueryFilters()
                .AnyAsync(f => f.FirstName == model.FirstName && f.MiddleName == model.MiddleName && f.LastName == model.LastName);

            if (fullNameExisted)
            {
                ModelState.AddModelError("FirstName", "A secretary with this Full name is already existed. Check archives also.");
                ModelState.AddModelError("MiddleName", "");
                ModelState.AddModelError("LastName", "");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            string? saveImagePath = null;
            //saveImageData is yung actual data ng image, na mag sesave sa imageFileData row sa database
            byte[]? saveImageData = null; //ginagamit ang byte para magsave ng files tulad ng images, pdf, etc. sa loob ng database

            if(model.imageFile != null)
            {
                //Konektado ito Sa AppUser na object para talagang magsave

                //create filename
                string newFile = DateTime.Now.ToString("yyyyMMddHHmmssff");
                newFile += Path.GetExtension(model.imageFile.FileName);
                //create physical path for the image
                string imageFullPath = environment.WebRootPath + "/ProfilePic/" + newFile;
                //save the actual image to ProfilePic file na naka declare sa variable na imageFullPath
                using(var stream = System.IO.File.Create(imageFullPath))
                {
                    await model.imageFile.CopyToAsync(stream);
                }
                //eto yung part na pag kasave nung actual image na (ex. image.jpg) eh pupunta na sya database sa imageFilePath na row
                saveImagePath = newFile;

                //Itong buong code na ito gang baba, dito kukunin yung mismong data ng image para iconvert sa byte para i-save na sa database
                //Kase diba sa viewmodel ko is IFormFile gamit ko dun, si ang code na ito is iconvert ang iFormFile to byte na iinput ng user
                using(var inputStream = model.imageFile.OpenReadStream())//para basahin yung upload file

                using(var memoryStream = new MemoryStream())// maging temporary kolektor o lalagyan ng data
                {
                    await inputStream.CopyToAsync(memoryStream);// eto na yung may hawak ng raw data
                    saveImageData = memoryStream.ToArray();// ngayon after makollect ng mismong data na naprocess na, dun na icoconvert sa byte
                }
            }

            //To Capitalize every first letter of word when inserting data
            TextInfo textinfo = CultureInfo.CurrentCulture.TextInfo;

            string formattedFirstName = textinfo.ToTitleCase(model.FirstName.ToLower());
            string formattedMiddleName = textinfo.ToTitleCase(model.MiddleName?.ToLower() ?? "");
            string formattedLastName = textinfo.ToTitleCase(model.LastName.ToLower());

            AppUser secretary = new AppUser()
            {
                //Email = model.Email,
                SchoolId = model.SchoolId,
                UserName = model.SchoolId.ToString(),
                FirstName = formattedFirstName,
                MiddleName = formattedMiddleName,
                LastName = formattedLastName,
                Sex = model.Sex,
                imageFileData = saveImageData,
                imageFilePath = saveImagePath,
                CreatedAt = DateTime.UtcNow
                
            };

            var result = await userManager.CreateAsync(secretary, model.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(secretary, "Secretary");
                var defaultAcademic = await GetCurrentAcademicPeriodId();

                var secretaryAssignment = new SecretaryAssignment
                {
                    SecretaryId = secretary.Id,
                    SectionId = model.SectionId,
                    //AcademicPeriodId = defaultAcademic, //(Optional)Used only for filtered archive
                    CreatedAt = DateTime.UtcNow,
                    StartDate = DateTime.UtcNow
                };

                context.SecretaryAssignments.Add(secretaryAssignment);
                await context.SaveChangesAsync();

                var userInfo = await GetCurrentUserInfo();

                await logService.LogActivity(
                    actionType: "Add",
                    entityName: "User",
                    entityId: secretary.Id.ToString(),
                    userId: userInfo.userId,
                    schoolId: userInfo.schoolId,
                    details: $"Admin {userInfo.username} Added secretary {secretary.FirstName} {secretary.MiddleName} {secretary.LastName}. LRN: {secretary.SchoolId}",
                    username: userInfo.username
                );

                return Json(new { success = true, message = "Secretary Added Successfully!" });
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    if (error.Code == "DuplicateUserName")
                    {
                        ModelState.AddModelError("Email", "Email is already used!");
                    } else if (error.Description.Contains("Password"))
                    {
                        ModelState.AddModelError("Password", error.Description);
                    }
                    else
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = errors });
            }   
        }
        [HttpGet]
        public async Task<IActionResult> PromoteSecretary(string id)
        {
            var secretary = await context.Users.FindAsync(id);
            //var secretary = await context.Users
            //    .Include(u => u.SecretariesAssignments)
            //    .FirstOrDefaultAsync();
            if (secretary == null)
            {
                logger.LogWarning("Secretary does not found with the ID: {SecretaryId}", id);
                return Json(new { success = false, error = "Secretary Not Found!" });
            }


            //1.var currentassignedClass = await context.SecretaryAssignments
            //    .Where(sa => sa.SecretaryId == secretary.Id)
            //    .Select(sec => sec.SectionId)
            //    .ToListAsync();

            var currentAssignment = await context.SecretaryAssignments
                .Include(sa => sa.Section)
                    .ThenInclude(s => s.Grade)
                .FirstOrDefaultAsync(sa => sa.SecretaryId == id);


            //2.var excludeSimilarGrade = await context.SecretaryAssignments
            //    .Include(sa => sa.Section)
            //        .ThenInclude(s => s.Grade)
            //    .Where(sa => sa.SecretaryId == secretary.Id)
            //    .FirstOrDefaultAsync();

            var currentSectionId = currentAssignment.SectionId;
            var currentGradeLevel = currentAssignment.Section.Grade.GradeLevel;

            //3.var exclude = excludeSimilarGrade.Section.Grade.GradeLevel;

            var alreadyAssigned = await context.SecretaryAssignments
                .Where(sa => sa.SecretaryId != id)
                .Select(sa => sa.SectionId)
                .ToListAsync();


            var availableGradeSection = await context.Sections
                .Include(s => s.Grade)
                .Where(s => s.Grade.GradeLevel > currentGradeLevel)
                .Where(s => s.Id != currentSectionId)
                .Where(s => !alreadyAssigned.Contains(s.Id))
                .OrderBy(g => g.Grade.GradeLevel)
                .Select(gs => new { gs.Id, gs.Grade.GradeLevel, gs.SectionName, gs.Track, gs.TVLProgram })
                .ToListAsync();

            var model = new PromoteSecretaryViewModel()
            {
                AvailableGradeSection = availableGradeSection.Select(gs => new SelectListItem
                {
                    Value = gs.Id.ToString(),
                    Text = gs.TVLProgram == null
                            ? $"Grade {gs.GradeLevel} - {gs.SectionName}, {gs.Track}" //if result is True
                           : $"Grade {gs.GradeLevel} - {gs.SectionName}, {gs.Track} - {gs.TVLProgram}" //Else False
                })
                .ToList(),

                SchoolId = secretary.SchoolId,
                FirstName = secretary.FirstName,
                MiddleName = secretary.MiddleName,
                LastName = secretary.LastName
            };

            return PartialView("_PromoteSecretaryPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteSecretary(string id, SavePromoteSecretaryViewModel model)
        {
            if (!ModelState.IsValid)
            {

                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );
                return Json(new { success = false, errors = errors });
            }
            //Use using in try catch if saving multiple database transacton like multiple .SaveChangesAsync()
            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    var promoteSecretary = await context.Users.FindAsync(id);

                    if (promoteSecretary == null)
                    {
                        logger.LogWarning("Secretary does not found with the ID: {SecretaryId}", id);
                        return Json(new { success = false, message = "Secretary id does not found" });
                    }

                    var secretaryAssignment = await context.SecretaryAssignments.FirstOrDefaultAsync(sa => sa.SecretaryId == id);

                    if (secretaryAssignment == null)
                    {
                        logger.LogWarning("Secretary class does not found with the ID: {SecretaryId}", id);
                        return Json(new { success = false, message = "Secretary class id does not found" });
                    }

                    var oldSectionId = secretaryAssignment.SectionId;

                    secretaryAssignment.SectionId = model.SectionId;
                    //secretaryAssignment.UpdatedAt = DateTime.UtcNow;
                    secretaryAssignment.StartDate = DateTime.UtcNow;

                    ///GAMITIN KAPAG MAG SET NG BAGONG SECRETARY ASSIGNMENT
                    //1.var oldSecretaryAssignment = await context.SecretaryAssignments
                    //    .Where(sa => sa.SecretaryId == id)
                    //    .FirstOrDefaultAsync();

                    //oldSecretaryAssignment.IsDeleted = true;
                    //oldSecretaryAssignment.DeletedAt = DateTime.UtcNow;

                    //await context.SaveChangesAsync();

                    //var defaultAcademic = await GetCurrentAcademicPeriodId();

                    //var newSecretaryAssignment = new SecretaryAssignment()
                    //{
                    //    SecretaryId = id,
                    //    SectionId = model.SectionId,
                    //    AcademicPeriodId = defaultAcademic,
                    //    CreatedAt = DateTime.UtcNow
                    //};

                    var userInfo = await GetCurrentUserInfo();

                    await logService.LogActivity(
                        actionType: "Promoted",
                        entityName: "Secretary",
                        entityId: promoteSecretary.Id.ToString(),
                        userId: userInfo.userId,
                        schoolId: userInfo.schoolId,
                        details: $"Secretary {promoteSecretary.FirstName} {promoteSecretary.LastName} promoted from {oldSectionId} to Class {model.SectionId}",
                        username: userInfo.username
                    );

                    //2.context.SecretaryAssignments.Add(newSecretaryAssignment);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    logger.LogInformation("Secretary {SecretaryId} promoted successfully by Admin", id);
                    
                    return Json(new { success = true, message = "Secretary Promoted Successfully" });

                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();

                    logger.LogError(ex, "Database error promoting secretary {SecretaryId}", id);
                    return Json(new { success = false, message = "Database Error" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    logger.LogError(ex, "Unexpected error restoring secretary {SecretaryId}", id);
                    return Json(new { success = false, message = "Something went wrong" });
                }
            }       
        }
        

        [HttpGet]
        public async Task<IActionResult> ViewSecretary(string id)
        {
            var secretary = await context.Users.FindAsync(id);

            if(secretary == null)
            {
                return Json(new { success = false, error = "Secretary Not Found!" });
            }

            //1. Nakuha na yung data sa database
            var secretaryAssignment = await context.SecretaryAssignments
                .Include(s => s.Section)
                    .ThenInclude(g => g.Grade)
                .Where(sc => sc.SecretaryId == id)
                .FirstOrDefaultAsync();


            var model = new EditSecretaryViewModel()
            {
                //Email = secretary.Email,
                SchoolId = secretary.SchoolId,
                FirstName = secretary.FirstName,
                MiddleName = secretary.MiddleName,
                LastName = secretary.LastName,
                Sex = secretary.Sex,
                imageFilePath = secretary.imageFilePath,
                CreatedAt = secretary.CreatedAt,
                secretaryClass = secretaryAssignment,
            };

            return PartialView("_ViewSecretaryPartial", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditSecretary(string id)
        {
            var secretary = await context.Users.FindAsync(id);

            if(secretary == null)
            {
                return Json(new { success = false, error = "Secretary does not found" });
            }      

            //Retrieve current student's assigned Grade and Section  to secretary
            var studentsGradeSection = await context.SecretaryAssignments
                .Include(sa => sa.Section)
                    .ThenInclude(s => s.Grade)
                //.Select(s => s.SectionId)
                .FirstOrDefaultAsync(sa => sa.SecretaryId == id);

            if (studentsGradeSection == null)
            {
                logger.LogWarning("No assignment found for secretary: {SecretaryId}", id);
                return Json(new { success = false, error = "No assignment found for secretary" });
            }

            //Always use this pattern if may gusto kang kunin sa isang query
            var currentGrade = studentsGradeSection.Section.Grade.Id;

            var alreadyAssigned = await context.SecretaryAssignments
                .Where(sa => sa.SecretaryId != id)
                .Select(s => s.SectionId)
                .ToListAsync();

            //Retrieve all available Sections
            var allSection = await context.Sections
                    .Include(g => g.Grade)
                    .Include(g => g.SecretaryAssignments)
                    .Where(s => s.GradesId == currentGrade && !alreadyAssigned.Contains(s.Id))
                    .Select(ags => new { ags.Id, ags.Grade.GradeLevel, ags.SectionName, ags.Track, ags.TVLProgram })
                    .ToListAsync();



            var model = new EditSecretaryViewModel()
            {
                AvailableGradeSection = allSection.Select(gs => new SelectListItem
                {
                    Value = gs.Id.ToString(),
                    Text = $"Grade {gs.GradeLevel} {gs.SectionName} {gs.Track} {gs.TVLProgram}",
                }).ToList(),

                SectionId = studentsGradeSection.SectionId,
                //Email = secretary.Email,
                SchoolId = secretary.SchoolId,
                FirstName = secretary.FirstName,
                MiddleName = secretary.MiddleName,
                LastName = secretary.LastName,  
                Sex = secretary.Sex,
                imageFilePath = secretary.imageFilePath,
                CreatedAt = secretary.CreatedAt,
            };

            return PartialView("_EditSecretaryPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSecretary(string id, EditSecretaryViewModel model)
        {
            //var editSecretary = await context.Users.FindAsync(id);
            var editSecretary = await userManager.FindByIdAsync(id.ToString());

            if (editSecretary == null)
            {
                return Json(new { success = false, error = "Secretary does not found!" });
            }

            //check for email duplication
            //bool sameEmail = await context.Users.AnyAsync(e => e.Email == model.Email && e.Id != id);

            //if (sameEmail)
            //{
            //    ModelState.AddModelError("Email", "Email is already used!");
            //}

            //duplicate check excluding self
            //Dito gumamit ng s.Id != id para pag nag check ng id is hindi isasama yung current id sa pag hahanap
            //check for Schoold Id Duplication
            bool schoolIdExisted = await context.Users.AnyAsync(s => s.SchoolId == model.SchoolId && s.Id != id);

            if (schoolIdExisted)
            {
                ModelState.AddModelError("SchoolId", "School Id is already taken!");
            }

            //Check if Full name duplication
            bool FullNameExisted = await context.Users.AnyAsync(f => f.FirstName == model.FirstName && f.MiddleName == model.MiddleName && f.LastName == model.LastName && f.Id != id);
            if (FullNameExisted)
            {
                ModelState.AddModelError("FirstName", "A secretary with this Full name is already existed");
                ModelState.AddModelError("MiddleName", " ");
                ModelState.AddModelError("LastName", " ");
            }


            //FindAsync() = used for primary key lookup lang(yung int Id ng SecretaryAssignment)
            //FirstOrDefaultAsync() = used for filtering by any column(like SecretaryId which is the GUID)
            //check muna if yung Id(secretary) sa AppUser is equals sa SecretaryId na nasa SecretaryAssignements Table

            //var secretaryAssigned = await context.SecretaryAssignments.FirstOrDefaultAsync(sa => sa.SecretaryId == id);
            var secretaryAssigned = await context.SecretaryAssignments
                .Include(sa => sa.Section)
                    .ThenInclude(s => s.Grade)
                .FirstOrDefaultAsync(sa => sa.SecretaryId == id);

            if (secretaryAssigned == null)
            {
                return Json(new { sucess = false, message = "Secretary assignment Id does not found" });

            }

            if (!ModelState.IsValid)
            {

                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );
                return Json(new { sucess = false, errors = errors });
            }

            string? saveImagePath = null;
            byte[]? saveImageData = null;

            if (model.imageFile != null)
            {
                string newFile = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                newFile += Path.GetExtension(model.imageFile.FileName);

                string imageFullPath = environment.WebRootPath + "/ProfilePic/" + newFile;

                using (var stream = System.IO.File.Create(imageFullPath))
                {
                    await model.imageFile.CopyToAsync(stream);
                }

                //check muna sa database if may laman ba yung image ng user
                if (!string.IsNullOrEmpty(editSecretary.imageFilePath))
                {
                    //if may laman saka palang bubuuin ang filepath
                    string oldImageFullPath = environment.WebRootPath + "/ProfilePic/" + editSecretary.imageFilePath;
                    //tapos kapag may laman nga, dun palang mag delete
                    if (oldImageFullPath != null)
                    {
                        //then mag execute to!
                        System.IO.File.Delete(oldImageFullPath);
                    }

                }

                saveImagePath = newFile;

                using (var inputStream = model.imageFile.OpenReadStream())
                using (var memoryStream = new MemoryStream())
                {
                    await inputStream.CopyToAsync(memoryStream);
                    saveImageData = memoryStream.ToArray();
                }
                //IMPORTANT: Assign to editTeacher
                editSecretary.imageFilePath = saveImagePath;
                editSecretary.imageFileData = saveImageData;
            }

            //To Capitalize every first letter of word when inserting data
            TextInfo textinfo = CultureInfo.CurrentCulture.TextInfo;

            string formattedFirstName = textinfo.ToTitleCase(model.FirstName.ToLower());
            string formattedMiddleName = textinfo.ToTitleCase(model.MiddleName?.ToLower() ?? "");
            string formattedLastName = textinfo.ToTitleCase(model.LastName.ToLower());

            //editSecretary.Email = model.Email;
            editSecretary.SchoolId = model.SchoolId;
            editSecretary.UserName = model.SchoolId.ToString();
            editSecretary.FirstName = formattedFirstName;
            editSecretary.MiddleName = formattedMiddleName;
            editSecretary.LastName = formattedLastName;
            editSecretary.Sex = model.Sex;

            var result = await userManager.UpdateAsync(editSecretary);

            var secretaryClassAssignment = $"Grade {secretaryAssigned.Section.Grade.GradeLevel} - {secretaryAssigned.Section.SectionName}";
            var secretaryTrackInfo = !string.IsNullOrEmpty(secretaryAssigned.Section.Track) ? $"{secretaryAssigned.Section.Track}" : "";
            var secretaryTVLProgram = !string.IsNullOrEmpty(secretaryAssigned.Section.TVLProgram) ? $"{secretaryAssigned.Section.TVLProgram}" : "";

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Edit",
                entityName: "User",
                entityId: editSecretary.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} edited secretary {editSecretary.FirstName} {editSecretary.MiddleName} {editSecretary.LastName}. LRN: {editSecretary.SchoolId} of Class of {secretaryClassAssignment} {secretaryTrackInfo} {secretaryTVLProgram}",
                username: userInfo.username
            );

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
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
                var removePassword = await userManager.RemovePasswordAsync(editSecretary);
                if (removePassword.Succeeded)
                {
                    var newPassword = await userManager.AddPasswordAsync(editSecretary, model.NewPassword);
                    if (!newPassword.Succeeded)
                    {
                        foreach(var error in newPassword.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        var errors = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                        );
                        return Json(new { success = false, errors = errors });
                    }
                }
            }

            secretaryAssigned.SecretaryId = editSecretary.Id;
            secretaryAssigned.SectionId = model.SectionId;
            secretaryAssigned.StartDate = DateTime.UtcNow;

            context.SecretaryAssignments.Update(secretaryAssigned);
            await context.SaveChangesAsync();

            // No need ng gamitin ang SaveChangesAsync() kase Ang UserManager.UpdateAsync(), RemovePasswordAsync(), at AddPasswordAsync() ay automatically nag - save na sa database.
            //await context.SaveChangesAsync();
            return Json(new { success = true, message = "Secretary Updated Successfully" });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSecretary(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { success = false, message = "ID is required" });
                }
                //var secretary = await userManager.FindByIdAsync(id);
                var secretary = await context.Users
                    .Include(u => u.SecretariesAssignments)
                    .Where(u => u.Id == id)
                    .FirstOrDefaultAsync();

                if (secretary == null)
                {
                    logger.LogWarning("Secretary not Found with the ID: {SecretaryId}", id);
                    return Json(new { success = false, error = "Secretary does not Found!" });
                }

                if (!string.IsNullOrEmpty(secretary.imageFilePath))
                {
                    string ImagePath = Path.Combine(environment.WebRootPath, "ProfilePic", secretary.imageFilePath);
                    if (System.IO.File.Exists(ImagePath))
                    {
                        System.IO.File.Delete(ImagePath);
                    }
                }

                var time = DateTime.UtcNow;

                secretary.IsDeleted = true;
                secretary.DeletedAt = time;

                foreach (var secretaryAssignments in secretary.SecretariesAssignments)
                {
                    secretaryAssignments.IsDeleted = true;
                    secretaryAssignments.DeletedAt = time;
                }

                //context.Users.Remove(secretary);
                await context.SaveChangesAsync();

                var userInfo = await GetCurrentUserInfo();

                await logService.LogActivity(
                    actionType: "Delete",
                    entityName: "User",
                    entityId: secretary.Id.ToString(),
                    userId: userInfo.userId,
                    schoolId: userInfo.schoolId,
                    details: $"User {userInfo.username} deleted secretary {secretary.FirstName} {secretary.MiddleName} {secretary.LastName}. LRN: {secretary.SchoolId}",
                    username: userInfo.username
                );

                logger.LogInformation("Secretary successfully deleted with the ID: {SecretaryId}", id);

                return Json(new { success = true, message = "Secretary Deleted Successfully!" });
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error deleting secretary {SecretaryId}", id);
                return Json(new { success = false, message = "Database Error" });
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Unexpected error restoring deleting {SecretaryId}", id);
                return Json(new { success = false, message = "Something went wrong" });

            }

        }

        [HttpGet]
        public async Task<IActionResult> RestoreDeletedSecretary()
        {
            var currentAcademicPeriod = await GetCurrentAcademicPeriodId();

            var userRoleId = await context.Roles
                .Where(r => r.Name == "Secretary")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            //bago
            //var assignCLass = await context.SecretaryAssignments
            //    .IgnoreQueryFilters()
            //    .Where(sa => sa.IsDeleted == true && sa.AcademicPeriodId == currentAcademicPeriod)
            //    .Select(sa => sa.SecretaryId)
            //    .ToListAsync();

            //Using two or more Where clause in query is a good practice!
            var deletedSecretary = await context.Users
                .IgnoreQueryFilters()
                //.Include(u => u.SecretariesAssignments.Where(sa => sa.AcademicPeriodId == currentAcademicPeriod))
                .Include(u => u.SecretariesAssignments)
                .Include(u => u.SecretariesAssignments)
                    .ThenInclude(s => s.Section.Grade)
                .Include(g => g.SecretariesAssignments)
                    .ThenInclude(sa => sa.Section.SectionSubjects) 
                
                .Where(u => context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == userRoleId))
                .Where(u => u.IsDeleted == true)
                //.Where(u => assignCLass.Contains(u.Id)) //bago
                .ToListAsync();

            return PartialView("_RestoreDeletedSecretaryPartial", deletedSecretary);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreDeletedSecretary(string secretaryId)
        {
            try
            {
                if (string.IsNullOrEmpty(secretaryId))
                {
                    return Json(new { success = false, message = "Id is required" });
                }

                //var secretary = await context.Users
                //    .Where(u => u.Id == secretaryId)
                //    .FirstOrDefaultAsync();

                var userRoleId = await context.Roles
                .Where(r => r.Name == "Secretary")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

                //Using two or more Where clause in query is a good practice!
                var secretary = await context.Users
                    .IgnoreQueryFilters()
                    .Include(u => u.SecretariesAssignments)
                        .ThenInclude(s => s.Section.Grade)
                    .Include(g => g.SecretariesAssignments)
                        .ThenInclude(sa => sa.Section.SectionSubjects)
                    .Where(u => context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == userRoleId))
                    .FirstOrDefaultAsync(u => u.Id == secretaryId);

                if (secretary == null)
                {
                    logger.LogWarning("Secretary was not found with the ID: {SecretaryId}", secretaryId);
                    return Json(new { success = false, message = "Secretary doesn't found" });
                }

                secretary.IsDeleted = false;
                secretary.DeletedAt = null;

                foreach(var secretaryAssignment in secretary.SecretariesAssignments)
                {
                    secretaryAssignment.IsDeleted = false;
                    secretaryAssignment.DeletedAt = null;
                }

                await context.SaveChangesAsync();

                var userInfo = await GetCurrentUserInfo();

                await logService.LogActivity(
                    actionType: "Restore",
                    entityName: "Secretary",
                    entityId: secretary.Id.ToString(),
                    userId: userInfo.userId,
                    schoolId: userInfo.schoolId,
                    details: $"Admin {userInfo.username} restore Secretary {secretary.FirstName} {secretary.LastName}. LRN: {secretary.LRN}",
                    username: userInfo.username
                );

                logger.LogInformation("Teacher {TeacherId} restored successfully by {username}",
                                    secretaryId, userInfo.username);

                var remainingUserRoleId = await context.Roles
                                .Where(r => r.Name == "Secretary")
                                .Select(r => r.Id)
                                .FirstOrDefaultAsync();

                var remainingDeletedSecretary = await context.Users
                    .IgnoreQueryFilters()
                    .Include(u => u.SecretariesAssignments)
                    .Include(u => u.SecretariesAssignments)
                        .ThenInclude(s => s.Section.Grade)
                    .Include(g => g.SecretariesAssignments)
                        .ThenInclude(sa => sa.Section.SectionSubjects)
                    .Where(u => context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == remainingUserRoleId))
                    .Where(u => u.IsDeleted == true)
                    .ToListAsync();

                //Using two or more Where clause in query is a good practice!
                //var remainingDeletedSecretary = await context.Users
                //    .IgnoreQueryFilters()
                //    .Where(u => context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == remainingUserRoleId))
                //    .Where(u => u.IsDeleted == true)
                //    .ToListAsync();

                return PartialView("_RestoreDeletedSecretaryPartial", remainingDeletedSecretary);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error restoring secretary{secretaryId}", secretaryId);
                return Json(new { success = false, message = "Database Error" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error restoring secretary {SecretaryId}", secretaryId);
                return Json(new { success = false, message = "Something went wrong" });
            }
        }


        ///        //NO NEED NA LALO NA INDUSTRY STANDARD. GAMITIN NALANG IF MISMONG CLIENT ANG NAG SUGGEST!
        ///HINDI PA GAANONG AYOS, PARA MAAYOS IS DAPAT NULLABLE YUNG SecretaryId sa SecretaryAssignment
        //[HttpDelete]
        //public async Task<IActionResult> PermanentDeleteSecretary(string id)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(id))
        //        {
        //            return Json(new { success = false, message = "ID is required" });
        //        }

        //        var secretaryId = await context.Users
        //                           .IgnoreQueryFilters()
        //                           .FirstOrDefaultAsync(u => u.Id == id);            

        //        if(secretaryId == null)
        //        {
        //            logger.LogWarning("Secretary not found with the Id of {SecretaryId}", id);
        //            return Json(new { success = false, message = "Secretary id is Null" });
        //        }

        //        var secretaryAssignment = await context.SecretaryAssignments
        //                                    .IgnoreQueryFilters()
        //                                    .Where(sa => sa.SecretaryId == secretaryId.Id)
        //                                    .FirstOrDefaultAsync();


        //        if (!string.IsNullOrEmpty(secretaryId.imageFilePath))
        //        {
        //            string ImagePath = Path.Combine(environment.WebRootPath, "ProfilePic", secretaryId.imageFilePath);
        //            if (System.IO.File.Exists(ImagePath))
        //            {
        //                System.IO.File.Delete(ImagePath);
        //            }
        //        }

        //        if(secretaryAssignment != null)
        //        {
        //            context.Remove(secretaryAssignment);
        //        }

        //        context.Remove(secretaryId);
        //        await context.SaveChangesAsync();

        //        var userInfo = await GetCurrentUserInfo();

        //        await logService.LogActivity(
        //            actionType: "Delete",
        //            entityName: "User",
        //            entityId: secretaryId.Id.ToString(),
        //            userId: userInfo.userId,
        //            schoolId: userInfo.schoolId,
        //            details: $"Admin {userInfo.username} deleted secretary {secretaryId.FirstName} {secretaryId.MiddleName} {secretaryId.LastName}. LRN: {secretaryId.SchoolId}",
        //            username: userInfo.username
        //        );

        //        var remainingUserRoleId = await context.Roles
        //                        .Where(r => r.Name == "Secretary")
        //                        .Select(r => r.Id)
        //                        .FirstOrDefaultAsync();

        //        var remainingDeletedSecretary = await context.Users
        //            .IgnoreQueryFilters()
        //            .Include(u => u.SecretariesAssignments)
        //            .Include(u => u.SecretariesAssignments)
        //                .ThenInclude(s => s.Section.Grade)
        //            .Include(g => g.SecretariesAssignments)
        //                .ThenInclude(sa => sa.Section.SectionSubjects)
        //            .Where(u => context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == remainingUserRoleId))
        //            .Where(u => u.IsDeleted == true)
        //            .ToListAsync();

        //        logger.LogInformation("Secretary successfully deleted with the ID: {SecretaryId}", id);
        //        //return Json(new { success = true, message = "Secretary Deleted Permanently!" });
        //        return PartialView("_RestoreDeletedSecretaryPartial", remainingDeletedSecretary);
        //    }
        //    catch (DbUpdateException ex)
        //    {
        //        logger.LogError(ex, "Database error restoring secretary{SecretaryId}", id);
        //        return Json(new { success = false, message = "Database Error" });
        //    }
        //    catch(Exception ex)
        //    {
        //        logger.LogError(ex, "Unexpected error restoring secretary {SecretaryId}", id);
        //        return Json(new { success = false, message = "Something went wrong" });
        //    }     
        //}

        [HttpGet]
        public async Task<IActionResult> AttendanceReport(string? SelectedTeacher,
                                                            int? SelectedAcademicPeriod,
                                                            int? SelectedTeacherAssignment, //selected  Class
                                                            string? SelectedAttendanceStatus,
                                                            DateTime? StartDate, //Date range start
                                                            DateTime? EndDate)
        {
            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(  
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            //get Current Academic Period
            var currentAcademicPeriod = await context.AcademicPeriods.FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            // Get all available Academic period
            var allAcademicPeriod = await context.AcademicPeriods
                                    .IgnoreQueryFilters()
                                    .OrderBy(ap => ap.Year)
                                    .ToListAsync();

            //Get all teacher list
            var allTeacher = await userManager.GetUsersInRoleAsync("Teacher");

            List<SelectListItem> teacherLists = new List<SelectListItem>();

            if (SelectedAcademicPeriod.HasValue)
            {
                //var user = await userManager.GetUsersInRoleAsync("Teacher");
                //teacherLists = user
                //    .Select(u => new SelectListItem
                //    {
                //        Value = u.Id,
                //        Text = $"{u.FirstName} {u.MiddleName} {u.LastName} - {u.positionTitle}",

                //    })
                //    .ToList();

                var teacherRoleId = await context.Roles //Bata may context automatic IQueryable yan
                .Where(r => r.Name == "Teacher")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

                teacherLists = await context.Users
                    .IgnoreQueryFilters()
                    .Where(u => context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == teacherRoleId))
                    .Select(al => new SelectListItem
                    {
                        Value = al.Id,
                        Text = $"{al.FirstName} {al.MiddleName} {al.LastName} - {al.positionTitle}",

                    })
                    .ToListAsync();
            }

            //Check if teacher is selected
            List<SelectListItem> teacherClass = new List<SelectListItem>();

            if (!string.IsNullOrEmpty(SelectedTeacher))
            {
                teacherClass = await context.TeacherAssignments
                                .IgnoreQueryFilters()
                                .Include(ta => ta.SectionSubject)
                                    .ThenInclude(ss => ss.Subject)
                                .Include(sn => sn.SectionSubject.Section)
                                    .ThenInclude(g => g.Grade)
                                .Where(s => s.TeacherId == SelectedTeacher && s.AcademicPeriodId == SelectedAcademicPeriod)
                                .OrderBy(s => s.SectionSubject.Section.Grade.GradeLevel)
                                .Select(tc => new SelectListItem
                                {
                                    Value = tc.Id.ToString(),
                                    Text = $"Grade {tc.SectionSubject.Section.Grade.GradeLevel} {tc.SectionSubject.Section.SectionName} {tc.SectionSubject.Section.Track} {tc.SectionSubject.Section.TVLProgram} {tc.SectionSubject.Subject.SubjectDescription}",
                                })
                                .ToListAsync();
            }


            List<AdminAttendanceReportData> studentAttendance = new List<AdminAttendanceReportData>();
            List<DateTime> dateRange = new List<DateTime>();
            //Check all filters
            if (SelectedAcademicPeriod.HasValue && !string.IsNullOrEmpty(SelectedTeacher) && SelectedTeacherAssignment.HasValue && StartDate.HasValue && EndDate.HasValue)
            {
                var selectedClass = await context.TeacherAssignments
                                .IgnoreQueryFilters()
                                .Include(ta => ta.SectionSubject)
                                    .ThenInclude(ss => ss.Subject)
                                .Include(sn => sn.SectionSubject.Section)
                                    .ThenInclude(g => g.Grade)
                                .Where(s => s.TeacherId == SelectedTeacher && s.AcademicPeriodId == SelectedAcademicPeriod)
                                .FirstOrDefaultAsync(tc => tc.Id == SelectedTeacherAssignment.Value);

                if (selectedClass != null)
                {
                    var sectionId = selectedClass.SectionSubject.SectionId;
                    var sectionSubjectId = selectedClass.SectionSubject.Id;

                    for(var date = StartDate.Value; date <= EndDate.Value; date = date.AddDays(1))
                    {
                        dateRange.Add(date);
                    }

                    var students = await context.StudentSectionAssignments
                                    .IgnoreQueryFilters()
                                    .Include(ssa => ssa.Student)
                                    //.Where(ssa => ssa.SectionId == sectionId)
                                    .Where(ssa => ssa.SectionId == sectionId && ssa.StudentId != null) // gamitin kapag gustong walang lalabas sa attendnace report kapag deelted nayung student
                                    .OrderBy(ssa => ssa.Student.LastName)
                                    .ToListAsync();

                    var attendanceRecord = context.Attendances
                                            .IgnoreQueryFilters()
                                            .Where(a => a.SectionSubjectId == sectionSubjectId
                                                    && a.AttendanceDate.Date >= StartDate.Value.Date
                                                    && a.AttendanceDate.Date <= EndDate.Value.Date
                                                    && a.AcademicPeriod.Id == SelectedAcademicPeriod.Value);
                                            //.ToListAsync();

                    //sa part na ito ay tinanggal ntin ang ToList sa attendance record query para gumana yung sa SelectedAttednanceStatus
                    //Since IQeuryable si Ef, hindi pa siya agad gaganda hanggat walang tolist
                    if (!string.IsNullOrEmpty(SelectedAttendanceStatus))
                    {
                        attendanceRecord = context.Attendances
                                            .IgnoreQueryFilters()
                                            .Where(a => a.AttendanceMarking == SelectedAttendanceStatus);
                    }

                    var record = await attendanceRecord.ToListAsync();

                    //Builder Report Data
                    foreach(var student in students)
                    {
                        var studentName = student.Student != null
                            ? $"{student.Student.FirstName} {student.Student.MiddelName} {student.Student.LastName}"
                            : "Deleted Student";

                        var studentData = new AdminAttendanceReportData
                        {
                            StudentSectionAssignmentId = student.Id,
                            //StudentId = student.StudentId,
                            StudentName = studentName,
                            DailyAttendance = new List<string>()
                        };

                        foreach(var date in dateRange)
                        {

                            var attendance = record
                                .FirstOrDefault(ar => ar.StudentSectionAssignmentId == student.Id
                                                && ar.AttendanceDate.Date == date.Date);

                            ///OLD QUERY FOR FETCHING STUDENT ID

                            //var attendance = record
                            //    .FirstOrDefault(ar => ar.StudentId == student.StudentId
                            //                    && ar.AttendanceDate.Date == date.Date);

                            if (attendance != null)
                            {
                                studentData.DailyAttendance.Add(
                                    attendance.AttendanceMarking == "Present" ? "P" :
                                    attendance.AttendanceMarking == "Late" ? "L" :
                                    attendance.AttendanceMarking == "Absent" ? "A" :
                                    attendance.AttendanceMarking == "Cutting" ? "C" :
                                    attendance.AttendanceMarking == "Excuse" ? "E" : "-"
                                );
                            }
                            else
                            {
                                studentData.DailyAttendance.Add("-");
                            }
                        }

                        if(studentData.DailyAttendance.Any(d => d != "-"))
                        {
                            studentAttendance.Add(studentData);
                        }

                    }   
                }

            }

            var model = new AdminAttendanceReportViewModel()
            {
                //teacherList = allTeacher.Select(at => new SelectListItem
                //{
                //    Value = at.Id.ToString(),
                //    Text = $"{at.FirstName} {at.MiddleName} {at.LastName} - {at.positionTitle}",

                //}).ToList(),
                teacherList = teacherLists,

                teacherClass = teacherClass,    

                academicPeriod = allAcademicPeriod.Select(aap => new SelectListItem
                {
                    Value = aap.Id.ToString(),
                    Text = $"{aap.Year} - {aap.GradingPeriod} Grading " + (aap.IsDefault == 1 ? "✓ Active" : ""),
                }).ToList(),

                SelectedAcademicPeriod = SelectedAcademicPeriod,
                SelectedTeacher = SelectedTeacher,
                SelectedTeacherAssignment = SelectedTeacherAssignment,
                SelectedAttendanceStatus = SelectedAttendanceStatus,
                StudentAttendance = studentAttendance,
                DateRange = dateRange,
                StartDate = StartDate,
                EndDate = EndDate
            };

            return View(model);
        }

        //FOr ajax for teacherclass dropdown when selecting specific teacher
        [HttpGet]
        public async Task<JsonResult> GetTeacherAssignments(string teacherId, int academicPeriodId)
        {
            var allAcademicPeriod = await context.AcademicPeriods
                                    .IgnoreQueryFilters()
                                    .ToListAsync();

            var teacherClass = await context.TeacherAssignments
                                .IgnoreQueryFilters()
                                .Include(ta => ta.SectionSubject)
                                    .ThenInclude(ss => ss.Subject)
                                .Include(sn => sn.SectionSubject.Section)
                                    .ThenInclude(g => g.Grade)
                                .Include(ap => ap.AcademicPeriod)
                                .Where(s => s.TeacherId == teacherId && s.AcademicPeriodId == academicPeriodId)
                                .OrderBy(s => s.SectionSubject.Section.Grade.GradeLevel)
                                .Select(tc => new
                                {
                                    Value = tc.Id.ToString(),
                                    Text = $"Grade {tc.SectionSubject.Section.Grade.GradeLevel} {tc.SectionSubject.Section.SectionName} {tc.SectionSubject.Section.Track} {tc.SectionSubject.Section.TVLProgram} {tc.SectionSubject.Subject.SubjectDescription}",
                                })
                                .ToListAsync();

            return Json(teacherClass);

        }

        [HttpGet]
        public async Task<JsonResult> GetAllTeacher()
        {
            ///no need Explicitly IQueryable kapag wla nmang complex query like my conditional after ng query
            ///IQueryable<ApplicationUser> query = _context.Users

            var teacherRoleId = await context.Roles //Bata may context automatic IQueryable yan
                .Where(r => r.Name == "Teacher")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var teachers = await context.Users
                .IgnoreQueryFilters()
                .Where(u => context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == teacherRoleId))
                .Select(al => new
                {
                    Value = al.Id,
                    Text = $"{al.FirstName} {al.MiddleName} {al.LastName} - {al.positionTitle}",

                })
                .ToListAsync();

            return Json(teachers);

            //var allTeacher = await userManager.GetUsersInRoleAsync("Teacher");

            //var result = allTeacher
            //    .Select(al => new
            //    {
            //        Value = al.Id,
            //        Text = $"{al.FirstName} {al.MiddleName} {al.LastName} - {al.positionTitle}",

            //    })
            //    .ToList();

            //return Json(result);

            //IQueryable<AppUser> query = context.Users.IgnoreQueryFilters();

            //if(query != null)
            //{
            //    query = query.Where(u => u.Id == teacherId);
            //}

            //var allTeacher = await query.Where(u => context.UserRoles
            //                            .Any(ur => ur.UserId == u.Id &&
            //                            context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Teacher")))
            //                .ToListAsync();
            //var result = allTeacher
            //    .Select(al => new
            //    {
            //        Value = al.Id,
            //        Text = $"{al.FirstName} {al.MiddleName} {al.LastName} - {al.positionTitle}",

            //    })
            //    .ToList();

            //return Json(result);
        }

        /// <summary>
        /// BACKUP AND RESTORE FEAUTRE
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ExportAttendanceReport(string? SelectedTeacher, string? SelectedAttendanceStatus, int? SelectedAcademicPeriod, int? SelectedTeacherAssignment, DateTime? StartDate, DateTime? EndDate)
        {
            if (!SelectedAcademicPeriod.HasValue 
                || string.IsNullOrEmpty(SelectedTeacher)
                || !SelectedTeacherAssignment.HasValue 
                || !StartDate.HasValue 
                || !EndDate.HasValue)
            {
                TempData["ErrorMessage"] = "Please select all filters before exporting.";
                return RedirectToAction("AttendanceReport");
                //return Json(new { success = false, message = "Please select all filters before exporting." });
            }

            var teacher = GetCurrentUserId();
            var query = await userManager.FindByIdAsync(teacher);

            var firstName = query.FirstName;
            var middleName = query?.MiddleName;
            var lastName = query.LastName;

            // Get the same data as the view
            var selectedClass = await context.TeacherAssignments
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Subject)
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Section)
                        .ThenInclude(s => s.Grade)
                .Where(ta => ta.TeacherId == SelectedTeacher && ta.AcademicPeriodId == SelectedAcademicPeriod)
                .FirstOrDefaultAsync(tc => tc.Id == SelectedTeacherAssignment.Value);

            if (selectedClass == null)
            {
                TempData["ErrorMessage"] = "Selected class not found.";
                return RedirectToAction("AttendanceReport");
                //return Json(new { success = false, message = "Selected class not found" });

            }

            var sectionId = selectedClass.SectionSubject.SectionId;
            var sectionSubjectId = selectedClass.SectionSubject.Id;

            var dateRange = new List<DateTime>();
            for (var date = StartDate.Value; date <= EndDate.Value; date = date.AddDays(1))
            {
                dateRange.Add(date);
            }

            //Get students
            var students = await context.StudentSectionAssignments
                .IgnoreQueryFilters()
                .Include(ssa => ssa.Student)
                //.Where(ssa => ssa.SectionId == sectionId)
                .Where(ssa => ssa.SectionId == sectionId && ssa.StudentId != null)
                .OrderBy(ssa => ssa.Student.LastName)
                .ToListAsync();


            //Get Attendance Record
            var attendanceRecord = context.Attendances
                .IgnoreQueryFilters()
                //.Include(a => a.StudentSectionAssignment)
                .Where(a => a.SectionSubjectId == sectionSubjectId
                        && a.AttendanceDate.Date >= StartDate.Value.Date
                        && a.AttendanceDate.Date <= EndDate.Value.Date
                        && a.AcademicPeriod.Id == SelectedAcademicPeriod.Value);
                //.ToListAsync();

                if (!string.IsNullOrEmpty(SelectedAttendanceStatus))
                {
                //attendanceRecord = context.Attendances
                //                .IgnoreQueryFilters()
                //                .Where(a => a.AttendanceMarking == SelectedAttendanceStatus);

                attendanceRecord = attendanceRecord
                                    .Where(a => a.AttendanceMarking == SelectedAttendanceStatus);
                }

                var record = await attendanceRecord.ToListAsync();

            //build report data
            var studentAttendance = new List<AdminAttendanceReportData>();

            foreach (var student in students)
            {
                //var studentName = student.Student != null
                //            ? $"{student.Student.LastName}, {student.Student.FirstName} {student.Student.MiddelName} "
                //            : "Deleted Student";

                var studentData = new AdminAttendanceReportData
                {
                    StudentSectionAssignmentId = student.Id,
                    //StudentId = student.StudentId,
                    StudentName = $"{student.Student.LastName}, {student.Student.FirstName} {student.Student.MiddelName} ",
                    //StudentName = studentName,
                    DailyAttendance = new List<string>()
                };

                foreach (var date in dateRange)
                {
                    var attendance = record
                        .FirstOrDefault(ar => ar.StudentSectionAssignmentId == student.Id
                                        && ar.AttendanceDate.Date == date.Date);

                    if (attendance != null)
                    {
                        studentData.DailyAttendance.Add(
                            attendance.AttendanceMarking == "Present" ? "P" :
                            attendance.AttendanceMarking == "Late" ? "L" :
                            attendance.AttendanceMarking == "Absent" ? "A" :
                            attendance.AttendanceMarking == "Cutting" ? "C" :
                            attendance.AttendanceMarking == "Excuse" ? "E" : "-"
                        );
                    }
                    else
                    {
                        studentData.DailyAttendance.Add("-");
                    }
                }

                if (studentData.DailyAttendance.Any(d => d != "-"))
                {
                    studentAttendance.Add(studentData);
                }
            }

            //check if no data
            if (!studentAttendance.Any())
            {
                TempData["ErrorMessage"] = "No attendance data to export.";
                return RedirectToAction("AttendanceReport");
                //return Json(new { success = false, message = "No Attendance data to export" });
            }


            var academicPeriod = await context.AcademicPeriods
                .FirstOrDefaultAsync(ap => ap.Id == SelectedAcademicPeriod.Value);


            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Attendance Report");
                //HEADER SECTION
                int currentRow = 1;

                //Title
                worksheet.Cells[currentRow, 1].Value = "ATTENDANCE REPORT";
                worksheet.Cells[currentRow, 1].Style.Font.Size = 16;
                worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                currentRow++;

                //Academic Period
                worksheet.Cells[currentRow, 1].Value = $"Academic Year: {academicPeriod?.Year} - {academicPeriod?.GradingPeriod} Grading";
                worksheet.Cells[currentRow, 1].Style.Font.Size = 12;
                currentRow++;

                worksheet.Cells[currentRow, 1].Value = $"Teacher Name: {firstName} {middleName} {lastName}";
                worksheet.Cells[currentRow, 1].Style.Font.Size = 12;
                currentRow++;


                //Class info
                var classInfo = $"Grade {selectedClass.SectionSubject.Section.Grade.GradeLevel} " +
                                $"{selectedClass.SectionSubject.Section.SectionName}" +
                                $"{selectedClass.SectionSubject.Section.Track}" +
                                $"{selectedClass.SectionSubject.Section.TVLProgram}" +
                                $"- {selectedClass.SectionSubject.Subject.SubjectDescription}";
                worksheet.Cells[currentRow, 1].Value = $"Class: {classInfo}";
                worksheet.Cells[currentRow, 1].Style.Font.Size = 11;
                currentRow++;

                // Date Range
                worksheet.Cells[currentRow, 1].Value = $"Date Range: {StartDate.Value:MMM dd, yyyy} - {EndDate.Value:MMM dd, yyyy}";
                worksheet.Cells[currentRow, 1].Style.Font.Size = 11;
                currentRow++;

                // Generated Date
                worksheet.Cells[currentRow, 1].Value = $"Generated: {DateTime.Now:MMM dd, yyyy hh:mm tt}";
                worksheet.Cells[currentRow, 1].Style.Font.Size = 10;
                worksheet.Cells[currentRow, 1].Style.Font.Italic = true;
                //currentRow += 2; // spacing
                currentRow++;
                //TABLE HEADER
                int col = 1;
                //int currentRow = 7;

                worksheet.Cells[currentRow, col].Value = "Student Name";
                worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                worksheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(86, 143, 135));
                worksheet.Cells[currentRow, col].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[currentRow, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                col++;

                //Date Columns
                foreach (var date in dateRange)
                {
                    worksheet.Cells[currentRow, col].Value = $"{date:ddd}\n{date:MMM d}";
                    worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(86, 143, 135));
                    worksheet.Cells[currentRow, col].Style.Font.Color.SetColor(System.Drawing.Color.White);
                    worksheet.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[currentRow, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[currentRow, col].Style.WrapText = true;
                    col++;
                }

                //Stats header columns (PRESENT, LATE, ETC)
                var statHeaders = new[]
                {
                    ("Present", System.Drawing.Color.FromArgb(40, 167, 69)),    // Green
                    ("Late", System.Drawing.Color.FromArgb(255, 193, 7)),       // Yellow
                    ("Absent", System.Drawing.Color.FromArgb(220, 53, 69)),     // Red
                    ("Cutting", System.Drawing.Color.FromArgb(23, 162, 184)),   // Cyan
                    ("Excuse", System.Drawing.Color.FromArgb(0, 123, 255))
                };

                foreach(var (header, color) in statHeaders)
                {
                    worksheet.Cells[currentRow, col].Value = header;
                    worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(color);
                    worksheet.Cells[currentRow, col].Style.Font.Color.SetColor(System.Drawing.Color.White);
                    worksheet.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[currentRow, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    col++;
                }



                // Summary header
                worksheet.Cells[currentRow, col].Value = "Summary";
                worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                worksheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(86, 143, 135));
                worksheet.Cells[currentRow, col].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[currentRow, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                currentRow++;

                //DATA ROWS
                foreach(var student in studentAttendance)
                {
                    //int startRow = currentRow;
                    col = 1;

                    worksheet.Cells[currentRow, col].Value = student.StudentName;
                    worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                    col++;

                    foreach(var marking in student.DailyAttendance)
                    {
                        worksheet.Cells[currentRow, col].Value = marking;
                        worksheet.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        var cellColor = marking switch
                        {
                            "P" => System.Drawing.Color.FromArgb(40, 167, 69),    // Green
                            "L" => System.Drawing.Color.FromArgb(255, 193, 7),    // Yellow
                            "A" => System.Drawing.Color.FromArgb(220, 53, 69),    // Red
                            "C" => System.Drawing.Color.FromArgb(23, 162, 184),   // Cyan
                            "E" => System.Drawing.Color.FromArgb(0, 123, 255),    // Blue
                            _ => System.Drawing.Color.FromArgb(108, 117, 125)     // Gray
                        };

                        worksheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(cellColor);
                        worksheet.Cells[currentRow, col].Style.Font.Color.SetColor(System.Drawing.Color.White);
                        worksheet.Cells[currentRow, col].Style.Font.Bold = true;

                        col++;
                    }

                    //Calculate summary
                    var presentCount = student.DailyAttendance.Count(d => d == "P");
                    var lateCount = student.DailyAttendance.Count(d => d == "L");
                    var absentCount = student.DailyAttendance.Count(d => d == "A");
                    var cuttingCount = student.DailyAttendance.Count(d => d == "C");
                    var exuseCount = student.DailyAttendance.Count(d => d == "E");
                    var totalDays = student.DailyAttendance.Count(d => d != "-");
                    var rate = totalDays > 0
                        ? Math.Round(((double)(presentCount + lateCount * 0.5) / totalDays) * 100)
                        : 0;

                    //PRESENT COUNT 
                    worksheet.Cells[currentRow, col].Value = presentCount;
                    worksheet.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, col].Style.Font.Size = 11;
                    worksheet.Cells[currentRow, col].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(40, 167, 69));
                    col++;

                    // Late count
                    worksheet.Cells[currentRow, col].Value = lateCount;
                    worksheet.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, col].Style.Font.Size = 11;
                    worksheet.Cells[currentRow, col].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 193, 7)); // Yellow
                    col++;

                    // ABSENT COUNT
                    worksheet.Cells[currentRow, col].Value = absentCount;
                    worksheet.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, col].Style.Font.Size = 11;
                    worksheet.Cells[currentRow, col].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(220, 53, 69)); // Red
                    col++;

                    // CUTTING COUNT
                    worksheet.Cells[currentRow, col].Value = cuttingCount;
                    worksheet.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, col].Style.Font.Size = 11;
                    worksheet.Cells[currentRow, col].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(23, 162, 184)); // Cyan
                    col++;

                    // EXCUSE COUNT
                    worksheet.Cells[currentRow, col].Value = exuseCount;
                    worksheet.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, col].Style.Font.Size = 11;
                    worksheet.Cells[currentRow, col].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(0, 123, 255)); // Blue
                    col++;

                    // SUMMARY COUNT
                    //col = dateRange.Count + 2;
                    worksheet.Cells[currentRow, col].Value = $"{rate}%";
                    worksheet.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[currentRow, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, col].Style.Font.Size = 14;

                    // Color based on rate
                    var summaryColor = rate == 100 ? System.Drawing.Color.FromArgb(40, 167, 69) :
                                      rate >= 90 ? System.Drawing.Color.FromArgb(23, 162, 184) :
                                      rate >= 70 ? System.Drawing.Color.FromArgb(255, 193, 7) :
                                      System.Drawing.Color.FromArgb(220, 53, 69);

                    worksheet.Cells[currentRow, col].Style.Font.Color.SetColor(summaryColor);

                    currentRow++; // Move to next row

                }
                        
                //LEGEND
                currentRow += 2; //For student
                worksheet.Cells[currentRow, 1].Value = "Legend:";
                worksheet.Cells[currentRow, 1].Style.Font.Size = 16;
                worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                currentRow++;

                var legends = new[]
                {
                    ("P", "PRESENT", System.Drawing.Color.FromArgb(40, 167, 69)),      // Green
                    ("L", "LATE", System.Drawing.Color.FromArgb(255, 193, 7)),         // Yellow
                    ("A", "ABSENT", System.Drawing.Color.FromArgb(220, 53, 69)),       // Red
                    ("C", "CUTTING", System.Drawing.Color.FromArgb(23, 162, 184)),     // Cyan
                    ("E", "EXCUSE", System.Drawing.Color.FromArgb(0, 123, 255)),       // Blue
                    ("-", "NO DATA", System.Drawing.Color.FromArgb(108, 117, 125))     // Gray
                };
            

                foreach (var (code, description, color) in legends)
                {
                    worksheet.Cells[currentRow, 2].Value = code;
                    worksheet.Cells[currentRow, 2].Style.Font.Size = 14;
                    worksheet.Cells[currentRow, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 2].Style.Fill.BackgroundColor.SetColor(color);
                    worksheet.Cells[currentRow, 2].Style.Font.Color.SetColor(System.Drawing.Color.White);
                    worksheet.Cells[currentRow, 2].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    worksheet.Cells[currentRow, 1].Value = $"{description}";
                    worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 1].Style.Font.Size = 12;


                    currentRow++;
                }

                //Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                worksheet.Column(1).Width = 30;

                int lastcol = dateRange.Count + 7; //Student name + dates + summary
                int dataStartRow = 7; //Wheere data table starts
                int dataEndRow = 7 + studentAttendance.Count; //Last data row
                //int dataEndRow = currentRow - 1;

                var dataRange = worksheet.Cells[dataStartRow, 1, dataEndRow, lastcol];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                //GENERATE FILE
                var stream = new MemoryStream(package.GetAsByteArray());

                var fileName = $"Attendance_Report_{selectedClass.SectionSubject.Section.Grade.GradeLevel}" +
                                $"{selectedClass.SectionSubject.Section.SectionName}_" +
                                $"{StartDate.Value:yyyyMMdd}-{EndDate.Value:yyyyMMdd}.xlsx";

                return File(stream,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
            }
        }

        // Shows backup page with list of existing backups
        [HttpGet]
        public async Task<IActionResult> BackupAndRestore(PaginatedRequest request)
        {
            try
            {
                //Get list of all backups
                //var backups = backupService.GetAllBackups();
                var backups = await backupService.GetPaginated(
                        request.PageNumber, 
                        PaginatedRequest.ITEM_PER_PAGE,
                        request.SearchKeyword ??  string.Empty
                        );

                backups.SearchKeyword = request.SearchKeyword;
                //BAGO//////////////////////
                var recentForRestore = backupService.GetRecentBackups(5);

                var viewmodel = new BackupViewModel
                {
                    PaginatedBackups = backups,
                    RecentBackupsForRestore = recentForRestore,
                    SearchKeyword = backups.SearchKeyword
                };
                ///////////////////////////////////////
                return View(viewmodel);
            }catch(Exception ex)
            {
                logger.LogError(ex, "Failed to load backup page");
                TempData["ErrorMessage"] = "Failed to load backups. Please try again.";
                //Json(new { success = false, message = "Failed to load backups. Please try again." });
                return View(new List<BackupFileInfo>());
            }
        }

        //Creats a new backup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                string backupFileName = await backupService.CreateBackupAsync();

                var userInfo = await GetCurrentUserInfo();

                await logService.LogActivity(
                    actionType: "Backup",
                    entityName: "Backup",
                    entityId: backupFileName,
                    userId: userInfo.userId,
                    schoolId: userInfo.schoolId,
                    details: $"User {userInfo.username} created a backup file",
                    username: userInfo.username
                );

                logger.LogInformation("Backup created: {FileName}", backupFileName);
                TempData["successMessage"] = "Backed up created Successfully!";
                //return Json(new { success = true, message = $"Backup created successfully! File: {backupFileName}" });
                //logger.LogInformation("Backup created: {FileName}", backupFileName);
            }catch(Exception ex)
            {
                logger.LogError(ex, "Backup Creation failed");
                TempData["ErrorMessage"] = "Failed to create backup. Please check server logs.";

                //return Json(new { success = false, message = "Failed to create backup. Please check server logs." });
            }
            return RedirectToAction(nameof(BackupAndRestore)); ////Use only if Tempdata is used
        }

        //Downloads a backup file
        [HttpGet]
        public IActionResult DownloadBackup(string filename)
        {
            try
            {
                //Validate filename first
                if (string.IsNullOrEmpty(filename))
                {
                    return BadRequest("FileName is Required!");
                    //return Json(new { success = false, message = "FileName is Required!" });
                }

                //Get full file path(validation happens inside service)
                string filePath = backupService.GetBackupFilePath(filename);

                //check if file exists
                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "Backup file not found";
                    return RedirectToAction(nameof(BackupAndRestore));
                    //return Json(new { success = false, message = "Backup file not found" });
                }

                //Read file bytes
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

                //Return file to user's browser (triggers download)
                return File(fileBytes, "application/octet-stream", filename); //application/octet-stream save the data to a file



            }
            catch(ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid filename attempt: {FileName}", filename);
                return BadRequest("Invalid filename");
                //return Json(new { success = false, message = "Invalid filename" });
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Download failed for: {Filename}", filename);
                TempData["ErrorMessage"] = "Failed to download backup";
                return RedirectToAction(nameof(BackupAndRestore));
            }
        }

        //Restore Database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreDatabase(string backupFileName)
        {
            try
            {
                if (string.IsNullOrEmpty(backupFileName))
                {
                    TempData["ErrorMEssage"] = "Please select a backup file to restore";
                    return RedirectToAction(nameof(BackupAndRestore));

                }
                //return warning about user. If it is null it will return Unknown
                logger.LogWarning(
                    "RESTORE INITIATED by user: {User}, Backup: {Backup}",
                    //?. means, null-conditaional operator means User.Identity is not null, it returns the value of `Name`
                                        //?? means, it checks the left side(User.Identity) is null. it returns "Unknown"
                    User.Identity?.Name ?? "Unknown", 
                    backupFileName
                );

                var result = await backupService.RestoreDatabaseAsync(backupFileName);

                var userInfo = await GetCurrentUserInfo();

                await logService.LogActivity(
                    actionType: "Restore",
                    entityName: "Restore",
                    entityId: backupFileName,
                    userId: userInfo.userId,
                    schoolId: userInfo.schoolId,
                    details: $"User {userInfo.username} restored a backup file",
                    username: userInfo.username
                );


                //Success message
                //TempData["SuccessMessage"] = $@"
                //    Database restored Sucessfully!

                //    Restored From: {result.RestoredFrom}
                //    Safely backup created: {result.SafetyBackupCreated}

                //    All data hase been restored to the state from the selected backup.
                //";

                TempData["SuccessMessage"] = "Database restored Sucessfully!";

                logger.LogWarning(
                    "RESTORED COMPLETED - User: {User}, From: {Backup}, Safety: {Safety}",
                    User.Identity?.Name ?? "Unknown", 
                    result.RestoredFrom,
                    result.SafetyBackupCreated
                );
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "RESTORE FAILED - User: {User}, Backup: {Backup}",
                User.Identity?.Name ?? "Unknown",
                backupFileName);

                TempData["ErrorMessage"] = $"Failed to restore DataBase: {ex.Message}";
            }

            return RedirectToAction(nameof(BackupAndRestore));
        }

        [HttpGet]
        public async Task<IActionResult> ActivityLogs(PaginatedRequest request)
        {
            

            var activityLogs = await _repo.GetPaginated(
                    request.PageNumber, 
                    PaginatedRequest.ITEM_PER_PAGE,
                    request.SearchKeyword ?? string.Empty
                    );
            activityLogs.SearchKeyword = request.SearchKeyword;

            return View(activityLogs);
        }

        // Shows backup page with list of existing backups
        //[HttpGet]
        //public IActionResult BackupAndRestore()
        //{
        //    try
        //    {
        //        //Get list of all backups
        //        var backups = backupService.GetAllBackups();

        //        var recentForRestore = backupService.GetRecentBackups(5);

        //        var model = new BackupViewModel
        //        {
        //            BackupFiles = backups,
        //            RecentBackupsForRestore = recentForRestore
        //        };

        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, "Failed to load backup page");
        //        TempData["ErrorMessage"] = "Failed to load backups. Please try again.";
        //        //Json(new { success = false, message = "Failed to load backups. Please try again." });
        //        return View(new List<BackupFileInfo>());
        //    }
        //}

        ////Creats a new backup
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> CreateBackup()
        //{
        //    try
        //    {
        //        string backupFileName = await backupService.CreateBackupAsync();

        //        var userInfo = await GetCurrentUserInfo();

        //        await logService.LogActivity(
        //            actionType: "Backup",
        //            entityName: "Backup",
        //            entityId: backupFileName,
        //            userId: userInfo.userId,
        //            schoolId: userInfo.schoolId,
        //            details: $"User {userInfo.username} created a backup file",
        //            username: userInfo.username
        //        );

        //        logger.LogInformation("Backup created: {FileName}", backupFileName);
        //        TempData["successMessage"] = "Backed up created Successfully!"; ;
        //        //return Json(new { success = true, message = $"Backup created successfully! File: {backupFileName}" });
        //        //logger.LogInformation("Backup created: {FileName}", backupFileName);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, "Backup Creation failed");
        //        //TempData["ErrorMessage"] = "Failed to create backup. Please check server logs.";

        //        //return Json(new { success = false, message = "Failed to create backup. Please check server logs." });
        //    }
        //    return RedirectToAction(nameof(BackupAndRestore)); ////Use only if Tempdata is used
        //}

        ////Downloads a backup file
        //[HttpGet]
        //public async Task<IActionResult> DownloadBackup(string filename)
        //{
        //    try
        //    {
        //        //Validate filename first
        //        if (string.IsNullOrEmpty(filename))
        //        {
        //            return BadRequest("FileName is Required!");
        //            //return Json(new { success = false, message = "FileName is Required!" });
        //        }

        //        //Get full file path(validation happens inside service)
        //        string filePath = backupService.GetBackupFilePath(filename);

        //        //check if file exists
        //        if (!System.IO.File.Exists(filePath))
        //        {
        //            TempData["ErrorMessage"] = "Backup file not found";
        //            return RedirectToAction(nameof(BackupAndRestore));
        //            //return Json(new { success = false, message = "Backup file not found" });
        //        }

        //        //Read file bytes
        //        byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

        //        var userInfo = await GetCurrentUserInfo();

        //        await logService.LogActivity(
        //            actionType: "Download Backup",
        //            entityName: "Backup",
        //            entityId: filePath,
        //            userId: userInfo.userId,
        //            schoolId: userInfo.schoolId,
        //            details: $"Admin {userInfo.username} download a backup file",
        //            username: userInfo.username
        //        );

        //        logger.LogInformation("Downloaded backup: {FileName}", filePath);
        //        TempData["successMessage"] = "Downloaded backup Successfully!"; ;
        //        //Return file to user's browser (triggers download)
        //        return File(fileBytes, "application/octet-stream", filename); //application/octet-stream save the data to a file

        //    }
        //    catch (ArgumentException ex)
        //    {
        //        logger.LogWarning(ex, "Invalid filename attempt: {FileName}", filename);
        //        return BadRequest("Invalid filename");
        //        //return Json(new { success = false, message = "Invalid filename" });
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, "Download failed for: {Filename}", filename);
        //        TempData["ErrorMessage"] = "Failed to download backup";
        //        return RedirectToAction(nameof(BackupAndRestore));
        //    }
        //}

        ////Restore Database
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> RestoreDatabase(string backupFileName)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(backupFileName))
        //        {
        //            TempData["ErrorMEssage"] = "Please select a backup file to restore";
        //            return RedirectToAction(nameof(BackupAndRestore));

        //        }
        //        //return warning about user. If it is null it will return Unknown
        //        logger.LogWarning(
        //            "RESTORE INITIATED by user: {User}, Backup: {Backup}",
        //            //?. means, null-conditaional operator means User.Identity is not null, it returns the value of `Name`
        //            //?? means, it checks the left side(User.Identity) is null. it returns "Unknown"
        //            User.Identity?.Name ?? "Unknown",
        //            backupFileName
        //        );

        //        var result = await backupService.RestoreDatabaseAsync(backupFileName);

        //        var userInfo = await GetCurrentUserInfo();

        //        await logService.LogActivity(
        //            actionType: "Restore",
        //            entityName: "Restore",
        //            entityId: backupFileName,
        //            userId: userInfo.userId,
        //            schoolId: userInfo.schoolId,
        //            details: $"User {userInfo.username} restored a backup file",
        //            username: userInfo.username
        //        );

        //        //Success message
        //        TempData["SuccessMessage"] = $@"
        //            Database restored Sucessfully!

        //            Restored From: {result.RestoredFrom}
        //            Safely backup created: {result.SafetyBackupCreated}

        //            All data hase been restored to the state from the selected backup.
        //        ";

        //        logger.LogWarning(
        //            "RESTORED COMPLETED - User: {User}, From: {Backup}, Safety: {Safety}",
        //            User.Identity?.Name ?? "Unknown",
        //            result.RestoredFrom,
        //            result.SafetyBackupCreated
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, "RESTORE FAILED - User: {User}, Backup: {Backup}",
        //        User.Identity?.Name ?? "Unknown",
        //        backupFileName);

        //        TempData["ErrorMessage"] = $"Failed to restore DataBase: {ex.Message}";
        //    }

        //    return RedirectToAction(nameof(BackupAndRestore));
        //}
        public async Task<IActionResult> Logout()
        {
            //Get the current user
            //var user = await userManager.GetUserAsync(User);

            //var userId = user?.Id;
            //var schoolId = user?.SchoolId ?? 0;
            //var username = user?.UserName;

            var userinfo = await GetCurrentUserInfo();
            await signInManager.SignOutAsync();

            await logService.LogActivity(
                actionType: "Logout",
                entityName: "User",
                entityId: userinfo.userId,
                userId: userinfo.userId,
                schoolId: userinfo.schoolId,
                details: $"User with the Id of {userinfo.schoolId}, logged out successfully!",
                username: userinfo.username
            );

            return RedirectToAction("Login", "Login");

        }
    }

    
}
