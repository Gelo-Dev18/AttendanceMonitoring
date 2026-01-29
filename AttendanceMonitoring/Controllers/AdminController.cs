using AttendanceMonitoring.Contracts;
using AttendanceMonitoring.Data;
using AttendanceMonitoring.Helper;
using AttendanceMonitoring.Models;
using AttendanceMonitoring.Services;
using AttendanceMonitoring.ViewModel;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering; // para sa SelectListItem
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using NuGet.DependencyResolver;
using NuGet.Packaging.Signing;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
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

        protected async Task<int> GetCurrentUserSchoolId()
        {
            var userId = GetCurrentUserId();

            var user = await userManager.FindByIdAsync(userId);

            return user.SchoolId;
        }

        protected async Task<(string userId, string username, int schoolId)> GetCurrentUserInfo()
        {
            var userId = GetCurrentUserId();

            var user = await userManager.FindByIdAsync(userId);

            return (userId, user.UserName, user.SchoolId);
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


                editedUser.LRN = model.LRN;
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

            var hasRecord = await context.AcademicPeriods.AnyAsync(ap => ap.Id == id);

            if (hasRecord)
            {
                return Json(new { success = false, message = "Cannot Delete Academic year when has already a record!" });
            }
            context.AcademicPeriods.Remove(AcademicId);
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
            var DeleteSubject = await context.Subjects.FindAsync(id);

            if (DeleteSubject == null)
            {
                return Json(new { success = false, error = "Subject Not Found!" });
            }

            var isAssigned = await context.SectionSubjects.AnyAsync(s => s.SubjectId == id);

            if (isAssigned)
            {
                return Json(new { success = false, message = "Cannot delete Subject when already Assigned!" });

            }

            context.Subjects.Remove(DeleteSubject);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Edit",
                entityName: "Subject",
                entityId: DeleteSubject.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} deleted subject {DeleteSubject.SubjectDescription}, {DeleteSubject.Category} Category",
                username: userInfo.username
            );

            return Json(new { success = true, message = "Subject Deleted Successfully!" });
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
            bool gradeExisted = await context.Grades.AnyAsync(g => g.GradeLevel == model.GradeLevel);

            if (gradeExisted)
            {
                ModelState.AddModelError("GradeLevel", "Grade Level is already Existed!");
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

            if(grade == null)
            {
                return Json(new { success = false, error = "Grade Level does not found" });
            }

            var hasSection = await context.Sections.AnyAsync(s => s.GradesId == id);

            if (hasSection)
            {
                return Json(new { success = false, message = "Cannot delete Grade when contain sections" });
            }
            context.Grades.Remove(grade);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Deleted",
                entityName: "Grade",
                entityId: grade.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} deleted grade {grade.GradeLevel}",
                username: userInfo.username
            );


            return Json(new { success = true, message = "Grade Successfully Deleted!" });

        }
        public async Task<IActionResult> SectionList()
        {
            var sectionList = await context.Sections
                .Include(g => g.Grade) // dahil sa Nav.Property/Lazy loading na nakadeclare sa Section.cs kaya gumana ang .Include
                .OrderBy(s => s.GradesId)
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
            var Section = await context.Sections.FindAsync(id);
            var grade = await context.Grades.FindAsync(Section.GradesId);

            var trackInfo = !string.IsNullOrEmpty(Section.Track) ? $" - {Section.Track}" : "";
            var tvlInfo = !string.IsNullOrEmpty(Section.TVLProgram) ? $" ({Section.TVLProgram})" : "";



            if (Section == null)
            {
                return Json(new { success = false, error = "Section does not exist!" });
            }

            //var hasStudentAssigned = await context.StudentSectionAssignments.AnyAsync(ssa => ssa.SectionId == id);
            var hasStudentAssigned = await context.SectionSubjects.AnyAsync(ss => ss.SectionId == id);
            if (hasStudentAssigned)
            {
                return Json(new { success = false, message = "Cannot delete section if class already have a subject" });

            }

            context.Sections.Remove(Section);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Deleted",
                entityName: "Section",
                entityId: Section.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} deleted section '{Section.SectionName}' of Grade {grade.GradeLevel} {trackInfo} {tvlInfo}",
                username: userInfo.username
            );

            return Json(new { success = true, message = "Section Successfully deleted!" });
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

            return View(teacher);// return view dahil full page ang nirereload
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
                .OrderBy(s => s.Id)
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
                Email = teacher.Email, //from entity
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
        public async Task<IActionResult> ViewTeacher(string id)
        {
            var teacher = await context.Users.FindAsync(id);

            if (teacher == null)
            {
                return RedirectToAction("TeacherList", "Admin");
            }

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
                                    .Where(ta => ta.TeacherId == id)
                                    .OrderBy(ta => ta.SectionSubject.SectionId)
                                    .ToListAsync();

            //Manual mapping
            var model = new ViewTeacherViewModel()
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

                teacherAssignments = teacherAssignment
            };

            ViewData["imageFileData"] = teacher.imageFileData;
            //ViewData["imageFilePath"] = teacher.imageFilePath;
            ViewData["CreatedAt"] = teacher.CreatedAt.ToString("MM/dd/yyyy");

            return PartialView("_ViewTeacherPartial", model);
        }

        public IActionResult AddTeacher()
        {
            return PartialView("_AddTeacherPartial");
        }

        [HttpGet]
        public async Task<IActionResult> AddSecretary()
        {
            var availableGradeSection = await context.Sections
                                                     .Include(g => g.Grade)
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
                .Take(10)
                .ToList()
            };

            return PartialView("_AddSecretaryPartial", model);
        }

        [HttpPost] //ViewModel → Entity (for saving to database)
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

            bool employeeIdExisted = await context.Users.AnyAsync(e => e.EmployeeId == model.EmployeeId);

            if (employeeIdExisted)
            {
                ModelState.AddModelError("EmployeeId", "Employee Id is already taken!");
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
                //To Capitalize every first letter of word when inserting data
                TextInfo textinfo = CultureInfo.CurrentCulture.TextInfo;

                string formattedFirstName = textinfo.ToTitleCase(model.FirstName.ToLower());
                string formattedMiddleName = textinfo.ToTitleCase(model.MiddleName?.ToLower() ?? "");
                string formattedLastName = textinfo.ToTitleCase(model.LastName.ToLower());

                //Map from viewmodel to entity
                AppUser teacher = new AppUser()
                {
                    Email = model.Email,
                    UserName = model.Email,
                    SchoolId = model.SchoolId,
                    EmployeeId = model.EmployeeId,
                    FirstName = formattedFirstName,
                    MiddleName = formattedMiddleName,
                    LastName = formattedLastName,
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

                    var userInfo = await GetCurrentUserInfo();

                    await logService.LogActivity(
                        actionType: "Add",
                        entityName: "User",
                        entityId: teacher.Id.ToString(),
                        userId: userInfo.userId,
                        schoolId: userInfo.schoolId,
                        details: $"User {userInfo.username} added new teacher : {teacher.FirstName} {teacher.MiddleName} {teacher.LastName}, LRN : {teacher.SchoolId}",
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
            editTeacher.Email = model.Email; //From ViewModel To Entity
            editTeacher.SchoolId = model.SchoolId;
            editTeacher.EmployeeId = model.EmployeeId;
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
                details: $"User {userInfo.username} edited teacher {editTeacher.FirstName} {editTeacher.MiddleName} {editTeacher.LastName}, School Id: {editTeacher.SchoolId}",
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
            
            var teacher = await context.Users.FindAsync(id);

            if(teacher == null)
            {
                //return RedirectToAction("TeacherList", "Admin");
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

            context.Users.Remove(teacher);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Delete",
                entityName: "User",
                entityId: teacher.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} deleted teacher {teacher.FirstName} {teacher.MiddleName} {teacher.LastName}, School Id: {teacher.SchoolId}",
                username: userInfo.username
            );

            //return RedirectToAction("TeacherList", "Admin");
            return Json(new { success = true, message = "Teacher has been Deleted successfully" }); //JSON store and transport data from server side to client side

        }

        [HttpGet]
        public async Task<IActionResult> AssignTeacher(string teacherId)
        {           
            //This excluded the assigned sectonsubject to a teacher by an specific section only
            var assignedToTeacher = await context.TeacherAssignments
                                    .Where(t => t.TeacherId == teacherId)
                                    .Select(ss => ss.SectionSubjectId)
                                    .ToListAsync();

            var assignedSubject = await context.TeacherAssignments
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
            

            if (teacher == null)
            {
                return Json(new { success = false, message = "Teacher Id Not Found!" });
            }

            var assigned = new TeacherAssignment()
            {
                TeacherId = teacherId,
                SectionSubjectId = sectionSubjectId,
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
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Section)
                    .ThenInclude(s => s.Grade)
                .FirstOrDefaultAsync(ta => ta.Id == id);

            if (teacherAssigned == null)
            {
                return Json(new { success = true, error = "Id not Found!" });
            }
            var teacherId = teacherAssigned.TeacherId;

            context.TeacherAssignments.Remove(teacherAssigned);
            await context.SaveChangesAsync();

            var teacher = await context.Users.FindAsync(teacherId);

            var gradeInfo = $"Grade {teacherAssigned.SectionSubject.Section.Grade.GradeLevel}";
            var sectionInfo = $"{teacherAssigned.SectionSubject.Section.SectionName}";
            var trackInfo = !string.IsNullOrEmpty(teacherAssigned.SectionSubject.Section.Track) ? $" - {teacherAssigned.SectionSubject.Section.Track}" : "";
            var tvlInfo = !string.IsNullOrEmpty(teacherAssigned.SectionSubject.Section.TVLProgram) ? $" ({teacherAssigned.SectionSubject.Section.TVLProgram})" : "";

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Assign Teacher",
                entityName: "TeacherAssignment",
                entityId: teacher.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} remove assignment {gradeInfo} - {sectionInfo} {trackInfo} {tvlInfo} for Teacher: {teacher.FirstName} {teacher.MiddleName} {teacher.LastName} School Id: {teacher.SchoolId}",
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

                teacherAssignments = remainingAssignments
            };

            ViewData["imageFileData"] = teacher.imageFileData;
            //ViewData["imageFilePath"] = teacher.imageFilePath;
            ViewData["CreatedAt"] = teacher.CreatedAt.ToString("MM/dd/yyyy");

            return PartialView("_ViewTeacherPartial", model);
        }

        public async Task<IActionResult> StudentList()
        {
            var Students = await context.Students   
                .Include(sa => sa.SectionAssignments)
                    .ThenInclude(sn => sn.Section)
                        .ThenInclude(g => g.Grade)
                .OrderBy(s => s.Id)
                .ToListAsync();

            return View(Students);
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
            bool studentFirstLastNameExist = await context.Students.AnyAsync(t => t.FirstName == model.FirstName && t.MiddelName == model.MiddelName && t.LastName == model.LastName);

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


            var sectionAssignment = new StudentSectionAssignment
            {
                StudentId = student.Id,
                SectionId = model.SectionId,
                CreatedAt = DateTime.Now
            };

            context.StudentSectionAssignments.Add(sectionAssignment);
            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Student Added Successfully" });
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
            var editStudent = await context.Students.FindAsync(id);

            if(editStudent == null)
            {
                return Json(new { success = false, message = "Student id does not found" });

            }

            var studentGradeSectionAssigned = await context.StudentSectionAssignments.FindAsync(id);
            if (studentGradeSectionAssigned == null)
            {
                return Json(new { sucess = false, message = "Student assignment Id does not found" });

            }

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

            studentGradeSectionAssigned.StudentId = editStudent.Id;
            studentGradeSectionAssigned.SectionId = model.SectionId;

            context.StudentSectionAssignments.Update(studentGradeSectionAssigned);
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
            var Student = await context.Students.FindAsync(id);


            if (Student == null)
            {
                return Json(new { success = false, error = "Student does not exist!" });
            }

            context.Students.Remove(Student);
            await context.SaveChangesAsync();

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Delete",
                entityName: "Student",
                entityId: Student.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.username} deleted student {Student.FirstName} {Student.MiddelName} {Student.LastName}. LRN: {Student.LRN}",
                username: userInfo.username
            );

            return Json(new { success = true, message = "Student Successfully deleted!" });
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

            bool fullNameExisted = await context.Users.AnyAsync(f => f.FirstName == model.FirstName && f.MiddleName == model.MiddleName && f.LastName == model.LastName);
            if (fullNameExisted)
            {
                ModelState.AddModelError("FirstName", "A secretary with this Full name is already existed");
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
                Email = model.Email,
                UserName = model.Email,
                SchoolId = model.SchoolId,
                FirstName = formattedFirstName,
                MiddleName = formattedMiddleName,
                LastName = formattedLastName,
                Sex = model.Sex,
                imageFileData = saveImageData,
                imageFilePath = saveImagePath,
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(secretary, model.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(secretary, "Secretary");

                var secretaryAssignment = new SecretaryAssignment
                {
                    SecretaryId = secretary.Id,
                    SectionId = model.SectionId,
                    CreatedAt = DateTime.Now
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
                    details: $"User {userInfo.username} Added secretary {secretary.FirstName} {secretary.MiddleName} {secretary.LastName}. LRN: {secretary.SchoolId}",
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
                Email = secretary.Email,
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

            //Retrieve all available Sections
            var allSection = await context.Sections
                    .Include(g => g.Grade)
                .Select(ags => new { ags.Id, ags.Grade.GradeLevel, ags.SectionName, ags.Track, ags.TVLProgram })
                .ToListAsync();

            //Retrieve current student's assigned Grade and Section  to secretary
            var studentsGradeSection = await context.SecretaryAssignments
                .Where(si => si.SecretaryId == id)
                .Select(s => s.SectionId)
                .FirstOrDefaultAsync();

            var model = new EditSecretaryViewModel()
            {
                AvailableGradeSection = allSection.Select(gs => new SelectListItem
                {
                    Value = gs.Id.ToString(),
                    Text = $"Grade {gs.GradeLevel} {gs.SectionName} {gs.Track} {gs.TVLProgram}",
                }).ToList(),

                SectionId = studentsGradeSection,
                Email = secretary.Email,
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

            editSecretary.Email = model.Email;
            editSecretary.SchoolId = model.SchoolId;
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

            context.SecretaryAssignments.Update(secretaryAssigned);
            await context.SaveChangesAsync();

            // No need ng gamitin ang SaveChangesAsync() kase Ang UserManager.UpdateAsync(), RemovePasswordAsync(), at AddPasswordAsync() ay automatically nag - save na sa database.
            //await context.SaveChangesAsync();
            return Json(new { success = true, message = "Secretary Updated Successfully" });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSecretary(string id)
        {
            var secretary = await userManager.FindByIdAsync(id);

            if (secretary == null)
            {
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

            context.Users.Remove(secretary);
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

            return Json(new { success = true, message = "Secretary Deleted Successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> AttendanceReport(string? SelectedTeacher,
                                                            int? SelectedAcademicPeriod,
                                                            int? SelectedTeacherAssignment, //selected  Class
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
                                    .OrderBy(ap => ap.Year)
                                    .ToListAsync();
            //Get all teacher list
            var allTeacher = await userManager.GetUsersInRoleAsync("Teacher");

            //Check if teacher is selected
            List<SelectListItem> teacherClass = new List<SelectListItem>();

            if (!string.IsNullOrEmpty(SelectedTeacher))
            {
                teacherClass = await context.TeacherAssignments
                                .Include(ta => ta.SectionSubject)
                                    .ThenInclude(ss => ss.Subject)
                                .Include(sn => sn.SectionSubject.Section)
                                    .ThenInclude(g => g.Grade)
                                .Where(s => s.TeacherId == SelectedTeacher)
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
                                .Include(ta => ta.SectionSubject)
                                    .ThenInclude(ss => ss.Subject)
                                .Include(sn => sn.SectionSubject.Section)
                                    .ThenInclude(g => g.Grade)
                                .Where(s => s.TeacherId == SelectedTeacher)
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
                                    .Include(ssa => ssa.Student)
                                    .Where(ssa => ssa.SectionId == sectionId)
                                    .OrderBy(ssa => ssa.Student.LastName)
                                    .ToListAsync();

                    var attendanceRecord = await context.Attendances
                                            .Where(a => a.SectionSubjectId == sectionSubjectId
                                                    && a.AttendanceDate.Date >= StartDate.Value.Date
                                                    && a.AttendanceDate.Date <= EndDate.Value.Date
                                                    && a.AcademicPeriod.Id == SelectedAcademicPeriod.Value)
                                            .ToListAsync();
                    //Builder Report Data
                    foreach(var student in students)
                    {
                        var studentData = new AdminAttendanceReportData
                        {
                            StudentId = student.StudentId,
                            StudentName = $"{student.Student.FirstName} {student.Student.MiddelName} {student.Student.LastName}",
                            DailyAttendance = new List<string>()
                        };

                        foreach(var date in dateRange)
                        {
                            var attendance = attendanceRecord
                                .FirstOrDefault(ar => ar.StudentId == student.StudentId
                                                && ar.AttendanceDate.Date == date.Date);

                            if(attendance != null)
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
                teacherList = allTeacher.Select(at => new SelectListItem
                {
                    Value = at.Id.ToString(),
                    Text = $"{at.FirstName} {at.MiddleName} {at.LastName} - {at.positionTitle}",

                }).ToList(),

                teacherClass = teacherClass,    

                academicPeriod = allAcademicPeriod.Select(aap => new SelectListItem
                {
                    Value = aap.Id.ToString(),
                    Text = $"{aap.Year} - {aap.GradingPeriod} Grading " + (aap.IsDefault == 1 ? "✓ Active" : ""),
                }).ToList(),

                SelectedAcademicPeriod = SelectedAcademicPeriod,
                SelectedTeacher = SelectedTeacher,
                SelectedTeacherAssignment = SelectedTeacherAssignment,
                StudentAttendance = studentAttendance,
                DateRange = dateRange,
                StartDate = StartDate,
                EndDate = EndDate
            };

            return View(model);
        }
        //FOr ajax for teacherclass dropdown when selecting specific teacher
        [HttpGet]
        public async Task<JsonResult> GetTeacherAssignments(string teacherId)
        {
            var teacherClass = await context.TeacherAssignments
                                .Include(ta => ta.SectionSubject)
                                    .ThenInclude(ss => ss.Subject)
                                .Include(sn => sn.SectionSubject.Section)
                                    .ThenInclude(g => g.Grade)
                                .Where(s => s.TeacherId == teacherId)
                                .OrderBy(s => s.SectionSubject.Section.Grade.GradeLevel)
                                .Select(tc => new
                                {
                                    Value = tc.Id.ToString(),
                                    Text = $"Grade {tc.SectionSubject.Section.Grade.GradeLevel} {tc.SectionSubject.Section.SectionName} {tc.SectionSubject.Section.Track} {tc.SectionSubject.Section.TVLProgram} {tc.SectionSubject.Subject.SubjectDescription}",
                                })
                                .ToListAsync();

            return Json(teacherClass);

        }

        /// <summary>
        /// BACKUP AND RESTORE FEAUTRE
        /// </summary>
        /// <returns></returns>

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
                //return Json(new { success = true, message = $"Backup created successfully! File: {backupFileName}" });
                //logger.LogInformation("Backup created: {FileName}", backupFileName);
            }catch(Exception ex)
            {
                logger.LogError(ex, "Backup Creation failed");
                //TempData["ErrorMessage"] = "Failed to create backup. Please check server logs.";

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
                TempData["SuccessMessage"] = $@"
                    Database restored Sucessfully!

                    Restored From: {result.RestoredFrom}
                    Safely backup created: {result.SafetyBackupCreated}

                    All data hase been restored to the state from the selected backup.
                ";

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
                details: $"User {userinfo.username} logged out successfully!",
                username: userinfo.username
            );

            return RedirectToAction("Login", "Login");

        }
    }

    
}
