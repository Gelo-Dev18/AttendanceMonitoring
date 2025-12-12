using AttendanceMonitoring.Models;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel.Secretary
{
    public class AttendanceViewModel
    {
        //public List<SecretaryAssignment> secretaryClass { get; set; } = new List<SecretaryAssignment>();
        // For Teacher (multiple classes)
        //public List<TeacherAssignment>? TeacherClasses { get; set; }
        public SecretaryAssignment? SecretaryClass { get; set; }//single object cause secretary has only 1 assignment
        public List<StudentSectionAssignment> Students { get; set; } = new List<StudentSectionAssignment>();
        public int? SecretaryAssignmentId { get; set; }
        public int CurrentAcademicPeriodId { get; set; }
        public string YearLevel { get; set; }
        public string GradingPeriod { get; set; }

        [Required]
        public Dictionary<int, string> ExcuseReason { get; set; } = new Dictionary<int, string>();
    }
}
