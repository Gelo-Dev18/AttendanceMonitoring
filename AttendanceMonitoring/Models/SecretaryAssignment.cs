namespace AttendanceMonitoring.Models
{
    public class SecretaryAssignment
    {
        public int Id { get; set; }
        public string SecretaryId { get; set; }
        public int SectionId { get; set; }
        public DateTime CreatedAt { get; set; }

        //Navigation property
        public Section Section { get; set; }
        public AppUser Secretary { get; set; }

        //Colection - One SecretaryAssignments, Many attendances
        public virtual ICollection<Attendance> SecretaryAttendances { get; set; }
    }
}
