using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "School Id is Required!")]
        [Display(Name = "School ID")]
        public int SchoolId { get; set; }

        //[Required(ErrorMessage = "Email address is required!")]
        [EmailAddress]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required!")]
        [DataType(DataType.Password)]
        public string Password { get; set; }


    }
}
