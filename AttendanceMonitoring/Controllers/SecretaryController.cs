using AttendanceMonitoring.Data;
using AttendanceMonitoring.Models;
using AttendanceMonitoring.Services;
using AttendanceMonitoring.ViewModel.Secretary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using System.Linq;
using System.Security.Claims;

namespace AttendanceMonitoring.Controllers
{

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [Authorize(Roles = "Secretary")]
    public class SecretaryController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IActivityLogService _logService;
        public SecretaryController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment, IActivityLogService logService)
        {
            this._signInManager = signInManager;
            this._userManager = userManager;
            this._context = context;
            this._environment = environment;
            _logService = logService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        public string GetCurrentUsername()
        {
            return User.Identity.Name;
        }
        public async Task<int> GetCurrentUserSchoolId()
        {
            var userId = GetCurrentUserId();

            var user = await _userManager.FindByIdAsync(userId);

            return user.SchoolId;
        }
        public async Task<(string userId, string userName, int schoolId)> GetCurrentUserInfo()
        {
            var userId = GetCurrentUserId();

            var user = await _userManager.FindByIdAsync(userId);

            return (userId, user.UserName, user.SchoolId);
        }

        public IActionResult SecretaryHome()
        {
            return View();
        }

        //[HttpGet]
        //public async Task<IActionResult> _Attendance(int? defaultClassId)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var overallErrors = ModelState.ToDictionary(
        //            kvp => kvp.Key,
        //            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
        //        );

        //        return Json(new { success = false, errors = overallErrors });
        //    }

        //    var today = DateTime.Now;
        //    //Get the user id that is currently login
        //    var secretaryId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //    if (string.IsNullOrEmpty(secretaryId))
        //    {
        //        return RedirectToAction("SecretaryHome", "Secretary");
        //    }

        //    //Get Current default academic Period
        //    var currentAcademicPeriod = await _context.AcademicPeriods.FirstOrDefaultAsync(ap => ap.IsDefault == 1);

        //    //Get Secretary's Assignment
        //    var SecretaryClass = await _context.SecretaryAssignments
        //                        .Include(sa => sa.Section)
        //                            .ThenInclude(s => s.Grade)
        //                        .Include(sn => sn.Section.SectionSubjects)
        //                            .ThenInclude(ss => ss.Subject)
        //                        .Where(s => s.SecretaryId == secretaryId)
        //                        .FirstOrDefaultAsync();

        //    //If Secretary has an assignment, check if attendance already Exists
        //    if(SecretaryClass != null)
        //    {
        //        //Single object code to get sectionId
        //        var sectionId = SecretaryClass.SectionId;

        //        //check if secretary already recorded attendance 
        //        var secretaryRecorded = await _context.Attendances
        //            .AnyAsync(a => a.SecretaryAssignmentId == SecretaryClass.Id
        //                        && a.RecordedById == secretaryId
        //                        && a.AcademicPeriod == currentAcademicPeriod
        //                        && a.AttendanceDate.Date == today);

        //        //Get the teacher's assignment for this same section that the secretary is assigned
        //        var teacherAssignment = await _context.TeacherAssignments
        //                                .Include(ta => ta.SectionSubject)
        //                                .Where(ta => ta.SectionSubject.SectionId == sectionId)
        //                                .FirstOrDefaultAsync();

        //        //1.If teacher assignment doesn't exist → Keep it as false (no need to check)
        //        //bool teacherRecorded = false; → Default assumption: "Teacher hasn't recorded yet"
        //        bool teacherRecorded = false;

        //        if(teacherAssignment != null)
        //        {
        //            //2. If may teacher Record dun palang gagana ang if else na ito -> only check if teacher assignment exist
        //            //check if teacher already conducted for attendance for this section
        //            teacherRecorded = await _context.Attendances
        //                .AnyAsync(a => a.TeacherAssignmentId == teacherAssignment.Id
        //                            && a.RecordedById == teacherAssignment.TeacherId
        //                            && a.AcademicPeriod == currentAcademicPeriod
        //                            && a.AttendanceDate.Date == today);
        //        }

        //        //If either secretary or teacher already recorded attendance for this section
        //        if(secretaryRecorded || teacherRecorded)
        //        {
        //            SecretaryClass = null;
        //        }
        //    }


        //    List<StudentSectionAssignment> students = null;

        //    if (SecretaryClass != null)
        //    {
        //        var sectionId = SecretaryClass.SectionId; //geet the actual Sectionid

        //        students = await _context.StudentSectionAssignments
        //            .Include(ssa => ssa.Student)
        //            .Include(ssa => ssa.Section)
        //                .ThenInclude(s => s.Grade)
        //            .Where(ssa => ssa.SectionId == sectionId)
        //            .ToListAsync();
        //    }

        //    var model = new AttendanceViewModel()
        //    {
        //        SecretaryClass = SecretaryClass, //Single assignment
        //        Students = students, //students in that section 
        //        SecretaryAssignmentId = SecretaryClass?.Id, //Use the actual assignment ID
        //        CurrentAcademicPeriodId = currentAcademicPeriod?.Id ?? 1,
        //        YearLevel = currentAcademicPeriod.Year,
        //        GradingPeriod = currentAcademicPeriod.GradingPeriod,
        //    };

        //    return View(model);
        //}

        [HttpGet]
        public async Task<IActionResult> _Attendance(int? selectedSubjectId)
        {
            //Get the user id that is currently login
            var secretaryId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(secretaryId))
            {
                return RedirectToAction("SecretaryHome", "Secretary");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            var today = DateTime.Today;


            //Get Current default academic Period
            var currentAcademicPeriod = await _context.AcademicPeriods.FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            //1.Get Secretary's Assignment    
            var SecretaryClass = await _context.SecretaryAssignments
                                .Where(s => s.SecretaryId == secretaryId)
                                .ToListAsync();
            //2.Extract only the Section IDs
            var secrectaryAssignedSectionIds = SecretaryClass
                .Select(s => s.SectionId)
                .ToList();

            // Get all subjects in sections assigned to this secretary
            var subjects = await _context.SectionSubjects
                .Include(ss => ss.Subject)
                .Include(ss => ss.Section)
                    .ThenInclude(s => s.Grade)
                .Where(ss => secrectaryAssignedSectionIds.Contains(ss.SectionId))
                .ToListAsync();

            ///<summary>
            ///Queries inside foreach. Problem: N + 1 problem
            /// </summary>
            //track subjects that must be hidden
            var alreadyRecordedSectionSubjectIds = new List<int>();

            foreach (var subject in subjects.ToList())
            {
                var sectionId = subject.SectionId;
                var sectionSubjectId = subject.Id;
                //Find the SecretaryAssignment for this section
                var secretaryAssignment = SecretaryClass
                    .FirstOrDefault(sc => sc.SectionId == sectionId);

                if (secretaryAssignment != null)
                {
                    //Check if secretary already recorded attendance for this specific subject
                    var secretaryRecorded = await _context.Attendances //n + 1 problem, kase may await sa loob ng foreach
                        .AnyAsync(a => a.SecretaryAssignmentId == secretaryAssignment.Id
                                    && a.SectionSubjectId == sectionSubjectId
                                    && a.RecordedById == secretaryId
                                    && a.AcademicPeriodId == currentAcademicPeriod.Id
                                    && a.AttendanceDate.Date == today);

                    if (secretaryRecorded)
                    {
                        alreadyRecordedSectionSubjectIds.Add(sectionSubjectId);
                        continue; // Skip teacher check, already excluded
                    }
                }

                var teacherRecorded = await _context.Attendances //n + 1 problem, kase may await sa loob ng foreach
                    .AnyAsync(a => a.TeacherAssignmentId != null
                                && a.SectionSubjectId == sectionSubjectId
                                //&& a.RecordedById == teacherAssignment.TeacherId
                                && a.AcademicPeriodId == currentAcademicPeriod.Id
                                && a.AttendanceDate.Date == today);

                if (teacherRecorded)
                {
                    alreadyRecordedSectionSubjectIds.Add(sectionSubjectId);
                    continue; // Skip teacher check, already excluded

                }

            }
            //Remove subject that already have attendance
            subjects = subjects
                    .Where(s => !alreadyRecordedSectionSubjectIds.Contains(s.Id))
                    .ToList();

            ///<summary>
            ///REFACTOR FOR = "Queries inside foreach. Problem: N + 1 problem"
            /// </summary>
            /// 

            ////1. Get all the data needed in one Query each
            //var sectiondIds = subjects
            //    .Select(s => s.SectionId)
            //    .Distinct()
            //    .ToList();
            //var sectionSubjectIds = subjects
            //    .Select(s => s.Id)
            //    .ToList();

            ////Get all relevant secretary assignemnts at once
            //var secretaryAssignments = SecretaryClass
            //    .Where(sc => sectiondIds.Contains(sc.SectionId))
            //    .ToList(); //Load into memory
            ////to avoid error CS1929 when query secretaryRecorded usses contains
            //var secretaryAssignmentIds = secretaryAssignments
            //    .Select(sa => sa.Id)
            //    .ToList();

            ////Get all secretary-recorded attendances at once    
            //var secretaryRecorded = await _context.Attendances
            //    .Where(a => secretaryAssignmentIds.Contains(a.SecretaryAssignmentId ?? 0) //Select().Contains() creates a new enumerable then searches through it //Any() stops immediately kapag nakita yung match
            //            && sectionSubjectIds.Contains(a.SectionSubjectId ?? 0) // PROBLEMA: int? vs int kapag walang HasValue at Value
            //            && a.RecordedById == secretaryId
            //            && a.AcademicPeriodId == currentAcademicPeriod.Id
            //            && a.AttendanceDate.Date == today)
            //    .Select(a => a.SectionSubjectId)
            //    .Distinct()
            //    .ToListAsync();

            ////Get all teacher assignment at once
            //var teacherAssignments = await _context.TeacherAssignments
            //    .Where(ta => sectionSubjectIds.Contains(ta.SectionSubjectId))
            //    .ToListAsync();

            ////Get all teacher-recorded attendances at once
            //var teacherAssignmentIds = teacherAssignments.Select(ta => ta.Id).ToList();
            //var teacherIds = teacherAssignments.Select(ta => ta.TeacherId).ToList();

            //var teacherRecorded = await _context.Attendances
            //    .Where(a => teacherAssignmentIds.Contains(a.TeacherAssignmentId ?? 0)
            //            && sectionSubjectIds.Contains(a.SectionSubjectId ?? 0)
            //            && teacherIds.Contains(a.RecordedById ?? "") //Quote sign kapag yung id ay string
            //            && a.AcademicPeriodId == currentAcademicPeriod.Id
            //            && a.AttendanceDate == today)
            //    .Select(a => a.SectionSubjectId)
            //    .Distinct()
            //    .ToListAsync();

            ////2.Combine exclusion List
            //var alreadyRecordedSectionSubjectsIds = secretaryRecorded
            //    .Union(teacherRecorded)
            //    .ToHashSet();
            ////3. Filter in Memory
            //subjects = subjects
            //    .Where(s => !alreadyRecordedSectionSubjectsIds.Contains(s.Id))
            //    .ToList();
            //////////////////////////////////////////////////////////////////////////////////////////
            List<StudentSectionAssignment> students = null;
            int? actualsecretaryAssignmentId = null; //kaya null kase tulad ng students wala pang selected value


            if (selectedSubjectId.HasValue)
            {
                var selectedSubject = subjects.FirstOrDefault(sc => sc.Id == selectedSubjectId.Value);

                if (selectedSubject != null)
                {
                    var sectionId = selectedSubject.SectionId; //geet the actual Sectionid

                    //Kunin yung SecretaryAssignment.Id                                                 - Question Mark prevents NullreferenceException para kapag null it will return null instead of crashing. And dapat yung secretaryAssignmentId is nullable kaya naging int?
                    actualsecretaryAssignmentId = SecretaryClass.FirstOrDefault(sc => sc.SectionId == sectionId)?.Id;

                    students = await _context.StudentSectionAssignments
                        .Include(ssa => ssa.Student)
                        .Include(ssa => ssa.Section)
                            .ThenInclude(s => s.Grade)
                        .Where(ssa => ssa.SectionId == sectionId)
                        .ToListAsync(); 
                }
            }


            var model = new AttendanceViewModel()
            {
                SecretaryClass = subjects, //Single assignment
                SelectedSubjectId = selectedSubjectId,
                Students = students, //students in that section 
                SecretaryAssignmentId = actualsecretaryAssignmentId,
                SectionSubjectId = selectedSubjectId,
                CurrentAcademicPeriodId = currentAcademicPeriod?.Id ?? 1,
                YearLevel = currentAcademicPeriod.Year,
                GradingPeriod = currentAcademicPeriod.GradingPeriod,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttendance(SecretarySaveAttendanceViewModel model)
        {
            if(!model.SecretaryAssignmentId.HasValue || model.SecretaryAssignmentId == 0)
            {
                return Json(new { success = false, message = "Secretary Assignment Id is missing!" });
            }

            if(!model.SectionSubjectId.HasValue || model.SectionSubjectId == 0)
            {
                return Json(new { sucess = false, message = "Section Subject Id is missing!" });
            }

            if(!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            try
            {
                var recordedById = User.FindFirstValue(ClaimTypes.NameIdentifier);

                //uses foreach to loop all student because multple student's attendance marking is inserted to database
                foreach(var attendance in model.StudentAttendance)
                {
                    var studentId = attendance.Key; //Student id(1, 2, 3)
                    var marking = attendance.Value; //Present etc 
                    
                    string? excuseReason = null;

                    if(marking == "Excuse" && model.ExcuseReason != null)
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
                        SecretaryAssignmentId = model.SecretaryAssignmentId,
                        SectionSubjectId = model.SectionSubjectId,
                        Remarks = model.Remarks,
                        CreatedAt = DateTime.Now
                    };

                    _context.Attendances.Add(newAttendance);
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Attendance saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Something went wrong" });
            }
        }
        public async Task<IActionResult> Logout()
        {
            //var user = await _userManager.GetUserAsync(User);

            //var userId = user?.Id;
            //var schoolId = user?.SchoolId ?? 0;
            //var username = user?.UserName;

            var userInfo = await GetCurrentUserInfo();

            await _signInManager.SignOutAsync();

            await _logService.LogActivity(
                actionType: "Logout",
                entityName: "User",
                entityId: userInfo.userId,
                userId: userInfo.userId,
                schoolId: userInfo.schoolId,
                details: $"User {userInfo.userName} logged out successfully!",
                username: userInfo.userName
            );

            return RedirectToAction("Login", "Login");

        }
    }
}
