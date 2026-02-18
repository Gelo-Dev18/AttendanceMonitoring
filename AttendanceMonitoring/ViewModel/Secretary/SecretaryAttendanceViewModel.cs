using AttendanceMonitoring.Models;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel.Secretary
{
    public class SecretaryAttendanceViewModel
    {
        //public List<SecretaryAssignment> secretaryClass { get; set; } = new List<SecretaryAssignment>();
        // For Teacher (multiple classes)
        //public List<TeacherAssignment>? TeacherClasses { get; set; }
        //nabago
        //public SecretaryAssignment? SecretaryClass { get; set; }//single object cause secretary has only 1 assignment
        public List<SectionSubject> SecretaryClass { get; set; } = new List<SectionSubject>();
        public int? SelectedSubjectId { get; set; } //bago
        public int? SectionSubjectId { get; set; }
        public int SubjectId { get; set; }
        public List<StudentSectionAssignment> Students { get; set; } = new List<StudentSectionAssignment>();
        public int? SecretaryAssignmentId { get; set; }
        public int CurrentAcademicPeriodId { get; set; }
        public string YearLevel { get; set; }
        public string GradingPeriod { get; set; }
        public bool IsAttendanceFinished { get; set; }
        public bool IsStarted { get; set; }



        [Required]
        public Dictionary<int, string> ExcuseReason { get; set; } = new Dictionary<int, string>();
    }
}
