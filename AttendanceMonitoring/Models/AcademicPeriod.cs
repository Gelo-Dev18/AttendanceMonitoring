namespace AttendanceMonitoring.Models
{
    public class AcademicPeriod
    {
        public int Id { get; set; }
        public string Year { get; set; }
        public string GradingPeriod { get; set; }
        public int IsDefault { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<Attendance> Attendances { get; set; }
    }
}
