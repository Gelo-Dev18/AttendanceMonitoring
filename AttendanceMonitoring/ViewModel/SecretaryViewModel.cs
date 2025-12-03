using AttendanceMonitoring.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class SecretaryViewModel
    {
        [Required(ErrorMessage = "Email is Required!"), MaxLength(60)]
        [EmailAddress(ErrorMessage = "Use a valid email with an '@' sign")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Password is Required!"), MaxLength(60)]
        public string Password { get; set; }

        [Required(ErrorMessage = "School Id is Required!")]
        public int SchoolId { get; set; }

        [Required, MaxLength(30)]               
        public string FirstName { get; set; }

        [MaxLength(30)]
        public string? MiddleName { get; set; }

        [Required, MaxLength(30)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Please Select Male or Female")]
        public string Sex { get; set; }

        public IFormFile? imageFile { get; set; }
        public int SectionId { get; set; }

        public List<SelectListItem>? AvailableGradeSection { get; set; }



    }
}
