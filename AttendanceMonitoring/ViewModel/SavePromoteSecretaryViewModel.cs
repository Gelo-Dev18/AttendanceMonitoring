using Microsoft.AspNetCore.Mvc.Rendering;

namespace AttendanceMonitoring.ViewModel
{
    public class SavePromoteSecretaryViewModel
    {
        public int SectionId { get; set; }

        public List<SelectListItem>? AvailableGradeSection { get; set; }
        public int SchoolId { get; set; }
    }
}
