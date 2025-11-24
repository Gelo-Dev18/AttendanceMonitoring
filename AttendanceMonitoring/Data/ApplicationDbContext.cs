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
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<SectionSubject> SectionSubjects { get; set; } // Linking Table for Section and subject!
        public DbSet<Teacher> Teacher { get; set; }
        //public DbSet<Student> Students { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //GradeLevel - SectionName Relationship (One-to-Many)
            modelBuilder.Entity<Section>()
                .HasOne(s => s.Grade)          //Section has one Grade
                .WithMany(g => g.Sections)      //Grade has many Sections
                .HasForeignKey(s => s.GradesId)      //Foreign key
                .OnDelete(DeleteBehavior.Restrict);  // ← IMPORTANT! Prevent cascade delete


            modelBuilder.Entity<SectionSubject>()
                .HasIndex(ss => new { ss.SectionId, ss.SubjectId })
                .IsUnique();
            modelBuilder.Entity<SectionSubject>()
                .HasOne(ss => ss.Section)
                .WithMany(s => s.SectionSubjects)
                .HasForeignKey(ss => ss.SectionId);

            modelBuilder.Entity<SectionSubject>()
                .HasOne(ss => ss.Subject)
                .WithMany(j => j.SectionSubjects)
                .HasForeignKey(ss => ss.SubjectId);

        }

    }
}
