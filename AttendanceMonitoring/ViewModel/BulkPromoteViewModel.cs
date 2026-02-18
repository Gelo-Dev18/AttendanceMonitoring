using AttendanceMonitoring.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AttendanceMonitoring.ViewModel
{
    public class BulkPromoteViewModel
    {
        public int SectionId { get; set; }

        public List<int> StudentIds { get; set; }
        public List<StudentSectionAssignment> Students { get; set; }
        public List<SelectListItem>? AvailableGradeSection { get; set; }
        //"Use IEnumerable for reading/displaying, use List/Array when you need to modify or access by index"
        public IEnumerable<string> currentSections { get; set; }
        //public string currentSections { get; set; }

    }
}
