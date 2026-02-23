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
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging.Signing;
using OfficeOpenXml;
using OfficeOpenXml.Style;
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
        //allowing code to run both before and after a controller action is executed
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userId = GetCurrentUserId();
            var user = await userManager.FindByIdAsync(userId);

            //Automatic na set this viewbag to all methods/actions
            ViewBag.UserLastName = user?.LastName ?? "User";
            //ViewBag.UserProfilePic = user?.imageFilePath ?? "defaultImage.png";
            ViewBag.UserProfilePic = user?.imageFilePath ?? "";

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
            //var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var teacherId = GetCurrentUserId();
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
                .Where(s => s.TeacherId == teacherId && s.AcademicPeriod == currentAcademicPeriod) //Filter to this teacher only
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

            //Get Current default academic period
            var currentAcademicPeriod = await context.AcademicPeriods
                .FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            //Recorded by the current user that is logged in
            var recordedById = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var TeachersClass = await context.TeacherAssignments
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Subject)
                .Include(sn => sn.SectionSubject.Section)
                    .ThenInclude(g => g.Grade)
                //.Where(s => s.TeacherId == recordedById) //Filter to this teacher only
                .FirstOrDefaultAsync(ta => ta.SectionSubjectId == model.SelectedClassId && ta.TeacherId == recordedById && ta.AcademicPeriod == currentAcademicPeriod);

            if(TeachersClass == null)
            {
                return Json(new
                {
                    success = false,
                    error = "Class not found or access denied"
                });
            }

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
                    //TeacherAssignmentId = model.TeacherAssignmentId,
                    TeacherAssignment = TeachersClass,
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
                                                            string? selectedAttendanceStatus,
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
                                    .IgnoreQueryFilters()
                                    .OrderBy(ap => ap.Year)
                                    //.Take(8)
                                    .ToListAsync();

            List<SelectListItem> myClasses = new List<SelectListItem>();

            if (selectedAcademicPeriod.HasValue)
            {
                //Query to fetch assign Grade & Section - Subjects on a specific teacher
                myClasses = await context.TeacherAssignments
                    .IgnoreQueryFilters()
                    .Include(ta => ta.SectionSubject)
                        .ThenInclude(ss => ss.Subject)
                    .Include(sn => sn.SectionSubject.Section)
                        .ThenInclude(g => g.Grade)
                    .Where(s => s.TeacherId == teacherId && s.AcademicPeriodId == selectedAcademicPeriod)
                    .OrderBy(s => s.SectionSubject.Section.Grade.GradeLevel)
                    .Select(tc => new SelectListItem
                    {
                        Value = tc.Id.ToString(),
                        Text = $"Grade {tc.SectionSubject.Section.Grade.GradeLevel} {tc.SectionSubject.Section.SectionName} {tc.SectionSubject.Section.Track} {tc.SectionSubject.Section.TVLProgram} {tc.SectionSubject.Subject.SubjectDescription}",
                    })
                    .ToListAsync();
            }

            ////Query to fetch assign Grade & Section - Subjects on a specific teacher
            //var teacherClass = await context.TeacherAssignments
            //    .IgnoreQueryFilters()
            //    .Include(ta => ta.SectionSubject)
            //        .ThenInclude(ss => ss.Subject)
            //    .Include(sn => sn.SectionSubject.Section)
            //        .ThenInclude(g => g.Grade)
            //    .Where(s => s.TeacherId == teacherId)
            //    .OrderBy(s => s.SectionSubject.Section.Grade.GradeLevel)
            //    .ToListAsync();


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
                //var selectedClass = myClasses.FirstOrDefault(tc => tc.Id == teacherAssignment.Value);
                var selectedClass = await context.TeacherAssignments
                    .IgnoreQueryFilters()
                    .Include(ta => ta.SectionSubject)
                        .ThenInclude(ss => ss.Subject)
                    .Include(sn => sn.SectionSubject.Section)
                        .ThenInclude(g => g.Grade)
                    .Where(s => s.TeacherId == teacherId && s.AcademicPeriodId == selectedAcademicPeriod)
                    .FirstOrDefaultAsync(tc => tc.Id == teacherAssignment.Value);
                //var selectedYear = allAcademicPeriod.FirstOrDefault(tc => tc.Id == selectedAcademicPeriod.Value);

                if (selectedClass != null)
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
                                    .IgnoreQueryFilters()
                                    .Include(ssa => ssa.Student)
                                    .Where(ssa => ssa.SectionId == sectionId)
                                    .OrderBy(ssa => ssa.Student.LastName)
                                    .ToListAsync();

                    //var secretaryRecord = await context.SecretaryAssignments
                    //                    .Include(sa => sa.Section)
                    //                    .Where(sa => sa.Section.Id == sectionId)
                    //                    .FirstOrDefaultAsync();

                    //Get attendance Record
                    var attendanceRecord = context.Attendances
                                            .IgnoreQueryFilters()
                                            .Where(a => //a.TeacherAssignmentId != null
                                                        //&& a.SecretaryAssignmentId == teacherAssignment.Value
                                                    a.SectionSubjectId == sectionSubjectId
                                                    && a.AttendanceDate.Date >= startDate.Value.Date
                                                    && a.AttendanceDate.Date <= endDate.Value.Date
                                                    && a.AcademicPeriod.Id == selectedAcademicPeriod.Value);
                                            //.ToListAsync();

                    if (!string.IsNullOrEmpty(selectedAttendanceStatus))
                    {
                        attendanceRecord = context.Attendances
                                            .IgnoreQueryFilters()
                                            .Where(a => a.AttendanceMarking == selectedAttendanceStatus);
                    }

                    var record = await attendanceRecord.ToListAsync();
                    
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
                            var attendance = record
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
                //teacherClass = teacherClass.Select(tc => new SelectListItem
                //{
                //    Value = tc.Id.ToString(),
                //    Text = $"Grade {tc.SectionSubject.Section.Grade.GradeLevel} {tc.SectionSubject.Section.SectionName} {tc.SectionSubject.Section.Track} {tc.SectionSubject.Section.TVLProgram} {tc.SectionSubject.Subject.SubjectDescription}",
                //}).ToList(),

                teacherClass = myClasses,

                academicPeriod = allAcademicPeriod.Select(aap => new SelectListItem
                {
                    Value = aap.Id.ToString(),
                    Text = $"{aap.Year} - {aap.GradingPeriod} Grading " + (aap.IsDefault == 1 ? "✓ Active" : "" ),
                }).ToList(),

                SelectedAcademicPeriod = selectedAcademicPeriod,
                StudentAttendance = studentAttendance,
                SelectedAttendanceStatus = selectedAttendanceStatus,
                DateRange = dateRange,
                SelectedTeacherAssignment = teacherAssignment,
                StartDate = startDate,
                EndDate = endDate,
                
            };

            //model.SelectedAcademicPeriod = selectedAcademicPeriod;
            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> GetTeacherAssignment(int academicPeriodId)
        {
            var teacherId = GetCurrentUserId();

            var allAcademicPeriod = await context.AcademicPeriods
                .IgnoreQueryFilters()
                .ToListAsync();

            var teacherClass = await context.TeacherAssignments
                .IgnoreQueryFilters()
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Subject)
                .Include(ta => ta.SectionSubject.Section)
                    .ThenInclude(s => s.Grade)
                .Include(ap => ap.AcademicPeriod)
                .Where(ta => ta.TeacherId == teacherId && ta.AcademicPeriodId == academicPeriodId)
                .OrderBy(ta => ta.SectionSubject.Section.Grade.GradeLevel)
                .Select(tc => new
                {
                    Value = tc.Id.ToString(),
                    Text = $"Grade {tc.SectionSubject.Section.Grade.GradeLevel} {tc.SectionSubject.Section.SectionName} {tc.SectionSubject.Section.Track} {tc.SectionSubject.Section.TVLProgram} {tc.SectionSubject.Subject.SubjectDescription}",
                })
                .ToListAsync();

            return Json(teacherClass);
        }

        [HttpGet]
        public async Task<IActionResult> ExportAttendanceReport(int? selectedAcademicPeriod,
                                                            string? selectedAttendanceStatus,
                                                            int? teacherAssignment, //selected  Class
                                                            DateTime? startDate, //Date range start
                                                            DateTime? endDate)
        {
            if (!selectedAcademicPeriod.HasValue
               || !teacherAssignment.HasValue
               || !startDate.HasValue
               || !endDate.HasValue)
            {
                TempData["ErrorMessage"] = "Please select all filters before exporting.";
                return RedirectToAction("AttendanceReport");
                //return Json(new { success = false, message = "Please select all filters before exporting." });
            }

            var teacherId = GetCurrentUserId();

            var query = await userManager.FindByIdAsync(teacherId);

            var firstName = query.FirstName;
            var middleName = query?.MiddleName;
            var lastName = query.LastName;

            var selectedClass = await context.TeacherAssignments
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Subject)
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Section)
                        .ThenInclude(s => s.Grade)
                .Where(ta => ta.TeacherId == teacherId && ta.AcademicPeriodId == selectedAcademicPeriod)
                .FirstOrDefaultAsync(tc => tc.Id == teacherAssignment.Value);

            if (selectedClass == null)
            {
                TempData["ErrorMessage"] = "Selected class not found.";
                return RedirectToAction("AttendanceReport");
                //return Json(new { success = false, message = "Selected class not found" });

            }

            var sectionId = selectedClass.SectionSubject.SectionId;
            var sectionSubjectId = selectedClass.SectionSubject.Id;

            var dateRange = new List<DateTime>();
            for (var date = startDate.Value; date <= endDate.Value; date = date.AddDays(1))
            {
                dateRange.Add(date);
            }

            var students = await context.StudentSectionAssignments
               .IgnoreQueryFilters()
               .Include(ssa => ssa.Student)
               .Where(ssa => ssa.SectionId == sectionId)
               .OrderBy(ssa => ssa.Student.LastName)
               .ToListAsync();

            //Get Attendance Record
            var attendanceRecord = context.Attendances
                .IgnoreQueryFilters()
                .Where(a => a.SectionSubjectId == sectionSubjectId
                        && a.AttendanceDate.Date >= startDate.Value.Date
                        && a.AttendanceDate.Date <= endDate.Value.Date
                        && a.AcademicPeriod.Id == selectedAcademicPeriod.Value);
            //.ToListAsync();

            if (!string.IsNullOrEmpty(selectedAttendanceStatus))
            {
                attendanceRecord = attendanceRecord
                                //.IgnoreQueryFilters()
                                .Where(a => a.AttendanceMarking == selectedAttendanceStatus);
            }

            var record = await attendanceRecord.ToListAsync();

            //build report data
            var studentAttendance = new List<AdminAttendanceReportData>();

            foreach (var student in students)
            {
                var studentData = new AdminAttendanceReportData
                {
                    StudentId = student.StudentId,
                    StudentName = $"{student.Student.FirstName} {student.Student.MiddelName} {student.Student.LastName}",
                    DailyAttendance = new List<string>()
                };

                foreach (var date in dateRange)
                {
                    var attendance = record
                        .FirstOrDefault(ar => ar.StudentId == student.StudentId
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
                .FirstOrDefaultAsync(ap => ap.Id == selectedAcademicPeriod.Value);


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
                worksheet.Cells[currentRow, 1].Value = $"Date Range: {startDate.Value:MMM dd, yyyy} - {endDate.Value:MMM dd, yyyy}";
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

                foreach (var (header, color) in statHeaders)
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
                foreach (var student in studentAttendance)
                {
                    //int startRow = currentRow;
                    col = 1;

                    worksheet.Cells[currentRow, col].Value = student.StudentName;
                    worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                    col++;

                    foreach (var marking in student.DailyAttendance)
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
                                $"{startDate.Value:yyyyMMdd}-{endDate.Value:yyyyMMdd}.xlsx";

                return File(stream,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);

            }
        }

        [HttpGet]
        public async Task<IActionResult> _MyClasses()
        {
            var teacherId = GetCurrentUserId();

            var user = await userManager.FindByIdAsync(teacherId);

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

            var defaultYear = await context.AcademicPeriods
                .FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            var TeacherClass = await context.TeacherAssignments
                //.IgnoreQueryFilters()
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Section)
                        .ThenInclude(s => s.Grade)
                .Include(ta => ta.SectionSubject.Subject)
                .Where(ta => ta.TeacherId == teacherId && ta.AcademicPeriod == defaultYear)
                //.Distinct()
                .ToListAsync();

            var model = new MyClassesViewModel()
            {
                LRN = user.SchoolId,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Sex = user.Sex,
                positionTitle = user.positionTitle,
                imageFilePath = user.imageFilePath,
                teacherAssignments = TeacherClass,
                currentAcademicYear = defaultYear.Year,
                currentPeriod = defaultYear.GradingPeriod

            };

            ViewData["imageFileData"] = user.imageFileData;

            return View(model);

        }

        [HttpGet]
        public async Task<IActionResult> SelfAssign()
        {
            var defaultYear = await context.AcademicPeriods
                            .FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            var teacherId = GetCurrentUserId();
            var teacher = await userManager.FindByIdAsync(teacherId);

            if (string.IsNullOrEmpty(teacherId))
            {
                return RedirectToAction("TeacherHome", "Teacher");
            }

            /// excluding ALL assigned subjects
            var assignedSectionSubjectIds = await context.TeacherAssignments
                        .IgnoreQueryFilters()
                        .Where(ta => ta.AcademicPeriod == defaultYear)
                        .Select(ss => ss.SectionSubjectId)
                        .Distinct()
                        .ToListAsync();

            var sectionSubjectQuery = await context.SectionSubjects
                        .Include(ss => ss.Subject)
                        .Include(s => s.Section)
                            .ThenInclude(g => g.Grade)
                        .Where(ss => !assignedSectionSubjectIds.Contains(ss.Id))
                        .OrderBy(ss => ss.Section.Grade.GradeLevel)
                        .ToListAsync();

            var model = new SelfAssignViewModel()
            {
                TeacherId = teacherId,
                SectionSubjects = sectionSubjectQuery
            };

            return PartialView("_SelfAssignPartial", model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSelfAssign(int sectionSubjectId)
        {
            var teacherId = GetCurrentUserId();

            if(teacherId == null)
            {
                return Json(new { success = false, message = "Could not find teacher Id!" });
            }
            var teacher = await userManager.FindByIdAsync(teacherId);

            if (teacher == null)
            {
                return Json(new { success = false, message = "Teacher not found!" });
            }

            var defaultYear = await context.AcademicPeriods
                .FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            var assigned = new TeacherAssignment()
            {
                TeacherId = teacherId,
                SectionSubjectId = sectionSubjectId,
                AcademicPeriod = defaultYear,
                CreatedAt = DateTime.UtcNow
            };

            await context.TeacherAssignments.AddAsync(assigned);
            await context.SaveChangesAsync();

            assigned = await context.TeacherAssignments
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Section)
                    .ThenInclude(s => s.Grade)
                .FirstOrDefaultAsync(ta => ta.Id == assigned.Id);

            var gradeInfo = $"Grade {assigned.SectionSubject.Section.Grade.GradeLevel}";
            var sectionInfo = $"{assigned.SectionSubject.Section.SectionName}";
            var trackInfo = !string.IsNullOrEmpty(assigned.SectionSubject.Section.Track) ? $" - {assigned.SectionSubject.Section.Track}" : "";
            var tvlInfo = !string.IsNullOrEmpty(assigned.SectionSubject.Section.TVLProgram) ? $" ({assigned.SectionSubject.Section.TVLProgram})" : "";

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Self Teacher",
                entityName: "TeacherAssignment",
                entityId: teacher.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Teacher {userInfo.username} self assigned to {gradeInfo} - {sectionInfo} {trackInfo} {tvlInfo}",
                username: userInfo.username
            );

            var assignedSectionSubjectIds = await context.TeacherAssignments
                        .IgnoreQueryFilters()
                        .Where(ta => ta.AcademicPeriod == defaultYear)
                        .Select(ss => ss.SectionSubjectId)
                        .Distinct()
                        .ToListAsync();

            var sectionSubjectQuery = await context.SectionSubjects
                .Include(ss => ss.Subject)
                .Include(s => s.Section)
                    .ThenInclude(g => g.Grade)
                .Where(ss => !assignedSectionSubjectIds.Contains(ss.Id))
                .OrderBy(ss => ss.Section.Grade.GradeLevel)
                .ToListAsync();

            var model = new SelfAssignViewModel()
            {
                TeacherId = teacherId,
                SectionSubjects = sectionSubjectQuery
            };

            return PartialView("_SelfAssignPartial", model);
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveSelfAssign(int Id)
        {
            var teacherId = GetCurrentUserId();

            var teacherAssigned = await context.TeacherAssignments
                .IgnoreQueryFilters()
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Section)
                        .ThenInclude(g => g.Grade)
                .FirstOrDefaultAsync(ta => ta.Id == Id && ta.TeacherId == teacherId);

            if (teacherAssigned == null)
            {
                return Json(new { success = false, error = "Id not Found!" });
            }

            //var teacherId = teacherAssigned.TeacherId;

            //context.TeacherAssignments.Remove(teacherAssigned);

            var time = DateTime.UtcNow;

            teacherAssigned.IsDeleted = true;
            teacherAssigned.DeletedAt = time;


            await context.SaveChangesAsync();

            var teacher = await context.Users.FindAsync(teacherId);

            var gradeInfo = $"Grade {teacherAssigned.SectionSubject.Section.Grade.GradeLevel}";
            var sectionInfo = $"{teacherAssigned.SectionSubject.Section.SectionName}";
            var trackInfo = !string.IsNullOrEmpty(teacherAssigned.SectionSubject.Section.Track) ? $" - {teacherAssigned.SectionSubject.Section.Track}" : "";
            var tvlInfo = !string.IsNullOrEmpty(teacherAssigned.SectionSubject.Section.TVLProgram) ? $" ({teacherAssigned.SectionSubject.Section.TVLProgram})" : "";

            var userInfo = await GetCurrentUserInfo();

            await logService.LogActivity(
                actionType: "Remove Assignment",
                entityName: "TeacherAssignment",
                entityId: teacher.Id.ToString(),
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"Teacher {userInfo.username} remove assignment {gradeInfo} - {sectionInfo} {trackInfo} {tvlInfo}. School Id: {teacher.SchoolId}",
                username: userInfo.username
            );

            var remainingAssignments = await context.TeacherAssignments
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(s => s.Section)
                        .ThenInclude(g => g.Grade)
                .Include(ss => ss.SectionSubject.Subject)
                .Where(ta => ta.TeacherId == teacherId)
                .ToListAsync();

            var model = new MyClassesViewModel()
            {
                LRN = teacher.SchoolId,
                FirstName = teacher.FirstName,
                MiddleName = teacher.MiddleName,
                LastName = teacher.LastName,
                Sex = teacher.Sex,
                positionTitle = teacher.positionTitle,
                imageFilePath = teacher.imageFilePath,
                teacherAssignments = remainingAssignments
            };

            return Json(new { success = true, message = "Assigned class removed successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> ManageClass(int id)
        {
            var assignmentId = await context.TeacherAssignments
                .FindAsync(id);

            if(assignmentId == null)
            {
                return Json(new { success = false, message = "Assginemnt Id does not found!" });
            }

            var teacherClass = await context.TeacherAssignments
                                .Include(ta => ta.SectionSubject)
                                    .ThenInclude(ss => ss.Section)
                                        .ThenInclude(g => g.Grade)
                                .Where(ta => ta.Id == id)
                                .FirstOrDefaultAsync();

            var sectionId = teacherClass.SectionSubject.SectionId;
            var section = teacherClass.SectionSubject.Section.Id;
            var subjectId = teacherClass.SectionSubject.SubjectId;

            var studentsClass = await context.Sections
                                .Include(s => s.Grade)
                                .Where(sec => sec.Id == section)
                                .FirstOrDefaultAsync();

            var student = await context.StudentSectionAssignments
                            .Include(ssa => ssa.Student)
                            .Where(ssa => ssa.SectionId == sectionId)
                            .OrderBy(s => s.Student.LastName)
                            .ToListAsync();

            var studentSubjects = await context.Subjects
                                  .Where(s => s.Id == subjectId)
                                  .FirstOrDefaultAsync();


            var model = new ViewManageClassViewModel()
            {
                Students = student,
                Section = studentsClass,
                Subject = studentSubjects
            };

            return View(model);
        }

        //[HttpDelete]
        //public async Task<IActionResult> RemoveStudent(int id)
        //{
        //    var student = await context.StudentSectionAssignments
        //                    .Include(ssa => ssa.Student)
        //                    .Include(sec => sec.Section)
        //                        .ThenInclude(s => s.SectionSubjects)
        //                            .ThenInclude(ss => ss.Subject)
        //                    .FirstOrDefaultAsync(ssa => ssa.Id == id);

        //    if(student == null)
        //    {
        //        return Json(new { succes = false, message = "Assignment id does not found" });
        //    }

        //    var studentId = await context.Students.FirstOrDefaultAsync(s => s.Id == student.StudentId);

        //    var hasAttendance = await context.Attendances
        //                        .AnyAsync(a => a.StudentId == studentId.Id);

        //    var time = DateTime.UtcNow;

        //    if (hasAttendance)
        //    {
        //        student.IsDeleted = true;
        //        student.DeletedAt = time;

        //        await context.SaveChangesAsync();

        //        return Json(new { success = true, message = "Student assignment! Attendance data is archived for history data!" });

        //    }

        //    student.IsDeleted = true;
        //    student.DeletedAt = time;

        //    await context.SaveChangesAsync();

        //    var grade = student.Section.Grade.GradeLevel;
        //    var section = student.Section.SectionName;
        //    var Track = student.Section?.Track;
        //    var TVLProgram = student.Section?.TVLProgram;
        //    //Need gumamit ng FirstOrDefault para maaccess si subject kase list of collection si SectionSubject eh isang subject lang need natin iaccess.
        //    var subject = student?.Section?.SectionSubjects?.FirstOrDefault().Subject.SubjectDescription;


        //    var userInfo = await GetCurrentUserInfo();

        //    await logService.LogActivity(
        //        actionType: "Remove",
        //        entityName: "Student's Assignment",
        //        entityId: student.Id.ToString(),
        //        userId: userInfo.userId,
        //        schoolId: userInfo.schoolId,
        //        details: $"Admin remove {studentId.FirstName} {studentId.MiddelName} {studentId.LastName} from Grade {grade} - {section} {Track} {TVLProgram}. Subject: {subject}",
        //        username: userInfo.username
        //    );

        //    return Json(new { success = true, message = "Student assignment! Attendance data is archived for history data!" });
        //}

        [HttpGet]
        public async Task<IActionResult> ManageSecretary()
        {
            var teacherId = GetCurrentUserId();

            var user = await userManager.FindByIdAsync(teacherId);

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


            var currentDefaultYear = await context.AcademicPeriods
                .FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            var sectionSubjectIds = await context.TeacherAssignments
                                    .Where(t => t.TeacherId == teacherId && t.AcademicPeriod == currentDefaultYear)
                                    .Select(ta => ta.SectionSubjectId)
                                    .ToListAsync();

            var sectionIds = await context.SectionSubjects
                                .Where(ss => sectionSubjectIds.Contains(ss.Id))
                                .Select(ss => ss.SectionId)
                                .Distinct()
                                .ToListAsync();

            //if (!sectionIds.Any())
            //{
            //    return RedirectToAction("TeacherHome", "Teacher");
            //}

            var secretaryAssignment = await context.SecretaryAssignments
                                .Include(sa => sa.Section)
                                    .ThenInclude(s => s.SectionSubjects)
                                        .ThenInclude(ss => ss.Subject)
                                .Include(g => g.Section.Grade)
                                .Include(user => user.Secretary)
                                .Where(s => sectionIds.Contains(s.SectionId))
                                .ToListAsync();

            var model = new MySecretariesViewModel()
            {
                Secretary = secretaryAssignment
            };

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> AddSecretary()
        {
            var teacherId = GetCurrentUserId();

            if(teacherId == null)
            {
                return Json(new { success = true, message = "Id does not Found" });
            }

            var currentAcademicPeriod = await context.AcademicPeriods
                                        .Where(ap => ap.IsDefault == 1)
                                        .FirstOrDefaultAsync();

            var teacherClass = await context.TeacherAssignments
                                        .Where(ta => ta.TeacherId == teacherId && ta.AcademicPeriod == currentAcademicPeriod)
                                        .Select(ta => new
                                        {
                                            SectionId = ta.SectionSubject.Section.Id,
                                            GradeLevel = ta.SectionSubject.Section.Grade.GradeLevel,
                                            SectionName = ta.SectionSubject.Section.SectionName,
                                            SubjectDescription = ta.SectionSubject.Subject.SubjectDescription,
                                            Track = ta.SectionSubject.Section.Track,
                                            TVLProgram = ta.SectionSubject.Section.TVLProgram
                                        })
                                        .OrderBy(x => x.GradeLevel)
                                        .ToListAsync();


            //var availableClass = await context.Sections
            //                        .Include(s => s.Grade)
            //                        .Include(sub => sub.SectionSubjects)
            //                            .ThenInclude(ss => ss.Subject)
            //                        .Where(s => teacherClass.Contains(s.Id))
            //                        .OrderBy(g => g.Grade.GradeLevel)
            //                        .Select(ags => new
            //                        {
            //                            ags.Id,
            //                            ags.Grade.GradeLevel,
            //                            ags.SectionName,
            //                            ags.Track,
            //                            ags.TVLProgram,
            //                            ags.SectionSubjects?
            //                        .FirstOrDefault().Subject.SubjectDescription
            //                        })
            //                        .ToListAsync();

            var model = new TeacherSecretaryViewModel()
            {
                AvailableClass = teacherClass.Select(ags => new SelectListItem
                {
                    Value = ags.SectionId.ToString(),
                    Text = ags.TVLProgram == null
                            ? $"Grade {ags.GradeLevel} - {ags.SectionName}, {ags.Track}: {ags.SubjectDescription}"
                            : $"Grade {ags.GradeLevel} - {ags.SectionName}, {ags.Track} - {ags.TVLProgram}: {ags.SubjectDescription}"
                })
                .ToList()
            };


            return PartialView("_AddSecretaryPartial", model);
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
