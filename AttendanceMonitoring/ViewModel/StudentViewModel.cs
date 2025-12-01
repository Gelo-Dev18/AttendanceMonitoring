using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class StudentViewModel
    {
        [Required]
        public int LRN { get; set; }
        [Required]
        public string FirstName { get; set; }
        public string? MiddelName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Sex { get; set; }
        public IFormFile? imageFile { get; set; }

        public int SectionId { get; set; }

        //Get total Students
        public int StudentCount { get; set; }
        public List<SelectListItem>? AvailableGradeSection { get; set; }
    }
}
