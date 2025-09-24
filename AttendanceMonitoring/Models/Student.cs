namespace AttendanceMonitoring.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string studentId { get; set; }
        public string LRN { get; set; }
        public string firstName { get; set; }
        public string? middelName { get; set; }
        public string lastName { get; set; }
        public int yearLevel { get; set; }
        public string sex { get; set; }
        public string? imageFile { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
