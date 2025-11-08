namespace AttendanceMonitoring.Models
{
    public class Grade
    {
        public int Id { get; set; }
        public int GradeLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Section> Sections { get; set; }
    }
}
