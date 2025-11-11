using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class EditSectionViewModel
    {
        [Required(ErrorMessage = "Grade Level is required")]
        [Display(Name = "Section Names")]
        public int GradesId { get; set; }

        [Required(ErrorMessage = "Setion name is required atleast 1")]
        [Display(Name = "Section Names")]
        public string SectionName { get; set; }

        [Display(Name = "Track")]
        public string? Track { get; set; }

        public List<SelectListItem>? AvailableGrades { get; set; } //SelectListItem design for creating dropdown list
    }
}
