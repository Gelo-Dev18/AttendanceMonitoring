using AttendanceMonitoring.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.ViewModel
{
    public class EditStudentViewModel
    {
        [Required]
        public string LRN { get; set; }
        [Required]
        public string FirstName { get; set; }
        public string? MiddelName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Sex { get; set; }
        public string? imageFilePath { get; set; }
        public IFormFile? imageFile { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SectionId { get; set; }
        public int? AcademicPeriodId { get; set; }

        //Get total Students
        public int StudentCount { get; set; }
        public List<SelectListItem>? AvailableGradeSection { get; set; }
        /// <summary>
        /// Para Sa Viewing
        /// </summary>
        public StudentSectionAssignment? studentClass { get; set; } //isa lang kailangan so single object. Kumbaga si Student is isa lang grade section nya
                                                                    //di tulad kapag naka Icollection is kapag maraming assignment like sa college kapag irreg mraming ibang section or subject

        

    }
}
