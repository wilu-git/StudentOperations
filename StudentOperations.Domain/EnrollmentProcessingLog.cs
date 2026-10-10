namespace StudentOperations.Domain;

public class EnrollmentProcessingLog
{
    public int Id { get; set; }

    public int EnrollmentRequestId { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}