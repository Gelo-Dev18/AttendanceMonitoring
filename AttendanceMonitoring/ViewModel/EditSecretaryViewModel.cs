using AttendanceMonitoring.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace AttendanceMonitoring.ViewModel
{
    public class EditSecretaryViewModel
    {
        public string Email {get; set;}
        public string? NewPassword { get; set; }
        public int SchoolId { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string Sex { get; set; }
        public IFormFile? imageFile{ get; set; }
        public string? imageFilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SectionId { get; set; }
        public List<SelectListItem>? AvailableGradeSection { get; set; }
        public SecretaryAssignment? secretaryClass { get; set; }

    }
}
