using Microsoft.AspNetCore.Mvc.Rendering;

namespace AttendanceMonitoring.ViewModel
{
    public class PromoteSecretaryViewModel
    {
        public int SectionId { get; set; }

        public List<SelectListItem>? AvailableGradeSection { get; set; }
        public string SchoolId { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string Sex { get; set; }
    }
}
