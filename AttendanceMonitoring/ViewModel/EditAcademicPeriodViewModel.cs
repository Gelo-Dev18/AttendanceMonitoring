using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class EditAcademicPeriodViewModel
    {
        [Required(ErrorMessage = "Academic Year is Required!")]
        public string Year { get; set; }

        [Required(ErrorMessage = "Grading Period is Required!")]
        public string GradingPeriod { get; set; }

        public int IsDefault { get; set; }
        public int Status { get; set; }
    }
}
