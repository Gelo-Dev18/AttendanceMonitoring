namespace AttendanceMonitoring.Models
{
    public class StudentSectionAssignment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int SectionId { get; set; }
        public DateTime CreatedAt { get; set; }

        //Many Student has one studentsectionassignment
        public Student Student { get; set; }
        public Section Section { get; set; }

    }
}
