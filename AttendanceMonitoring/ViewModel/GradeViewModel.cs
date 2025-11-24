using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class GradeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Grade Level is Required!")]
        [Range(1, 12, ErrorMessage = "Grade Level must be between 1 and 12")]
        public int GradeLevel { get; set; }
        [Required(ErrorMessage = "Category is Required!")]
        public string Category { get; set; }

    }
}
