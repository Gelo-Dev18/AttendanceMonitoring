using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class SubjectViewModel
    {
        [Required(ErrorMessage = "Subject Description is Required!")]
        public string SubjectDescription { get; set; }

        [Required(ErrorMessage = "Category is Required!")]
        public string Category { get; set; }

    }
}
