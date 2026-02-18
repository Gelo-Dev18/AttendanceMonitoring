namespace AttendanceMonitoring.Models
{
    public class TeacherAssignment
    {
        public int Id { get; set; }
        public string TeacherId { get; set; }
        public int SectionSubjectId { get; set; }
        //Nullable muna para di mag cause ng conflict sa database. set non-nullable kapag
        public int? AcademicPeriodId { get; set; } //This is needed for archive so it can filter assigned history
        public DateTime CreatedAt { get; set; }

        //For Soft Delete function
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        //Navigation Properties
        public AppUser Teacher { get; set; }
        public SectionSubject SectionSubject { get; set; }
        public AcademicPeriod AcademicPeriod { get; set; }
        public virtual ICollection<Attendance> TeacherAttendances { get; set; }

    }
}
