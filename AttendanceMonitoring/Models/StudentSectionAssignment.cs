namespace AttendanceMonitoring.Models
{
    public class StudentSectionAssignment
    {
        public int Id { get; set; }
        public int? StudentId { get; set; }
        public int SectionId { get; set; }
        public int? AcademicPeriodId { get; set; } //This is needed for archive so it can filter assigned history

        public DateTime CreatedAt { get; set; }

        //For Soft Delete function
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        //Many Student has one studentsectionassignment
        //Nullable na parehas yung section at secretary para sa soft deletion
        public Student? Student { get; set; }
        public Section? Section { get; set; }

        public AcademicPeriod AcademicPeriod { get; set; }
        //BAGONG DAGDAG FOR REFACTOR ABOUT STUDENT
        public virtual ICollection<Attendance> StudentAttendances { get; set; }



    }
}
