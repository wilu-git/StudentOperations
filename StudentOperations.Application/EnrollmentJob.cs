using Hangfire;

namespace StudentOperations.Application;

public class EnrollmentJob
{
    private readonly IEnrollmentService _service;

    public EnrollmentJob(IEnrollmentService service)
    {
        _service = service;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 5, 15, 30 })]
    public Task ProcessAsync(int requestId)
    {
        return _service.ProcessEnrollmentAsync(requestId);
    }

    public Task ProcessPendingAsync()
    {
        return _service.ProcessPendingAsync();
    }

    public Task ProcessBatchAsync()
    {
        return _service.ProcessBatchAsync();
    }

    public Task SendConfirmationAsync(int requestId)
    {
        return _service.SendConfirmationAsync(requestId);
    }
}
