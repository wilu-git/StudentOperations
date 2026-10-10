namespace StudentOperations.Domain;

public class Student
{
    public int Id { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EnrollmentRequest> EnrollmentRequests
    { get; set; } = new List<EnrollmentRequest>();
}