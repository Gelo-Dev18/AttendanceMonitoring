using AttendanceMonitoring.Data;
using AttendanceMonitoring.Models;
using AttendanceMonitoring.Services;
using AttendanceMonitoring.ViewModel;
using AttendanceMonitoring.ViewModel.Teacher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace AttendanceMonitoring.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // disabled caching para kapag pinindot back button sa isang browser at naka logged out na eh hindi na babalik sa specific user dashboard
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        public readonly SignInManager<AppUser> signInManager;
        private readonly UserManager<AppUser> userManager;
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly IActivityLogService logService;

        public TeacherController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment, IActivityLogService logService)
        {

            this.signInManager = signInManager;
            this.userManager = userManager;
            this.context = context;
            this.environment = environment;
            this.logService = logService;

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

        [HttpGet]
        public async Task<IActionResult> ManageTeacherAccount()
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return Json(new { success = false, message = "Cannot find user" });
            }

            var model = new TeacherManageAccountViewModel()
            {
                LRN = user.SchoolId,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Sex = user.Sex,
                PositionTitle = user.positionTitle,
                imageFilePath = user.imageFilePath

            };
            return PartialView("_TeacherManageAccount", model);
        }

        [HttpPost] //Route Parameter
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageTeacherAccount(string id, TeacherManageAccountViewModel model)
        {
            var editTeacher = await context.Users.FindAsync(id);

            if (editTeacher == null)
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

                    if (!string.IsNullOrEmpty(editTeacher.imageFilePath))
                    {
                        string oldImageFullPath = environment.WebRootPath + "/ProfilePic" + editTeacher.imageFilePath;

                        if (oldImageFullPath != null)
                        {
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

                    editTeacher.imageFilePath = saveImagePath;
                    editTeacher.imageFileData = saveImageData;
                }

                //Capitalize every firsy letter
                TextInfo textinfo = CultureInfo.CurrentCulture.TextInfo;

                string formattedFirstName = textinfo.ToTitleCase(model.FirstName.ToLower());
                string formattedMiddleName = textinfo.ToTitleCase(model.MiddleName?.ToLower() ?? ""); //ang .ToTitleCase is hindi tumatanggap ng null kaya need ng MiddleName? at ?? ""
                string formattedLastName = textinfo.ToTitleCase(model.LastName.ToLower());


                editTeacher.LRN = model.LRN;
                editTeacher.FirstName = formattedFirstName;
                editTeacher.MiddleName = formattedMiddleName;
                editTeacher.LastName = formattedLastName;
                editTeacher.Sex = model.Sex;
                editTeacher.positionTitle = model.PositionTitle;

                var result = await userManager.UpdateAsync(editTeacher);

                //Log Activity
                var userInfo = await GetCurrentUserInfo();

                await logService.LogActivity(
                    actionType: "Manage Account",
                    entityName: "Teacher",
                    entityId: editTeacher.Id.ToString(),
                    userId: userInfo.userId,
                    schoolId: userInfo.schoolId,
                    details: $"User {userInfo.username} edited admin user {editTeacher.FirstName} {editTeacher.MiddleName} {editTeacher.LastName}, School Id: {editTeacher.SchoolId}",
                    username: userInfo.username
                );

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
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
                    var removePassword = await userManager.RemovePasswordAsync(editTeacher);
                    if (removePassword.Succeeded)
                    {
                        var addPassword = await userManager.AddPasswordAsync(editTeacher, model.NewPassword);
                        if (!addPassword.Succeeded)
                        {
                            foreach (var error in addPassword.Errors)
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

                return Json(new { success = true, message = "Account Updated Successfully!" });

            }
            catch
            {
                return Json(new { success = false, message = "Something went wrong" });
            }

        }
        //[Authorize(Roles = "Teacher")]
        public IActionResult TeacherHome()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> _Attendance(int? selectedClassId)  // WALANG parameter for Id of teacher kase gumamit na ng ClaimTypes
        {
            // Kunin yung ID ng naka-login na teacher
            //Equivalent nito sa PHP is eto:
            //$user_id = $_SESSION['login_id'];
            //$username = $_SESSION['login_name'];
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check kung naka-login ba
            if (string.IsNullOrEmpty(teacherId))
            {
                return RedirectToAction("TeacherHome", "Teacher");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }
            //Check if academic status is not yet started or closed
            //var AcademicStatus = await context.AcademicPeriods.FirstOrDefaultAsync(ap => ap.Status == 1);

            var today = DateTime.Today;

            //Get Current default academic period
            var currentAcademicPeriod = await context.AcademicPeriods
                .FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            var hasAnyAssignments = await context.TeacherAssignments
                         .AnyAsync(ta => ta.TeacherId == teacherId);

            //Exclude student that already has an attendance record
            var alreadyRecordedAttendance = await context.Attendances
                                            .Where(a => a.RecordedById == teacherId && a.AcademicPeriod == currentAcademicPeriod && a.AttendanceDate.Date == today)
                                            .Select(ta => ta.SectionSubjectId) //Use SectionSubjectId not TeacherAssignmentId
                                            .ToListAsync();

            //Query to fetch assign Grade & Section - Subjects on a specific teacher
            var TeachersClass = await context.TeacherAssignments
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Subject)
                .Include(sn => sn.SectionSubject.Section)
                    .ThenInclude(g => g.Grade)
                .Where(s => s.TeacherId == teacherId) //Filter to this teacher only
                .Where(s => !alreadyRecordedAttendance.Contains(s.SectionSubjectId)) //Use SectionSubjectId not Id
                .OrderBy(s => s.SectionSubject.Section.Grade) 
                .ToListAsync();

            bool isAttendanceFinished = false;

            if (TeachersClass != null && TeachersClass.Any())
            {
                //1.Create List to store classes that should be removed
                var classesToRemove = new List<TeacherAssignment>();

                //var sectionId = TeacherClass.SectionSubjectId; wont work because TeacherClass is a list
                //Need to use loop to access SectionSubjectId  
                foreach (var teacherClass in TeachersClass)
                {
                    var sectiondId = teacherClass.SectionSubject.SectionId;
                    var sectionSubjectId = teacherClass.SectionSubject.Id; //1. BAGO

                    // Get the secretary's assignment for this same section
                    var secretaryAssignment = await context.SecretaryAssignments
                                            .Where(sa => sa.SectionId == sectiondId)
                                            .FirstOrDefaultAsync();
                    // Default: secretary hasn't recorded
                    bool secretaryRecorded = false;

                    if (secretaryAssignment != null)
                    {
                        // Check if secretary already recorded attendance for this section
                        secretaryRecorded = await context.Attendances
                                            .AnyAsync(a => a.SecretaryAssignmentId == secretaryAssignment.Id
                                            && a.SectionSubjectId == sectionSubjectId //1. BAGO
                                            && a.RecordedById == secretaryAssignment.SecretaryId
                                            && a.AcademicPeriod == currentAcademicPeriod
                                            && a.AttendanceDate.Date == today);
                    }

                    //2.If secretary already recorded, mark this class for removal
                    if (secretaryRecorded)
                    {
                        classesToRemove.Add(teacherClass);
                    }
                }
                //3.Remove the classes that secretary already recorded attendance for
                foreach(var classToRemove in classesToRemove)
                {
                    TeachersClass.Remove(classToRemove);
                }
                if (!TeachersClass.Any())
                {
                    isAttendanceFinished = true;
                }

            }
            else if (hasAnyAssignments)
            {
                isAttendanceFinished = true;
            }

            ///Initialize students variable as null (wala pang value) 
            List<StudentSectionAssignment> students = null;
            //int? assignmentId = null;

            //If a class is selected, get the students
            if (selectedClassId.HasValue)
            {
                //Get the specific class that was selected
                var selectedClass = TeachersClass.FirstOrDefault(tc => tc.SectionSubjectId == selectedClassId.Value);
                
                if(selectedClass != null)
                {
                    //Get the SectionId from the class
                    var sectionId = selectedClass.SectionSubject.SectionId;

                    //Query students assigned to that section when class is selected
                    students = await context.StudentSectionAssignments
                        .Include(ssa => ssa.Student)
                        .Include(ssa => ssa.Section)
                            .ThenInclude(s => s.Grade)
                        .Where(ssa => ssa.SectionId == sectionId)
                        .OrderBy(ssa => ssa.Student.LastName)
                        .ToListAsync();
                }
            }

            var model = new TeacherAttendanceViewModel()
            {
                teacherClass = TeachersClass, //all teacher's class
                SelectedClassId = selectedClassId, //Selected class (null)
                Students = students, //students in selected class(null)
                TeacherAssignmentId = selectedClassId,
                SectionSubjectId = selectedClassId, //1. BAGO
                //AcademicStatusId = currentAcademicPeriod?.Status ?? - 1, for debugging
                CurrentAcademicPeriodId = currentAcademicPeriod?.Id ?? 1,
                IsStarted = currentAcademicPeriod?.Status == 1,
                YearLevel = currentAcademicPeriod.Year,
                GradingPeriod = currentAcademicPeriod.GradingPeriod,
                IsAttendanceFinished = isAttendanceFinished
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAttendance(SaveAttendanceViewModel model, int? selectedClassId)
        {
            

            if (!model.TeacherAssignmentId.HasValue || model.TeacherAssignmentId == 0)
            {
                return Json(new { success = false, message = "TeacherAssignmentId is missing!" });
            }


            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            //Recorded by the current user that is logged in
            var recordedById = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var TeachersClass = await context.TeacherAssignments
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Subject)
                .Include(sn => sn.SectionSubject.Section)
                    .ThenInclude(g => g.Grade)
                .Where(s => s.TeacherId == recordedById) //Filter to this teacher only
                .FirstOrDefaultAsync();

            foreach (var attendance in model.StudentAttendance)
            {
                var studentId = attendance.Key;
                var marking = attendance.Value;

                //if(attendance.Value == "Excuse")
                //{
                //    string? excuseReason = null;
                //    model.ExcuseReason.TryGetValue(studentId, out excuseReason);

                //    if (string.IsNullOrWhiteSpace(excuseReason))
                //    {
                //        ModelState.AddModelError("ExcuseReason", "Please enter a reason for the excuse.");
                //    }
                //}

                string? excuseReason = null;
                if (marking == "Excuse" && model.ExcuseReason != null)
                {
                    model.ExcuseReason.TryGetValue(studentId, out excuseReason);
                }

                var newAttendance = new Attendance
                {
                    StudentId = studentId,
                    AttendanceMarking = marking,
                    AcademicPeriodId = model.AcademicPeriodId,
                    AttendanceDate = model.AttendanceDate,
                    RecordedById = recordedById,
                    ExcuseReason = excuseReason,
                    TeacherAssignmentId = model.TeacherAssignmentId,
                    SectionSubjectId = model.SelectedClassId, //1. BAGO
                    SecretaryAssignmentId = null,
                    Remarks = model.Remarks,
                    CreatedAt = DateTime.Now

                };
                context.Attendances.Add(newAttendance);
            }

            await context.SaveChangesAsync();

            var classInfo = $"Grade {TeachersClass.SectionSubject.Section.Grade.GradeLevel} - {TeachersClass.SectionSubject.Section.SectionName}";
            var TrackInfo = !string.IsNullOrEmpty(TeachersClass.SectionSubject.Section.Track) ? $"{TeachersClass.SectionSubject.Section.Track}" : "" ;
            var TVLInfo = !string.IsNullOrEmpty(TeachersClass.SectionSubject.Section.TVLProgram) ? $"{TeachersClass.SectionSubject.Section.TVLProgram}" : "";
            var subjectInfo = $"{TeachersClass.SectionSubject.Subject.SubjectDescription}";

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Conduct Attendance",
                entityName: "Attendance",
                entityId: recordedById,
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Teacher {userInfo.username} conduct a record to Class {classInfo} {TrackInfo} {TVLInfo} - {subjectInfo}",
                username: userInfo.username
            );
            model.SelectedClassId = null;

            return Json(new { success = true, message = "Attendance saved successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> _AttendanceReport(int? selectedAcademicPeriod,
                                                            int? teacherAssignment, //selected  Class
                                                            DateTime? startDate, //Date range start
                                                            DateTime? endDate) //date range end
        {
            //Get the Id of the current logged in user
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(teacherId))
            {
                return RedirectToAction("Login", "Login");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            //Get Current default academic period 
            ///NAKA COMMENT NA MUNA KASE MAG BEBASE NA SA SELECTED YEAR SA UI
            var currentAcademicPeriod = await context.AcademicPeriods.FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            //if(!selectedAcademicPeriod.HasValue && currentAcademicPeriod != null)
            //{
            //    selectedAcademicPeriod = currentAcademicPeriod.Id;
            //}

            //Fetch all available academic periods
            var allAcademicPeriod = await context.AcademicPeriods
                                    .OrderBy(ap => ap.Year)
                                    //.Take(8)
                                    .ToListAsync();

            //Query to fetch assign Grade & Section - Subjects on a specific teacher
            var teacherClass = await context.TeacherAssignments
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Subject)
                .Include(sn => sn.SectionSubject.Section)
                    .ThenInclude(g => g.Grade)
                .Where(s => s.TeacherId == teacherId)
                .OrderBy(s => s.SectionSubject.Section.Grade.GradeLevel)
                .ToListAsync();


            //This code means initialize empty lists
            List<AttendanceReportData> studentAttendance = new List<AttendanceReportData>();
            List<DateTime> dateRange = new List<DateTime>();

            ///SUMMARY
            ///.HasValue - a bloolean propeprty tells you whether the nullable variable acutally contains a value
            ///.Value - returns the actual value contained in the nullable type
            ///SUMMARY
            //If all filters are selected, get the data:( e.g filters: teacherClass, startdate to endDate)
            if (teacherAssignment.HasValue && selectedAcademicPeriod.HasValue && startDate.HasValue && endDate.HasValue)
            {
                //get the value base on the selected filter
                var selectedClass = teacherClass.FirstOrDefault(tc => tc.Id == teacherAssignment.Value);
                //var selectedYear = allAcademicPeriod.FirstOrDefault(tc => tc.Id == selectedAcademicPeriod.Value);

                if(selectedClass != null)
                {
                    //get the sectionId on the selected class
                    var sectionId = selectedClass.SectionSubject.SectionId;
                    var sectionSubjectId = selectedClass.SectionSubject.Id;
                    //var academicID = selectedYear.Id;

                    //Get all dates in range
                    for(var date = startDate.Value; date <= endDate.Value; date = date.AddDays(1))
                    {
                        dateRange.Add(date);
                    }

                    //Get the academic year
                    //var year = await context.AcademicPeriods
                    //                .Where(ap => ap.Id == academicID)
                    //                .ToListAsync();

                    //Get students in this section
                    var students = await context.StudentSectionAssignments
                                    .Include(ssa => ssa.Student)
                                    .Where(ssa => ssa.SectionId == sectionId)
                                    .OrderBy(ssa => ssa.Student.LastName)
                                    .ToListAsync();

                    //var secretaryRecord = await context.SecretaryAssignments
                    //                    .Include(sa => sa.Section)
                    //                    .Where(sa => sa.Section.Id == sectionId)
                    //                    .FirstOrDefaultAsync();

                    //Get attendance Record
                    var attendanceRecord = await context.Attendances
                                            .Where(a => //a.TeacherAssignmentId != null
                                                    //&& a.SecretaryAssignmentId == teacherAssignment.Value
                                                    a.SectionSubjectId == sectionSubjectId
                                                    && a.AttendanceDate.Date >= startDate.Value.Date
                                                    && a.AttendanceDate.Date <= endDate.Value.Date
                                                    && a.AcademicPeriod.Id == selectedAcademicPeriod.Value)
                                            .ToListAsync();
                    
                    //BUild report data
                    foreach(var student in students)
                    {
                        var studentData = new AttendanceReportData //Helper in the ViewModel
                        {
                            StudentId = student.StudentId,
                            StudentName = $"{student.Student.FirstName} {student.Student.MiddelName} {student.Student.LastName}",
                            DailyAttendance = new List<string>()
                        };

                        //For each date, find attendance marking (e.g "Present, Late, etc)
                        foreach(var date in dateRange)
                        {
                            var attendance = attendanceRecord
                                .FirstOrDefault(ar => ar.StudentId == student.StudentId
                                                && ar.AttendanceDate.Date == date.Date);

                            if(attendance != null)
                            {
                                //Map attendance marking to P/L/A, shortcute for Present,Late,Absent
                                studentData.DailyAttendance.Add(
                                    attendance.AttendanceMarking == "Present" ? "P":
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
                        //Show 'No data Found' if the selected date range has 0 attendances
                        if(studentData.DailyAttendance.Any(d => d != "-"))
                        {
                            studentAttendance.Add(studentData);

                        }
                    }
                }
            }

            var model = new AttendanceReportViewModel()
            {
                teacherClass = teacherClass.Select(tc => new SelectListItem
                {
                    Value = tc.Id.ToString(),
                    Text = $"Grade {tc.SectionSubject.Section.Grade.GradeLevel} {tc.SectionSubject.Section.SectionName} {tc.SectionSubject.Section.Track} {tc.SectionSubject.Section.TVLProgram} {tc.SectionSubject.Subject.SubjectDescription}",
                }).ToList(),

                academicPeriod = allAcademicPeriod.Select(aap => new SelectListItem
                {
                    Value = aap.Id.ToString(),
                    Text = $"{aap.Year} - {aap.GradingPeriod} Grading " + (aap.IsDefault == 1 ? "✓ Active" : "" ),
                }).ToList(),

                SelectedAcademicPeriod = selectedAcademicPeriod,
                StudentAttendance = studentAttendance,
                DateRange = dateRange,
                SelectedTeacherAssignment = teacherAssignment,
                StartDate = startDate,
                EndDate = endDate,
                
            };

            //model.SelectedAcademicPeriod = selectedAcademicPeriod;
            return View(model);
        } 

        public async Task<IActionResult> Logout()
        {
            var user = await userManager.GetUserAsync(User);

            var userId = user?.Id;
            var schoolId = user?.SchoolId ?? 0;
            var username = user?.UserName;

            await signInManager.SignOutAsync();

            await logService.LogActivity(
                actionType: "Logout",
                entityName: "User",
                entityId: userId,
                userId: userId,
                schoolId: schoolId,
                details: $"User {username} logged out successfully!",
                username: username
            );

            return RedirectToAction("Login", "Login");
        }
    }
}
