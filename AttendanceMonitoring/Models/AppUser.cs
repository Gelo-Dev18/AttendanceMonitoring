using Microsoft.AspNetCore.Identity;

namespace AttendanceMonitoring.Models
{
    public class AppUser : IdentityUser //Appuser inherits from IdentityUser
    {
        public string SchoolId { get; set; }
        public int? LRN { get; set; }
        public int? EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set;}
        public string LastName { get; set; }
        public string Sex { get; set; }
        public string? positionTitle { get; set; }
        public string? imageFilePath { get; set; }
        public byte[]? imageFileData { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<TeacherAssignment> TeachingAssignments { get; set; }
        public ICollection<SecretaryAssignment> SecretariesAssignments { get; set; }
        

    }
}
