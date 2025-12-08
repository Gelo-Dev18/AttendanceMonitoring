namespace AttendanceMonitoring.Models
{
    public class TeacherAssignment
    {
        public int Id { get; set; }
        public string TeacherId { get; set; }
        public int SectionSubjectId { get; set; }
        public DateTime CreatedAt { get; set; }
        public AppUser Teacher { get; set; }
        public SectionSubject SectionSubject { get; set; }
        public virtual ICollection<Attendance> TeacherAttendances { get; set; }

    }
}
