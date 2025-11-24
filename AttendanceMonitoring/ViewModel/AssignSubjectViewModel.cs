using AttendanceMonitoring.Models;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class AssignSubjectViewModel
    {
        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public bool isActive { get; set; }
        public string SelectedCategory { get; set; }
        [MinLength(1, ErrorMessage = "Please select atleast 1 subject")]
        public List<int> SelectedSubjects { get; set; } = new List<int>();
        public List<Subject> AvailableSubject { get; set; } = new List<Subject>();
        public List<SectionSubject> assignedList { get; set; } = new List<SectionSubject>();
        //Always remember from the word "View". Kaya lang may viewmodel for display of data
    }
}
