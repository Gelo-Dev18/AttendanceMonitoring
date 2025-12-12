using AttendanceMonitoring.Data;
using AttendanceMonitoring.Models;
using AttendanceMonitoring.ViewModel.Secretary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
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

        public SecretaryController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            this._signInManager = signInManager;
            this._userManager = userManager;
            this._context = context;
            this._environment = environment;
        }
        public IActionResult SecretaryHome()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> _Attendance(int? defaultClassId)
        {
            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            //Get the user id that is currently login
            var secretaryId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(secretaryId))
            {
                return RedirectToAction("SecretaryHome", "Secretary");
            }

            //Get Current default academic Period
            var currentAcademicPeriod = await _context.AcademicPeriods.FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            //Get Secretary's Assignment
            var SecretaryClass = await _context.SecretaryAssignments
                                .Include(sa => sa.Section)
                                    .ThenInclude(s => s.Grade)
                                .Include(sn => sn.Section.SectionSubjects)
                                    .ThenInclude(ss => ss.Subject)
                                .Where(s => s.SecretaryId == secretaryId)
                                .FirstOrDefaultAsync();

            //If Secretary has an assignment, check if attendance already Exists
            if(SecretaryClass != null)
            {
                //Single object code to get sectionId
                var sectionId = SecretaryClass.SectionId;

                //check if secretary already recorded attendance 
                var secretaryRecorded = await _context.Attendances
                    .AnyAsync(a => a.SecretaryAssignmentId == SecretaryClass.Id
                                && a.RecordedById == secretaryId
                                && a.AcademicPeriod == currentAcademicPeriod);

                //Get the teacher's assignment for this same section that the secretary is assigned
                var teacherAssignment = await _context.TeacherAssignments
                                        .Include(ta => ta.SectionSubject)
                                        .Where(ta => ta.SectionSubject.SectionId == sectionId)
                                        .FirstOrDefaultAsync();

                //1.If teacher assignment doesn't exist → Keep it as false (no need to check)
                //bool teacherRecorded = false; → Default assumption: "Teacher hasn't recorded yet"
                bool teacherRecorded = false;

                if(teacherAssignment != null)
                {
                    //2. If may teacher Record dun palang gagana ang if else na ito -> only check if teacher assignment exist
                    //check if teacher already conducted for attendance for this section
                    teacherRecorded = await _context.Attendances
                        .AnyAsync(a => a.TeacherAssignmentId == teacherAssignment.Id
                                    && a.RecordedById == teacherAssignment.TeacherId
                                    && a.AcademicPeriod == currentAcademicPeriod);
                }

                //If either secretary or teacher already recorded attendance for this section
                if(secretaryRecorded || teacherRecorded)
                {
                    SecretaryClass = null;
                }
            }


            List<StudentSectionAssignment> students = null;

            if (SecretaryClass != null)
            {
                var sectionId = SecretaryClass.SectionId; //geet the actual Sectionid

                students = await _context.StudentSectionAssignments
                    .Include(ssa => ssa.Student)
                    .Include(ssa => ssa.Section)
                        .ThenInclude(s => s.Grade)
                    .Where(ssa => ssa.SectionId == sectionId)
                    .ToListAsync();
            }

            var model = new AttendanceViewModel()
            {
                SecretaryClass = SecretaryClass, //Single assignment
                Students = students, //students in that section 
                SecretaryAssignmentId = SecretaryClass?.Id, //Use the actual assignment ID
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
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Login");

        }
    }
}
