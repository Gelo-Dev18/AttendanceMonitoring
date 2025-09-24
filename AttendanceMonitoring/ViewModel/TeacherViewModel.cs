using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class TeacherViewModel
    {
        [Required(ErrorMessage ="Email is Required!"), MaxLength(60)]
        [EmailAddress(ErrorMessage = "Use a valid email with an" + " '@' " + "sign")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage ="Password is Required!"), MaxLength(60)]
        public string Password { get; set; }
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
    }
}
