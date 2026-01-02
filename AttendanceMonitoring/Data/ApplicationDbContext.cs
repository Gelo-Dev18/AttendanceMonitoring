using AttendanceMonitoring.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoring.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        //public DbSet<AttendanceMonitoring.Models.Student> Student { get; set; } = default!;

        public DbSet<AcademicPeriod> AcademicPeriods { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Teacher> Teacher { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<SectionSubject> SectionSubjects { get; set; } // Linking Table for Section and subject!
        public DbSet<StudentSectionAssignment> StudentSectionAssignments { get; set; }
        public DbSet<TeacherAssignment> TeacherAssignments { get; set; }
        public DbSet<SecretaryAssignment> SecretaryAssignments { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //GradeLevel - SectionName Relationship (One-to-Many)
            modelBuilder.Entity<Section>()
                .HasOne(s => s.Grade)          //Section has one Grade
                .WithMany(g => g.Sections)      //Grade has many Sections
                .HasForeignKey(s => s.GradesId)      //Foreign key
                .OnDelete(DeleteBehavior.Restrict);  // ← IMPORTANT! Prevent cascade delete

            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///// FOR SECTION SUBJECT ASSIGNMENT ///////

            modelBuilder.Entity<SectionSubject>()
                .HasIndex(ss => new { ss.SectionId, ss.SubjectId })
                .IsUnique();

            modelBuilder.Entity<SectionSubject>()
                .HasOne(ss => ss.Section)
                .WithMany(s => s.SectionSubjects)
                .HasForeignKey(ss => ss.SectionId)
                .OnDelete(DeleteBehavior.Cascade); //Deletes assignment when section deleted

            modelBuilder.Entity<SectionSubject>()
                .HasOne(ss => ss.Subject)
                .WithMany(j => j.SectionSubjects)
                .HasForeignKey(ss => ss.SubjectId)
                .OnDelete(DeleteBehavior.Restrict); //To Prevent deletion when subject is already assigned

            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///// FOR STUDENT ASSIGNMENT ///////

            modelBuilder.Entity<StudentSectionAssignment>()
                .HasIndex(ssa => new { ssa.StudentId, ssa.SectionId })
                .IsUnique();


            modelBuilder.Entity<StudentSectionAssignment>()
                .HasOne(ssa => ssa.Student)
                .WithMany(sa => sa.SectionAssignments)
                .HasForeignKey(ssa => ssa.StudentId)
                .OnDelete(DeleteBehavior.Cascade); //Deletes Student = delete assignment so that's why Cascade is okay.

            modelBuilder.Entity<StudentSectionAssignment>()
                .HasOne(ssa => ssa.Section)
                .WithMany(sa => sa.StudentAssignments)
                .HasForeignKey(ssa => ssa.SectionId)
                .OnDelete(DeleteBehavior.Restrict); //Restrict delete so if section is accidentally delete, it will be block 
                                                    //to protect students who are enrolled on a specific section that is being deleted

            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///// FOR SECRETARY ASSIGNMENT ///////

            modelBuilder.Entity<SecretaryAssignment>()
                .HasIndex(sa => new { sa.SecretaryId, sa.SectionId })
                .IsUnique();

            modelBuilder.Entity<SecretaryAssignment>()
                .HasOne(sa => sa.Secretary)
                .WithMany(s => s.SecretariesAssignments)
                .HasForeignKey(sa => sa.SecretaryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SecretaryAssignment>()
                .HasOne(sa => sa.Section)
                .WithMany(s => s.SecretaryAssignments)
                .HasForeignKey(sa => sa.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///// FOR TEACHER ASSIGNMENT ///////

            modelBuilder.Entity<TeacherAssignment>()
                .HasIndex(ta => new { ta.TeacherId, ta.SectionSubjectId })
                .IsUnique();

            modelBuilder.Entity<TeacherAssignment>()
                .HasOne(ta => ta.Teacher)
                .WithMany(t => t.TeachingAssignments)
                .HasForeignKey(ta => ta.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeacherAssignment>()
                .HasOne(ta => ta.SectionSubject)
                .WithMany(t => t.TeacherAssignments)
                .HasForeignKey(ta => ta.SectionSubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///// FOR TEACHER ASSIGNMENT ///////
            ///

            //ALWAYS CONFIGURE FOREIGN KEY IF A CLASS HAS A FOREIGN KEY (EX.Class Attendance has public int Academic Period - public virtual AcademicPeriod AcademicPeriod
            //foreign keys to Student table
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            //foreign keys to AppUser table
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.RecordedBy)
                .WithMany()
                .HasForeignKey(a => a.RecordedById)
                .OnDelete(DeleteBehavior.Restrict);

            //foreign keys to AppUser table
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.AcademicPeriod)
                .WithMany(ap => ap.Attendances) //(e.g., "Get all attendances for School Year 2024-2025")
                .HasForeignKey(a => a.AcademicPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            //Secretary Assignment Relationship
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.SecretaryAssignment)
                .WithMany(sa => sa.SecretaryAttendances)
                .HasForeignKey(a => a.SecretaryAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            //Teacher Assignment Relationship
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.TeacherAssignment)
                .WithMany(ta => ta.TeacherAttendances)
                .HasForeignKey(a => a.TeacherAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.SectionSubject)
                .WithMany(ss => ss.SectionSubjectAttendance)
                .HasForeignKey(a => a.SectionSubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<Attendance>()
            //    .HasOne(a => a.Su)

            //Unique index - prevent duplicate attendance on same day
            ///❌ You CANNOT have 2 attendance records with:
            ///Same StudentId
            ///Same AttendanceDate
            ///Same AcademicPeriodId
            ///AND same TeacherAssignment or SecretaryAssignment
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new
                {
                    a.StudentId,
                    a.AttendanceDate,
                    a.TeacherAssignmentId,
                    a.SecretaryAssignmentId,
                    a.AcademicPeriodId
                })
                .IsUnique();

            //↑This filter ensures unique constraint workds properly with nullable columns

            //Check Constraint - ensure EXACTLY ONE assignment (Teacher or Secretary not both)
            //Ensure na Exacly one person lang ang recorded na attendance
            modelBuilder.Entity<Attendance>()
                .ToTable(b => b.HasCheckConstraint(
                    "CK_Attendance_Assignment",
                    "([TeacherAssignmentId] IS NOT NULL AND [SecretaryAssignmentId] IS NULL)" +
                    "OR ([TeacherAssignmentId] IS NULL AND [SecretaryAssignmentId] IS NOT NULL)"
                    )
                );
        }

    }

}

