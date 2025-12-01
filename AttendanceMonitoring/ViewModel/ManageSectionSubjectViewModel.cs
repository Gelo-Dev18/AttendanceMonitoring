using AttendanceMonitoring.Models;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class ManageSectionSubjectViewModel
    {
        public int SubjectId { get; set; }
        public int SectionId { get; set; }
        public int GradesId { get; set; }
        [Required(ErrorMessage = "Subject Description is Required!")]
        public string SubjectDescription { get; set; }

        [Required(ErrorMessage = "Category is Required!")]
        public string Category { get; set; }
        public string? TVLProgram { get; set; }
        public Section Section { get; set; }

        public int DataCount { get; set; }
        public List<SectionSubject> assignedList { get; set; } = new List<SectionSubject>();
        public List<Section> otherSectionWithSameGrade { get; set; } = new List<Section>();

        public List<Subject> AvailableSubject { get; set; } = new List<Subject>();

        
    }

}
