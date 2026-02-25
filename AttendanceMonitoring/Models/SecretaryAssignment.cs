namespace AttendanceMonitoring.Models
{
    public class SecretaryAssignment
    {
        public int Id { get; set; }
        public string SecretaryId { get; set; }
        public int SectionId { get; set; }
        //Nullable muna para di mag cause ng conflict sa database. set non-nullable kapag
        public int? AcademicPeriodId { get; set; } //This is needed for archive so it can filter assigned history

        public DateTime CreatedAt { get; set; }

        //For Soft Delete function
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; } //For class promotion if Secretary moves to new grade level
        public DateTime StartDate { get; set; }

        //Navigation property
        //Nullable na parehas yung section at secretary para sa soft deletion
        public Section? Section { get; set; } 
        public AppUser? Secretary { get; set; }

        //Colection - One SecretaryAssignments, Many attendances
        public virtual ICollection<Attendance> SecretaryAttendances { get; set; }
        public AcademicPeriod AcademicPeriod { get; set; }

    }
}
