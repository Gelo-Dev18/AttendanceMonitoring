namespace AttendanceMonitoring.Models
{
    public class SectionSubject // Linking Table - Bridge for Section and Subject
    {
        public int Id { get; set; }
        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public DateTime CreatedAt { get; set; }


        public Section Section { get; set; } //Navigation Property
        public Subject Subject { get; set; } //Navigation Property

    }
}
