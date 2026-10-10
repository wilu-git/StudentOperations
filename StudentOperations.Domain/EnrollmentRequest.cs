using System;
using System.Collections.Generic;
using System.Text;

namespace StudentOperations.Domain
{
    public class EnrollmentRequest
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public Student Student { get; set; } = null!;

        public EnrollmentStatus Status { get; set; }
            = EnrollmentStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        public bool ConfirmationSent { get; set; }

        // Used to demonstrate a recoverable failure in this lab.
        public bool SimulateTransientFailure { get; set; }

        public bool HasFailedOnce { get; set; }
    }
}
