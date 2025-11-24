using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class EditSubjectViewModel
    {
        //[Required(ErrorMessage = "Subject Code is Required!")]
        //public string SubjectCode { get; set; }

        [Required(ErrorMessage = "Subject Code is Required!")]
        public string SubjectDescription { get; set; }

        [Required(ErrorMessage = "Category is Required!")]
        public string Category { get; set; }
    }
}
