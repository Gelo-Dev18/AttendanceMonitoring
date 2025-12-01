using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.Models
{
    public class Student
    {
        public int Id { get; set; }
        [Required]
        public int LRN { get; set; }
        [Required]

        public string FirstName { get; set; }
        public string? MiddelName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Sex { get; set; }
        public string? imageFilePath { get; set; }
        public byte[]? imageFileData { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<StudentSectionAssignment> SectionAssignments { get; set; }
    }
}
