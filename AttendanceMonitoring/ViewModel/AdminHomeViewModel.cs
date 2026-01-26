using AttendanceMonitoring.Models;

namespace AttendanceMonitoring.ViewModel
{
    public class AdminHomeViewModel
    {
        public int StudentCount { get; set; }
        public int TeacherCount { get; set; }
        public int SecretaryCount { get; set; }
        public int SubjectCount { get; set; }
        public int ReportCount { get; set; }
        public string LastName { get; set; }
        public IFormFile? imageFile { get; set; }
        public string? imageFilePath { get; set; }
    }
}
