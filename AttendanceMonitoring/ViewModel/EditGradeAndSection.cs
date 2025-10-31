using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class EditGradeAndSectionViewModel
    {
        //public int Id { get; set; }

        [Required(ErrorMessage = "Grade Level is Required!")]
        [Range(1, 12, ErrorMessage = "Grade Level must be between 1 and 12")]
        public string GradeLevel { get; set; }

        [Required(ErrorMessage = "Setion name is required atleast 1")]
        [Display(Name = "Section Names")]
        public string SectionName { get; set; }

    }
}
