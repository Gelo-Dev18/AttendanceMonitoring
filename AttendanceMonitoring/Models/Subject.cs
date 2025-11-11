using Microsoft.AspNetCore.Http.HttpResults;

namespace AttendanceMonitoring.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public string? SubjectCode { get; set; }
        public string SubjectDescription { get; set; }
        //public string Category { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
