using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using StudentOperations.Application;
using StudentOperations.Domain;

namespace StudentOperations.ConsoleApp;

public class ConsoleMenu
{
    private readonly IEnrollmentService _service;
    private readonly IServiceScopeFactory _scopeFactory;

    public ConsoleMenu(
        IEnrollmentService service,
        IServiceScopeFactory scopeFactory)
    {
        _service = service;
        _scopeFactory = scopeFactory;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("STUDENT OPERATIONS");
            Console.WriteLine("1. Register student");
            Console.WriteLine("2. Create enrollment request");
            Console.WriteLine("3. Immediate job");
            Console.WriteLine("4. Delayed job");
            Console.WriteLine("5. Continuation job");
            Console.WriteLine("6. Batch job");
            Console.WriteLine("7. Show processing history");
            Console.WriteLine("0. Exit");
            Console.Write("Select: ");

            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await RegisterStudentAsync();
                        break;

                    case "2":
                        await CreateRequestAsync();
                        break;

                    case "3":
                        ScheduleImmediate();
                        break;

                    case "4":
                        ScheduleDelayed();
                        break;

                    case "5":
                        ScheduleContinuation();
                        break;

                    case "6":
                        BackgroundJob.Enqueue<EnrollmentJob>(
                            job => job.ProcessBatchAsync());

                        Console.WriteLine("Batch job queued.");
                        break;

                    case "7":
                        await ShowHistoryAsync();
                        break;

                    case "0":
                        return;

                    default:
                        Console.WriteLine("Invalid menu option.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private async Task RegisterStudentAsync()
    {
        Console.Write("Full name: ");
        var name = Console.ReadLine() ?? "";

        Console.Write("Email: ");
        var email = Console.ReadLine() ?? "";

        var studentId = await _service.RegisterStudentAsync(
            name,
            email);

        Console.WriteLine($"Student created. ID: {studentId}");
    }

    private async Task CreateRequestAsync()
    {
        Console.Write("Student ID: ");

        if (!int.TryParse(Console.ReadLine(), out var studentId))
        {
            Console.WriteLine("Enter a valid student ID.");
            return;
        }

        Console.Write("Simulate a temporary failure? (y/n): ");
        var simulateFailure = Console.ReadLine()
            ?.Trim()
            .Equals("y", StringComparison.OrdinalIgnoreCase) == true;

        var requestId = await _service.CreateEnrollmentRequestAsync(
            studentId,
            simulateFailure);

        Console.WriteLine(
            $"Enrollment request created. ID: {requestId}");
    }

    private static void ScheduleImmediate()
    {
        Console.Write("Enrollment request ID: ");

        if (!int.TryParse(Console.ReadLine(), out var requestId))
        {
            Console.WriteLine("Enter a valid request ID.");
            return;
        }

        var jobId = BackgroundJob.Enqueue<EnrollmentJob>(
            job => job.ProcessAsync(requestId));

        Console.WriteLine($"Job queued. Hangfire ID: {jobId}");
    }

    private static void ScheduleDelayed()
    {
        Console.Write("Enrollment request ID: ");

        if (!int.TryParse(Console.ReadLine(), out var requestId))
        {
            Console.WriteLine("Enter a valid request ID.");
            return;
        }

        var jobId = BackgroundJob.Schedule<EnrollmentJob>(
            job => job.ProcessAsync(requestId),
            TimeSpan.FromMinutes(1));

        Console.WriteLine($"Delayed job scheduled. ID: {jobId}");
    }

    private static void ScheduleContinuation()
    {
        Console.Write("Enrollment request ID: ");

        if (!int.TryParse(Console.ReadLine(), out var requestId))
        {
            Console.WriteLine("Enter a valid request ID.");
            return;
        }

        var parentJobId = BackgroundJob.Enqueue<EnrollmentJob>(
            job => job.ProcessAsync(requestId));

        var continuationId =
            BackgroundJob.ContinueJobWith<EnrollmentJob>(
                parentJobId,
                job => job.SendConfirmationAsync(requestId),
                JobContinuationOptions.OnlyOnSucceededState);

        Console.WriteLine($"Parent job: {parentJobId}");
        Console.WriteLine($"Continuation job: {continuationId}");
    }

    private async Task ShowHistoryAsync()
    {
        var logs = await _service.GetLogsAsync();

        foreach (var log in logs)
        {
            Console.WriteLine(
                $"{log.CreatedAt:u} | Request {log.EnrollmentRequestId} | {log.Message}");
        }

        if (logs.Count == 0)
            Console.WriteLine("No processing history found.");
    }
}