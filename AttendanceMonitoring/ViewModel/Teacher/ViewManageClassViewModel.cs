using AttendanceMonitoring.Models;

namespace AttendanceMonitoring.ViewModel.Teacher
{
    public class ViewManageClassViewModel
    {
        public List<StudentSectionAssignment> Students { get; set; } = new List<StudentSectionAssignment>();
        public Section Section { get; set; }
        public Subject Subject { get; set; }
        //public List<Subject> Subject { get; set; } = new List<Subject>();


    }
}
