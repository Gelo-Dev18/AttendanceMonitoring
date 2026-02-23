using AttendanceMonitoring.Models;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel.Teacher
{
    public class MyClassesViewModel
    {
        public int LRN { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Sex { get; set; }
        public string positionTitle { get; set; }
        //[Required]
        public IFormFile? imageFile { get; set; }
        public string? imageFilePath { get; set; }
        public string? currentAcademicYear { get; set; }
        public string? currentPeriod { get; set; }

        public List<TeacherAssignment> teacherAssignments { get; set; } = new List<TeacherAssignment>();
    }
}
