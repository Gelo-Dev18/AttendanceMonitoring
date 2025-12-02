namespace AttendanceMonitoring.Models
{
    public class SecretaryAssignment
    {
        public int Id { get; set; }
        public string SecretaryId { get; set; }
        public AppUser Secretary { get; set; }
        public int SectionId { get; set; }
        public Section Section { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
