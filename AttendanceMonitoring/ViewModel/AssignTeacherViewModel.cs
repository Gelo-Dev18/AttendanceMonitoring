using AttendanceMonitoring.Models;

namespace AttendanceMonitoring.ViewModel
{
    public class AssignTeacherViewModel
    {
        public string TeacherId { get; set; }
        public int SectionId { get; set; }

        //public string SectionSubjectId { get; set; }
        public List<SectionSubject> SectionSubjects { get; set; } = new List<SectionSubject>();

        //public List<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();


    }
}
