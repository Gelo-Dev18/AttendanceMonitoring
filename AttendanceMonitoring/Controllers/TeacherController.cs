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
        private readonly SignInManager<AppUser> signInManager;
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
                return RedirectToAction("Login", "Account");
            }

            //Query to fetch assign Grade & Section - Subjects on a specific teacher
            var TeachersClass = await context.TeacherAssignments
                .Include(ta => ta.SectionSubject)
                    .ThenInclude(ss => ss.Subject)
                .Include(sn => sn.SectionSubject.Section)
                    .ThenInclude(g => g.Grade)
                .Where(s => s.TeacherId == teacherId)               
                .OrderBy(s => s.Id)
                .ToListAsync();

            ///Initialize students variable as null (wala pang value)
            List<StudentSectionAssignment> students = null;

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
                Students = students //students in selected class(null)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttendance()
        {
            return Json(new { success = true, message = "Attendance saved successfully!" });
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Login");
        }
    }
}
