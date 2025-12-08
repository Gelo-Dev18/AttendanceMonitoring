using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel.Teacher
{
    public class SaveAttendanceViewModel
    {
        [Required]
        public int AcademicPeriodId{ get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        // Either TeacherAssignmentId OR SecretaryAssignmentId
        public int? TeacherAssignmentId { get; set; }
        public int? SecretaryAssignmentId { get; set; }

        //Dictionary: StudentId -> AttendanceMarking
        // Example: { "STU001": "Present", "STU002": "Absent", "STU003": "Late" }
        ///to store and manage a collection of key-value pairs 
        [Required] 
        public Dictionary<int, string> StudentAttendance { get; set; } = new Dictionary<int, string>();

        [MaxLength(500)]
        public string? ExcuseReason { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

    }
}
