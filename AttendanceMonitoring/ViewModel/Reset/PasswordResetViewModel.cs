using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel.Reset
{
    public class PasswordResetViewModel
    {
        public string SchoolId { get; set; }

        [Required(ErrorMessage = "New Password is Required!")]
        [DataType(DataType.Password)]
        [Compare("ConfirmNewPassword", ErrorMessage = "Password does not match")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Confirm New Password is Required!")]
        public string ConfirmNewPassword { get; set; }
    }
}
