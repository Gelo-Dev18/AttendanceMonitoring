using AttendanceMonitoring.Models;

namespace AttendanceMonitoring.ViewModel.Teacher
{
    public class SelfAssignViewModel
    {
        public string TeacherId { get; set; }
        public List<SectionSubject> SectionSubjects { get; set; } = new List<SectionSubject>();
    }
}
