using AttendanceMonitoring.Data;
using AttendanceMonitoring.Models;
using AttendanceMonitoring.ViewModel.Teacher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        public TeacherController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.context = context;
            this.environment = environment;

        }

        //[Authorize(Roles = "Teacher")]
        public IActionResult TeacherHome()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> _Attendance(int? selectedClassId)  // WALANG parameter for Id of teacher kase gumamit na ng ClaimTypes
        {

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }
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

            //Get Current default academic period
            var currentAcademicPeriod = await context.AcademicPeriods.FirstOrDefaultAsync(ap => ap.IsDefault == 1);

            //Exclude student that already has an attendance record
            var alreadyRecordedAttendance = await context.Attendances
                                            .Where(a => a.RecordedById == teacherId && a.AcademicPeriod == currentAcademicPeriod)
                                            .Select(ta => ta.TeacherAssignmentId)
                                            .ToListAsync();


            //Query to fetch assign Grade & Section - Subjects on a specific teacher
            var TeachersClass = await context.TeacherAssignments
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Subject)
                .Include(sn => sn.SectionSubject.Section)
                    .ThenInclude(g => g.Grade)
                .Where(s => s.TeacherId == teacherId) //Filter to this teacher only
                .Where(s => !alreadyRecordedAttendance.Contains(s.Id))
                .OrderBy(s => s.SectionSubject.Section.Grade)
                .ToListAsync();

            if (TeachersClass != null)
            {
                //1.Create List to store classes that should be removed
                var classesToRemove = new List<TeacherAssignment>();

                //var sectionId = TeacherClass.SectionSubjectId; wont work because TeacherClass is a list
                //Need to use loop to access SectionSubjectId
                foreach (var teacherClass in TeachersClass)
                {
                    var sectiondId = teacherClass.SectionSubject.SectionId;

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
                                            && a.RecordedById == secretaryAssignment.SecretaryId
                                            && a.AcademicPeriod == currentAcademicPeriod);
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
            }

            ///Initialize students variable as null (wala pang value) 
            List<StudentSectionAssignment> students = null;
            //int? assignmentId = null;

            //If a class is selected, get the students
            if (selectedClassId.HasValue)
            {
                //Get the specific class that was selected
                var selectedClass = TeachersClass.FirstOrDefault(tc => tc.Id == selectedClassId.Value);
                
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
            
            var model = new AttendanceViewModel()
            {
                teacherClass = TeachersClass, //all teacher's class
                SelectedClassId = selectedClassId, //Selected class (null)
                Students = students, //students in selected class(null)
                TeacherAssignmentId = selectedClassId,
                CurrentAcademicPeriodId = currentAcademicPeriod?.Id ?? 1,
                YearLevel = currentAcademicPeriod.Year,
                GradingPeriod = currentAcademicPeriod.GradingPeriod
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAttendance(SaveAttendanceViewModel model, int? selectedClassId)
        {
            if(!model.TeacherAssignmentId.HasValue || model.TeacherAssignmentId == 0)
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

            foreach(var attendance in model.StudentAttendance)
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
                    SecretaryAssignmentId = null,
                    Remarks = model.Remarks,
                    CreatedAt = DateTime.Now

                };
                context.Attendances.Add(newAttendance);
            }

            await context.SaveChangesAsync();
            model.SelectedClassId = null;

            return Json(new { success = true, message = "Attendance saved successfully!" });
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Login");
        }
    }
}
