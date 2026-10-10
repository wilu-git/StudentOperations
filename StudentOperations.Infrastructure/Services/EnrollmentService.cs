using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentOperations.Application;
using StudentOperations.Domain;
using StudentOperations.Infrastructure.Data;

namespace StudentOperations.Infrastructure.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly StudentOperationsDbContext _db;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(StudentOperationsDbContext db, ILogger<EnrollmentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> RegisterStudentAsync(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.");

        var student = new Student
        {
            StudentNumber = $"STU-{Guid.NewGuid():N}"
                .Substring(0, 12)
                .ToUpperInvariant(),
            FullName = fullName.Trim(),
            Email = email.Trim()
        };

        _db.Students.Add(student);

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Registered student {StudentId}",
            student.Id);

        return student.Id;
    }

    public async Task<int> CreateEnrollmentRequestAsync(
        int studentId,
        bool simulateTransientFailure = false)
    {
        var studentExists = await _db.Students
            .AnyAsync(x => x.Id == studentId);

        if (!studentExists)
            throw new InvalidOperationException(
                $"Student {studentId} does not exist.");

        var request = new EnrollmentRequest
        {
            StudentId = studentId,
            Status = EnrollmentStatus.Pending,
            SimulateTransientFailure = simulateTransientFailure
        };

        _db.EnrollmentRequests.Add(request);

        await _db.SaveChangesAsync();

        return request.Id;
    }

    public async Task ProcessEnrollmentAsync(int requestId)
    {
        var request = await _db.EnrollmentRequests
            .SingleOrDefaultAsync(x => x.Id == requestId);

        if (request is null)
            throw new InvalidOperationException(
                $"Enrollment request {requestId} was not found.");

        // A retry must not process an already completed request again.
        if (request.Status == EnrollmentStatus.Completed)
        {
            _logger.LogInformation(
                "Request {RequestId} is already completed",
                requestId);

            return;
        }

        request.Status = EnrollmentStatus.Processing;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Processing enrollment request {RequestId}",
            requestId);

        // Demonstrate a recoverable failure on the first attempt.
        if (request.SimulateTransientFailure &&
            !request.HasFailedOnce)
        {
            request.HasFailedOnce = true;
            request.Status = EnrollmentStatus.Failed;

            _db.ProcessingLogs.Add(new EnrollmentProcessingLog
            {
                EnrollmentRequestId = requestId,
                Message = "Simulated temporary processing failure."
            });

            await _db.SaveChangesAsync();

            // Hangfire sees the exception and can retry the job.
            throw new InvalidOperationException(
                "Simulated temporary failure. Retry expected.");
        }

        request.Status = EnrollmentStatus.Completed;
        request.ProcessedAt = DateTime.UtcNow;

        _db.ProcessingLogs.Add(new EnrollmentProcessingLog
        {
            EnrollmentRequestId = requestId,
            Message = "Enrollment processed successfully."
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Completed enrollment request {RequestId}",
            requestId);
    }

    public async Task ProcessPendingAsync()
    {
        var pendingIds = await _db.EnrollmentRequests
            .AsNoTracking()
            .Where(x => x.Status == EnrollmentStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .Take(100)
            .ToListAsync();

        foreach (var requestId in pendingIds)
        {
            await ProcessEnrollmentAsync(requestId);
        }
    }

    public async Task ProcessBatchAsync()
    {
        var pendingIds = await _db.EnrollmentRequests
            .AsNoTracking()
            .Where(x => x.Status == EnrollmentStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .Take(100)
            .ToListAsync();

        foreach (var requestId in pendingIds)
        {
            try
            {
                await ProcessEnrollmentAsync(requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process request {RequestId} in batch",
                    requestId);
            }
        }
    }

    public async Task SendConfirmationAsync(int requestId)
    {
        var request = await _db.EnrollmentRequests
            .SingleOrDefaultAsync(x => x.Id == requestId);

        if (request is null)
            throw new InvalidOperationException(
                $"Enrollment request {requestId} was not found.");

        if (request.Status != EnrollmentStatus.Completed)
        {
            throw new InvalidOperationException(
                "Confirmation requires a completed enrollment.");
        }

        if (request.ConfirmationSent)
            return;

        // Lab demonstration: record the confirmation.
        // A real application could send an email here.
        request.ConfirmationSent = true;

        _db.ProcessingLogs.Add(new EnrollmentProcessingLog
        {
            EnrollmentRequestId = requestId,
            Message = "Enrollment confirmation recorded."
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Confirmation recorded for request {RequestId}",
            requestId);
    }

    public async Task<List<EnrollmentProcessingLog>> GetLogsAsync()
    {
        return await _db.ProcessingLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync();
    }
}