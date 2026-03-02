using AttendanceMonitoring.Models;

namespace AttendanceMonitoring.ViewModel.Teacher
{
    public class TeacherHomeViewModel
    {
        public string YearLevel { get; set; }
        public string Grading { get; set; }
        public int Status { get; set; }

        public List<TeacherAssignment> teacherAssignments { get; set; }
    }
}
