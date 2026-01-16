using AttendanceMonitoring.Models;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel.Teacher
{
    public class AttendanceViewModel
    {
        public List<TeacherAssignment> teacherClass { get; set; } = new List<TeacherAssignment>();
        public List<StudentSectionAssignment> Students { get; set; } = new List<StudentSectionAssignment>();
        public int? SelectedClassId { get; set; }
        public int? SectionSubjectId { get; set; } //1. BAGO


        public int? TeacherAssignmentId { get; set; }
        public int SubjectId { get; set; }
        public int AcademicStatusId { get; set; }
        public bool IsStarted { get; set; }

        public int CurrentAcademicPeriodId { get; set; }
        public string YearLevel { get; set; }
        public string GradingPeriod { get; set; }
        public bool IsAttendanceFinished { get; set; }

        [Required]
        public Dictionary<int, string> ExcuseReason { get; set; } = new Dictionary<int, string>();
    }
}
