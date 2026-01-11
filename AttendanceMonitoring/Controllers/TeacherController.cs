using AttendanceMonitoring.Data;
using AttendanceMonitoring.Models;
using AttendanceMonitoring.ViewModel.Teacher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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

            if (TeachersClass != null)
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
            
            var model = new AttendanceViewModel()
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
                    SectionSubjectId = model.SelectedClassId, //1. BAGO
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

                    //Get attendanc Record
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
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Login");
        }
    }
}
