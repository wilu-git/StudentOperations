using Microsoft.EntityFrameworkCore;
using StudentOperations.Domain;

namespace StudentOperations.Infrastructure.Data;

public class StudentOperationsDbContext : DbContext
{
    public StudentOperationsDbContext(
        DbContextOptions<StudentOperationsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();

    public DbSet<EnrollmentRequest> EnrollmentRequests
        => Set<EnrollmentRequest>();

    public DbSet<EnrollmentProcessingLog> ProcessingLogs
        => Set<EnrollmentProcessingLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Student>()
            .HasIndex(x => x.StudentNumber)
            .IsUnique();

        modelBuilder.Entity<Student>()
            .Property(x => x.FullName)
            .HasMaxLength(150)
            .IsRequired();

        modelBuilder.Entity<Student>()
            .Property(x => x.Email)
            .HasMaxLength(150)
            .IsRequired();

        modelBuilder.Entity<EnrollmentRequest>()
            .HasOne(x => x.Student)
            .WithMany(x => x.EnrollmentRequests)
            .HasForeignKey(x => x.StudentId);

        modelBuilder.Entity<EnrollmentRequest>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<EnrollmentProcessingLog>()
            .Property(x => x.Message)
            .HasMaxLength(500)
            .IsRequired();
    }
}