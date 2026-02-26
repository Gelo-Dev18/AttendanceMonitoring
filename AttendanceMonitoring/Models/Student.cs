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

        //For Soft Delete function
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        //Direct relationship: One-to-Many(Student → StudentSectionAssignment)
        //Overall pattern: Many-to-Many (Student ↔ Section through linking table)
        public ICollection<StudentSectionAssignment> SectionAssignments { get; set; }

        //public ICollection<Attendance> Attendances { get; set; }
    }
}
