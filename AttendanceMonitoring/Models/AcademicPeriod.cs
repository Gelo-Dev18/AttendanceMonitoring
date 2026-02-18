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

        //For Soft Delete function
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual ICollection<Attendance> Attendances { get; set; }
        public virtual ICollection<TeacherAssignment> TeacherAssignments { get; set; }
        public virtual ICollection<SecretaryAssignment> SecretaryAssignments { get; set; }
        public virtual ICollection<StudentSectionAssignment> StudentSectionAssignments{ get; set; }


    }
}
