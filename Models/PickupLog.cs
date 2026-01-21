using System;

// PickupLog.cs
namespace BiometricStudentPickup.Models
{
    public class PickupLog
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int? GuardianId { get; set; } // Nullable for student self-scan
        public DateTime RequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "Completed"; // "Completed", "Timeout", "Cancelled"
        public TimeSpan? Duration { get; set; }

        // Navigation properties
        public Student? Student { get; set; }
        public Guardian? Guardian { get; set; }
        public string? ClassName { get; set; }
    }
}