namespace AttendanceMonitoring.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public string employeeId { get; set; }
        public string firstName { get; set; }
        public string? middelName { get; set; }
        public string lastName { get; set; }
        public int positionTitle { get; set; }
        public string? imageFile { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
