using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class SubjectViewModel
    {
        [Required(ErrorMessage = "Subject Code is Required!")]
        public string SubjectDescription { get; set; }
    }
}
