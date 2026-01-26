using AttendanceMonitoring.Models;

using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class ManageAccountViewModel
    {
        [Required]
        public int LRN { get; set; }
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 Characters")]
        public string? NewPassword { get; set; }
        [Required]
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        [Required]
        public string LastName { get; set; }
        public IFormFile? imageFile { get; set; }
        public string? imageFilePath { get; set; }
    }
}
