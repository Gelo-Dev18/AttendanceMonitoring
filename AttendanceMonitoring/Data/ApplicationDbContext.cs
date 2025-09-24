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

        public DbSet<Teacher> Teacher { get; set; }
        //public DbSet<Student> Students { get; set; }

        //protected override void OnModelCreating(ModelBuilder builder)
        //{
        //    base.OnModelCreating(builder);

        //    builder.Entity<AppUser>()
        //        .Property(a => a.CreatedAt)
        //        .HasDefaultValueSql("GETDATE()"); // An Entity Framework Core method for to set the current value during insert using GETDATE()

        //    //builder.Entity<AppUser>()
        //    //    .Property(a => a.UpdatedAt)
        //    //    .HasDefaultValueSql("GETDATE()")
        //    //    .ValueGeneratedOnAddOrUpdate();

            
        //}

    }
}
