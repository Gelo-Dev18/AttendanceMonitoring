namespace AttendanceMonitoring.Models
{
    public class Grade
    {
        public int Id { get; set; }
        public int GradeLevel { get; set; }
        public string Category { get; set; }

        public DateTime CreatedAt { get; set; }

        //For Soft Delete function
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        //This means Grade has many sections
        public ICollection<Section> Sections { get; set; } //Collection of Object, One to Many relation
    }
}
