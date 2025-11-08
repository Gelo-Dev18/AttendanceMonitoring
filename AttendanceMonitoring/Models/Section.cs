namespace AttendanceMonitoring.Models
{
    public class Section
    {
        public int Id { get; set; }
        public int GradesId { get; set; }
        public string SectionName { get; set; } 
        public string? Track { get; set; }
        public DateTime CreatedAt { get; set; }
        public Grade Grade { get; set; } //Navigation Property - REQUIRED for .Include()! // Pag Public Virtual, no need include sa controller

    }
}
