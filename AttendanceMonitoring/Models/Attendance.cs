namespace AttendanceMonitoring.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int AcademicPeriodId { get; set; }


        public string AttendanceMarking { get; set; } //Present, Late, Absent, Excuse
        public string? ExcuseReason { get; set; }//If Excuse is selected on attendanceMarking

        public DateTime AttendanceDate { get; set; }//Actual date of attendance
        
        public int StudentId { get; set; }
        public string RecordedById { get; set; }// Recorded by (Teacher or Secretary)

        //public int SubjectId { get; set; }

        public int? TeacherAssignmentId { get; set; }
        public int? SecretaryAssignmentId { get; set; }
        public int? SectionSubjectId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? Remarks { get; set; }

        /// <summary>
        /// Foreign Keys //NAVIGATION PROPERTY FOR LAZY LOADING
        /// THey are all MAny to One relationship
        /// </summary>
        /// 

        //NEED NA LAHAT NG NAVIGATION PROPERTIES NA MAGING NULLABLE PARA GUMANA ANG SOFT DELETION, KASE SI SOFT DELETION USES QUERY FILTER
        //QUERY FILTERS CAN HIDE ENTITIES FROM NAVIGATION PROPERTIES
        //PAG NON-NULLABLE ANG MGA NP, EF CORE EXPECTS IT TO ALWAYS HAVE VALUE
            
        //public Student Student { get; set; } Option 1 : just public (older Style) used in .net 6 and below
        public virtual Student Student { get; set; } //Option 2: public virtual na (modern Ef Core best practice)
                                                     //other info: SINGEL OBJECT (one to one or many to one) kaya walang ICollection
                                                     //Ibig sabihin isang attendance lang kada isang student
        public virtual AppUser RecordedBy { get; set; } 
        public virtual TeacherAssignment TeacherAssignment { get; set; }
        public virtual AcademicPeriod AcademicPeriod { get; set; }
        public virtual SecretaryAssignment SecretaryAssignment { get; set; } //SINGLE - One Attendance links to ONE secretaryAssignment so ICollection didn't use
        public virtual SectionSubject SectionSubject { get; set; }
        //public virtual ICollection<SecretaryAssignment> SecretaryAssignments { get; set; } // Didn't use cause it is a collection. Sa SecretaryASssignments Class to gagamitin
                                                                                           //Kase One SecretaryAssingnemnt has Many Attendances
        

    }
}
