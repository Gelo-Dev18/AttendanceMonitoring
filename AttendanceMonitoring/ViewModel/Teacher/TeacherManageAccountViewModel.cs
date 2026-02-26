using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel.Teacher
{
    public class TeacherManageAccountViewModel
    {
        [Required]
        public string LRN { get; set; }
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 Characters")]
        public string? NewPassword { get; set; }
        [Required]
        public string FirstName { get; set; }

        public string? MiddleName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Sex { get; set; }
        [Required]
        public string PositionTitle { get; set; }
        public IFormFile? imageFile { get; set; }
        public string? imageFilePath { get; set; }
    }
}
