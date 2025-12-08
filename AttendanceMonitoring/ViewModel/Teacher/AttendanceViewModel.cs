using AttendanceMonitoring.Models;

namespace AttendanceMonitoring.ViewModel.Teacher
{
    public class AttendanceViewModel
    {
        public List<TeacherAssignment> teacherClass { get; set; } = new List<TeacherAssignment>();
        public List<StudentSectionAssignment> Students { get; set; } = new List<StudentSectionAssignment>();
        public int? SelectedClassId { get; set; }

    }
}
