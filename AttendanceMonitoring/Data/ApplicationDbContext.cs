using AttendanceMonitoring.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Security.Claims;

namespace AttendanceMonitoring.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        //public DbSet<AttendanceMonitoring.Models.Student> Student { get; set; } = default!;

        public DbSet<AcademicPeriod> AcademicPeriods { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
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

        
        //public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        //{
        //    var userId = GetCurrentUserId();
        //    var schoolId = GetCurrentSchoolId();

        //    //var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
        //    var entries = ChangeTracker.Entries()
        //        .Where(e => e.State == EntityState.Added ||
        //                    e.State == EntityState.Modified ||
        //                    e.State == EntityState.Deleted)
        //        .ToList();

        //    foreach(var entry in entries)
        //    {
        //        var log = new ActivityLog
        //        {
        //            UserId = userId,
        //            SchoolId = schoolId,
        //            ActionType = entry.State.ToString(),
        //            EntityName = entry.Entity.GetType().Name,
        //            EntityId = GetPrimaryKeyValue(entry),
        //            Details = GetChangeDetails(entry),
        //            TimeActivityCommited = DateTime.UtcNow,
        //            CreatedAt = DateTime.UtcNow
        //        };

        //        ActivityLogs.Add(log);
        //    }

        //    return await base.SaveChangesAsync(cancellationToken);

        //}

        //private string GetCurrentUserId()
        //{
        //    var user = _httpContextAccessor.HttpContext?.User;
        //    if(user == null)
        //    {
        //        return "System";
        //    }

        //    return user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
        //}

        //private int GetCurrentSchoolId()
        //{
        //    var user = _httpContextAccessor.HttpContext?.User;
        //    var schoolIdClaim = user?.FindFirstValue("SchoolId");

        //    //return user.FindFirstValue("SchoolId") ?? "N/A";
        //    //convert string to int
        //    return int.TryParse(schoolIdClaim, out var schoolId) ? schoolId : 0;
        //}
        //private string GetPrimaryKeyValue(EntityEntry entry)
        //{
        //    var key = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        //    return key?.CurrentValue?.ToString() ?? "Unknown";
        //}
        //private string GetChangeDetails(EntityEntry entry)
        //{
        //    if(entry.State == EntityState.Added)
        //    {
        //        return "New record Added";
        //    }

        //    if(entry.State == EntityState.Deleted)
        //    {
        //        return "Record Deleted";
        //    }

        //    var changes = new List<string>();
        //    foreach(var property in entry.Properties)
        //    {
        //        if (property.IsModified)
        //        {
        //            changes.Add($"{property.Metadata.Name}: '{property.OriginalValue}' → '{property.CurrentValue}'");
        //        }
        //    }

        //    return string.Join(", ", changes);
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //GradeLevel - SectionName Relationship (One-to-Many)
            modelBuilder.Entity<Section>()
                .HasOne(s => s.Grade)          //Section has one Grade
                .WithMany(g => g.Sections)      //Grade has many Sections
                .HasForeignKey(s => s.GradesId)      //Foreign key
                .OnDelete(DeleteBehavior.Restrict)  // ← IMPORTANT! Prevent cascade delete
                .IsRequired(false); //this is needed so that the navigation can be null

            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///// FOR SECTION SUBJECT ASSIGNMENT ///////

            modelBuilder.Entity<SectionSubject>()
                .HasIndex(ss => new { ss.SectionId, ss.SubjectId })
                .IsUnique();

            modelBuilder.Entity<SectionSubject>()
                .HasOne(ss => ss.Section)
                .WithMany(s => s.SectionSubjects)
                .HasForeignKey(ss => ss.SectionId)
                //.OnDelete(DeleteBehavior.Cascade); //Deletes assignment when section deleted
                .OnDelete(DeleteBehavior.Restrict) //Deletes assignment when section deleted
                .IsRequired(false); //this is needed so that the navigation can be null


            modelBuilder.Entity<SectionSubject>()
                .HasOne(ss => ss.Subject)
                .WithMany(j => j.SectionSubjects)
                .HasForeignKey(ss => ss.SubjectId)
                .OnDelete(DeleteBehavior.Restrict) //To Prevent deletion when subject is already assigned
                .IsRequired(false); //this is needed so that the navigation can be null

            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///// FOR STUDENT ASSIGNMENT ///////

            modelBuilder.Entity<StudentSectionAssignment>()
                .HasIndex(ssa => new { ssa.StudentId, ssa.SectionId })
                .IsUnique();


            modelBuilder.Entity<StudentSectionAssignment>()
                .HasOne(ssa => ssa.Student)
                .WithMany(sa => sa.SectionAssignments)
                .HasForeignKey(ssa => ssa.StudentId)
                //.OnDelete(DeleteBehavior.Cascade); //Deletes Student = delete assignment so that's why Cascade is okay.
                .OnDelete(DeleteBehavior.Restrict) //Deletes Student = delete assignment so that's why Cascade is okay.
                .IsRequired(false); //this is needed so that the navigation can be null


            modelBuilder.Entity<StudentSectionAssignment>()
                .HasOne(ssa => ssa.Section)
                .WithMany(sa => sa.StudentAssignments)
                .HasForeignKey(ssa => ssa.SectionId)
                .OnDelete(DeleteBehavior.Restrict) //Restrict delete so if section is accidentally delete, it will be block 
                                                    //to protect students who are enrolled on a specific section that is being deleted
                .IsRequired(false); //this is needed so that the navigation can be null


            modelBuilder.Entity<StudentSectionAssignment>()
                .HasOne(ssa => ssa.AcademicPeriod)
                .WithMany(sa => sa.StudentSectionAssignments)
                .HasForeignKey(ssa => ssa.AcademicPeriodId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///// FOR SECRETARY ASSIGNMENT ///////

            modelBuilder.Entity<SecretaryAssignment>()
                .HasIndex(sa => new { sa.SecretaryId, sa.SectionId})
                .IsUnique();

            modelBuilder.Entity<SecretaryAssignment>()
                .HasOne(sa => sa.Secretary)
                .WithMany(s => s.SecretariesAssignments)
                .HasForeignKey(sa => sa.SecretaryId)
                .OnDelete(DeleteBehavior.Restrict) //Not Cascade, Restrict
                .IsRequired(false); //this is needed so that the navigation can be null

            modelBuilder.Entity<SecretaryAssignment>()
                .HasOne(sa => sa.Section)
                .WithMany(s => s.SecretaryAssignments)
                .HasForeignKey(sa => sa.SectionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); //this is needed so that the navigation can be null

            modelBuilder.Entity<SecretaryAssignment>()
                .HasOne(sa => sa.AcademicPeriod)
                .WithMany(s => s.SecretaryAssignments)
                .HasForeignKey(sa => sa.AcademicPeriodId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///// FOR TEACHER ASSIGNMENT ///////
            
            //This avoid duplications for assign, But because of newly added AcademicPeriodId it can assign SAME TEACHER, SAME CLASS, DIFFERENT ACADEMIC PERIOD
            modelBuilder.Entity<TeacherAssignment>()
                .HasIndex(ta => new { ta.TeacherId, ta.SectionSubjectId, ta.AcademicPeriodId })
                .IsUnique();

            modelBuilder.Entity<TeacherAssignment>()
                .HasOne(ta => ta.Teacher)
                .WithMany(t => t.TeachingAssignments)
                .HasForeignKey(ta => ta.TeacherId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); //this is needed so that the navigation can be null

            modelBuilder.Entity<TeacherAssignment>()
                .HasOne(ta => ta.SectionSubject)
                .WithMany(t => t.TeacherAssignments)
                .HasForeignKey(ta => ta.SectionSubjectId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); //this is needed so that the navigation can be null

            modelBuilder.Entity<TeacherAssignment>()
                .HasOne(ta => ta.AcademicPeriod)
                .WithMany(t => t.TeacherAssignments)
                .HasForeignKey(ta => ta.AcademicPeriodId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///// FOR TEACHER ASSIGNMENT ///////
            ///

            //ALWAYS CONFIGURE FOREIGN KEY IF A CLASS HAS A FOREIGN KEY (EX.Class Attendance has public int Academic Period - public virtual AcademicPeriod AcademicPeriod
            //foreign keys to Student table
            //modelBuilder.Entity<Attendance>()
            //    .HasOne(a => a.Student)
            //    .WithMany(a => a.Attendances) // bagong lagay para gumana ang permanent delete function
            //    .HasForeignKey(a => a.StudentId)
            //    .OnDelete(DeleteBehavior.Restrict)
            //    .IsRequired(false); //this is needed so that the navigation can be null

            //BAGONG DAGDAG FOR REFACTOR ABOUT STUDENT
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.StudentSectionAssignment)
                .WithMany(a => a.StudentAttendances)
                .HasForeignKey(a => a.StudentSectionAssignmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            //foreign keys to AppUser table
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.RecordedBy)
                .WithMany()
                .HasForeignKey(a => a.RecordedById)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); //this is needed so that the navigation can be null

            //foreign keys to AppUser table
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.AcademicPeriod)
                .WithMany(ap => ap.Attendances) //(e.g., "Get all attendances for School Year 2024-2025")
                .HasForeignKey(a => a.AcademicPeriodId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false); //this is needed so that the navigation can be null

            //Secretary Assignment Relationship
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.SecretaryAssignment)
                .WithMany(sa => sa.SecretaryAttendances)
                .HasForeignKey(a => a.SecretaryAssignmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); //this is needed so that the navigation can be null

            //Teacher Assignment Relationship
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.TeacherAssignment)
                .WithMany(ta => ta.TeacherAttendances)
                .HasForeignKey(a => a.TeacherAssignmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); //this is needed so that the navigation can be null

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.SectionSubject)
                .WithMany(ss => ss.SectionSubjectAttendance)
                .HasForeignKey(a => a.SectionSubjectId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); //this is needed so that the navigation can be null

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
                    //a.StudentId,
                    a.StudentSectionAssignmentId,
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

            //For Soft Deletion 
            modelBuilder.Entity<AppUser>().HasQueryFilter(user => !user.IsDeleted);
            modelBuilder.Entity<AcademicPeriod>()
                .HasQueryFilter(ap => !ap.IsDeleted); //Automatically exclude delete records
            modelBuilder.Entity<Grade>().HasQueryFilter(g => !g.IsDeleted);
            modelBuilder.Entity<Section>().HasQueryFilter(s => !s.IsDeleted);
            modelBuilder.Entity<Subject>().HasQueryFilter(s => !s.IsDeleted);
            modelBuilder.Entity<Student>().HasQueryFilter(s => !s.IsDeleted);
            modelBuilder.Entity<SectionSubject>().HasQueryFilter(ss => !ss.IsDeleted);
            modelBuilder.Entity<StudentSectionAssignment>().HasQueryFilter(ssa => !ssa.IsDeleted);
            modelBuilder.Entity<TeacherAssignment>().HasQueryFilter(ta => !ta.IsDeleted);
            modelBuilder.Entity<SecretaryAssignment>().HasQueryFilter(sa => !sa.IsDeleted);

        }

    }

}

