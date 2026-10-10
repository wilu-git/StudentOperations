using StudentOperations.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace StudentOperations.Application
{
    public interface IEnrollmentService
    {
        Task<int> RegisterStudentAsync(string fullName, string email);
        Task<int> CreateEnrollmentRequestAsync(int studentId, bool simulateTransientFailure = false);
        Task ProcessEnrollmentAsync(int requestId);
        Task ProcessPendingAsync();
        Task ProcessBatchAsync();
        Task SendConfirmationAsync(int requestId);
        Task<List<EnrollmentProcessingLog>> GetLogsAsync();
    }
}
