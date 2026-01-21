// Attendance.cs
using System;

namespace BiometricStudentPickup.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateTime Date { get; set; }
        public DateTime TimeIn { get; set; }
        
        // Navigation property (optional)
        public Student? Student { get; set; }
    }
}