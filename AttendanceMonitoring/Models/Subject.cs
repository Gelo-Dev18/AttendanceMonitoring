using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoring.Models
{
    public class Subject
    {
        public int Id { get; set; }
        [Required]
        public string SubjectDescription { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
