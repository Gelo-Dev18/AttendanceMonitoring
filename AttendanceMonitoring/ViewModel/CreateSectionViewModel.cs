using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class CreateSectionViewModel
    {
        [Required]
        public int GradesId { get; set; }
        [Required]
        public string SectionName { get; set; }
        public string? Track { get; set; }
    }
}
