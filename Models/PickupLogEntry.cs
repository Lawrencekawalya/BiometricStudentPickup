using System;

namespace BiometricStudentPickup.Models
{
    public class PickupLogEntry
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = ""; // Initialize with default value
        public int StudentId { get; set; }
        public string StudentName { get; set; } = ""; // Initialize with default value
        public string ClassName { get; set; } = ""; // Initialize with default value
        public int GuardianId { get; set; }
        public string GuardianName { get; set; } = ""; // Initialize with default value
        public string Details { get; set; } = ""; // Initialize with default value
        
        // Constructor for creating new entries
        public PickupLogEntry(string eventType, int studentId = 0, string studentName = "", 
                            string className = "", int guardianId = 0, string guardianName = "", 
                            string details = "")
        {
            Timestamp = DateTime.Now;
            EventType = eventType;
            StudentId = studentId;
            StudentName = studentName ?? "";
            ClassName = className ?? "";
            GuardianId = guardianId;
            GuardianName = guardianName ?? "";
            Details = details ?? "";
        }
        
        // Empty constructor for Dapper - properties are already initialized above
        public PickupLogEntry() { }
    }
}