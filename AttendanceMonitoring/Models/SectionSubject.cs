namespace AttendanceMonitoring.Models
{
    public class SectionSubject // Linking Table - Bridge for Section and Subject
    {
        public int Id { get; set; }
        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public DateTime CreatedAt { get; set; }

        //For Soft Delete function
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }


        public Section Section { get; set; } //Navigation Property
        public Subject Subject { get; set; } //Navigation Property

        public ICollection<TeacherAssignment> TeacherAssignments { get; set; }
        public ICollection<Attendance> SectionSubjectAttendance { get; set; }
    }
}
