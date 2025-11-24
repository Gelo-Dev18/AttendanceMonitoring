namespace AttendanceMonitoring.Models
{
    public class Section
    {
        public int Id { get; set; }
        public int GradesId { get; set; } //Grade Level!
        public string SectionName { get; set; } 
        public string? Track { get; set; }
        public string? TVLProgram { get; set; }
        public DateTime CreatedAt { get; set; }
        public Grade Grade { get; set; } //Navigation Property/Lazy Loading - REQUIRED for .Include()! // Pag Public Virtual, no need include sa controller
        
        //Section Relationship (Many-to-Many relationship through SectionSubject) 
        public ICollection<SectionSubject> SectionSubjects { get; set; }
    }
}
