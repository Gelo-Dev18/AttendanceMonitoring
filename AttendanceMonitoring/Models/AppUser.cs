using Microsoft.AspNetCore.Identity;

namespace AttendanceMonitoring.Models
{
    public class AppUser : IdentityUser
    {
        public int SchoolId { get; set; }
        public int LRN { get; set; }
        public int EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set;}
        public string LastName { get; set; }
        public string Sex { get; set; }
        public string positionTitle { get; set; }
        public string? imageFilePath { get; set; }
        public byte[]? imageFileData { get; set; }
        public DateTime CreatedAt { get; set; }
        //public DateTime UpdatedAt { get; set; }

    }
}
