using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class SectionViewModel
    {
        [Required]
        public int GradesId { get; set; }
        [Required]
        public string SectionName { get; set; }
        public string? Track { get; set; }
        public string? TVLProgram { get; set; }

        public List<SelectListItem> AvailableGrades { get; set; } //SelectListItem design for creating dropdown list
    }
}
