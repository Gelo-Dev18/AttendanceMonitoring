using AttendanceMonitoring.Models;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class ViewTeacherViewModel
    {

        public string TeacherId { get; set; }
        public int SectionId { get; set; }


        [Required(ErrorMessage = "Email is Required!"), MaxLength(60)]
        [EmailAddress(ErrorMessage = "Use a valid email with an" + " '@' " + "sign")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 Characters")]
        public string? NewPassword { get; set; }

        [Required(ErrorMessage = "School Id is Required!")]
        public int SchoolId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required, MaxLength(30)]
        public string FirstName { get; set; }

        public string? MiddleName { get; set; }

        [Required, MaxLength(30)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Please Select Male or Female")]
        public string Sex { get; set; }

        [Required, MaxLength(30)]
        public string positionTitle { get; set; }
        //[Required]
        public IFormFile? imageFile { get; set; }
        public string? imageFilePath { get; set; }

        public List<TeacherAssignment> teacherAssignments { get; set; } = new List<TeacherAssignment>();
        public List<SectionSubject> SectionSubjects { get; set; } = new List<SectionSubject>();

    }
}
