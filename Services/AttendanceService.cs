// AttendanceService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using BiometricStudentPickup.Models;

namespace BiometricStudentPickup.Services
{
    public class AttendanceService
    {
        private readonly DatabaseService _databaseService;
        private readonly AuditLogService _auditLogService;

        public AttendanceService(DatabaseService databaseService, AuditLogService auditLogService)
        {
            _databaseService = databaseService;
            _auditLogService = auditLogService;
            InitializeTable();
        }

        private void InitializeTable()
        {
            using var conn = _databaseService.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Attendance (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentId INTEGER NOT NULL,
                    Date TEXT NOT NULL,
                    TimeIn TEXT NOT NULL,
                    FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_attendance_studentid ON Attendance(StudentId);
                CREATE INDEX IF NOT EXISTS idx_attendance_date ON Attendance(Date);
                CREATE INDEX IF NOT EXISTS idx_attendance_datetime ON Attendance(Date, StudentId);
            ";

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Records attendance for a student if they haven't been marked present today
        /// </summary>
        public bool RecordAttendance(int studentId, string studentName)
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            
            // Check if student is already marked present today
            if (IsPresentToday(studentId))
            {
                _auditLogService.Log("ATTENDANCE_DUPLICATE", 
                    $"Student already marked present today: {studentName}", 
                    studentId: studentId,
                    details: "Attendance not recorded - already present");
                return false;
            }

            try
            {
                using var conn = _databaseService.OpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    INSERT INTO Attendance (StudentId, Date, TimeIn)
                    VALUES (@studentId, @date, @timeIn)
                ";

                cmd.Parameters.AddWithValue("@studentId", studentId);
                cmd.Parameters.AddWithValue("@date", today);
                cmd.Parameters.AddWithValue("@timeIn", DateTime.Now.ToString("HH:mm:ss"));

                cmd.ExecuteNonQuery();

                // Log attendance recorded
                _auditLogService.Log("ATTENDANCE_RECORDED", 
                    $"Attendance recorded for {studentName}", 
                    studentId: studentId,
                    details: $"Date: {today}, Time: {DateTime.Now:HH:mm:ss}");

                return true;
            }
            catch (Exception ex)
            {
                _auditLogService.Log("ATTENDANCE_ERROR", 
                    $"Failed to record attendance for {studentName}", 
                    studentId: studentId,
                    success: false,
                    errorMessage: ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Checks if a student is already marked present today
        /// </summary>
        public bool IsPresentToday(int studentId)
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            using var conn = _databaseService.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT COUNT(*) FROM Attendance 
                WHERE StudentId = @studentId AND Date = @date
            ";

            cmd.Parameters.AddWithValue("@studentId", studentId);
            cmd.Parameters.AddWithValue("@date", today);

            var count = Convert.ToInt32(cmd.ExecuteScalar());
            return count > 0;
        }

        /// <summary>
        /// Gets attendance records for a specific date
        /// </summary>
        public List<Attendance> GetAttendanceByDate(DateTime date)
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var attendanceList = new List<Attendance>();

            using var conn = _databaseService.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT a.*, s.FullName, s.ClassName 
                FROM Attendance a
                LEFT JOIN Students s ON a.StudentId = s.Id
                WHERE a.Date = @date
                ORDER BY a.TimeIn
            ";

            cmd.Parameters.AddWithValue("@date", dateStr);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                attendanceList.Add(new Attendance
                {
                    Id = reader.GetInt32(0),
                    StudentId = reader.GetInt32(1),
                    Date = DateTime.Parse(reader.GetString(2)),
                    TimeIn = DateTime.Parse(reader.GetString(3))
                });
            }

            return attendanceList;
        }

        /// <summary>
        /// Gets attendance records for a specific student
        /// </summary>
        public List<Attendance> GetAttendanceByStudent(int studentId)
        {
            var attendanceList = new List<Attendance>();

            using var conn = _databaseService.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT * FROM Attendance 
                WHERE StudentId = @studentId
                ORDER BY Date DESC, TimeIn DESC
                LIMIT 30
            ";

            cmd.Parameters.AddWithValue("@studentId", studentId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                attendanceList.Add(new Attendance
                {
                    Id = reader.GetInt32(0),
                    StudentId = reader.GetInt32(1),
                    Date = DateTime.Parse(reader.GetString(2)),
                    TimeIn = DateTime.Parse(reader.GetString(3))
                });
            }

            return attendanceList;
        }

        /// <summary>
        /// Gets today's attendance count
        /// </summary>
        public int GetTodayAttendanceCount()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            using var conn = _databaseService.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT COUNT(DISTINCT StudentId) FROM Attendance 
                WHERE Date = @date
            ";

            cmd.Parameters.AddWithValue("@date", today);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Gets all students who are present today
        /// </summary>
        public List<int> GetTodayPresentStudentIds()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var studentIds = new List<int>();

            using var conn = _databaseService.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT DISTINCT StudentId FROM Attendance 
                WHERE Date = @date
            ";

            cmd.Parameters.AddWithValue("@date", today);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                studentIds.Add(reader.GetInt32(0));
            }

            return studentIds;
        }
    }
}