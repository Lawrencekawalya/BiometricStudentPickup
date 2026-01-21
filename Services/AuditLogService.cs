// using System;
// using Microsoft.Data.Sqlite;

// namespace BiometricStudentPickup.Services
// {
//     public class AuditLogService
//     {
//         private readonly DatabaseService _db;

//         public AuditLogService(DatabaseService db)
//         {
//             _db = db;
//         }

//         public void Log(
//             string eventType,
//             string description,
//             int? studentId = null,
//             int? guardianId = null)
//         {
//             using var conn = _db.OpenConnection();
//             using var cmd = conn.CreateCommand();

//             cmd.CommandText = @"
//                 INSERT INTO AuditLogs
//                 (EventType, Description, Timestamp, StudentId, GuardianId)
//                 VALUES (@e, @d, @t, @s, @g)
//             ";

//             cmd.Parameters.AddWithValue("@e", eventType);
//             cmd.Parameters.AddWithValue("@d", description);
//             cmd.Parameters.AddWithValue("@t", DateTime.UtcNow.ToString("o"));
//             cmd.Parameters.AddWithValue("@s", (object?)studentId ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@g", (object?)guardianId ?? DBNull.Value);

//             cmd.ExecuteNonQuery();
//         }
//     }
// }

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace BiometricStudentPickup.Services
{
    public class AuditLogService : IDisposable
    {
        private readonly DatabaseService _db;
        private readonly string _sessionId;
        private readonly string _userName;
        private readonly string _machineName;
        private bool _disposed;

        // Event type constants
        public static class EventTypes
        {
            // Enrollment events
            public const string StudentEnrolled = "STUDENT_ENROLLED";
            public const string StudentEnrollmentFailed = "STUDENT_ENROLLMENT_FAILED";
            public const string StudentEnrollmentCancelled = "STUDENT_ENROLLMENT_CANCELLED"; // NEW
            public const string GuardianEnrolled = "GUARDIAN_ENROLLED";
            public const string GuardianEnrollmentFailed = "GUARDIAN_ENROLLMENT_FAILED";
            public const string GuardianEnrollmentCancelled = "GUARDIAN_ENROLLMENT_CANCELLED"; // NEW
            
            // New enrollment-related events
            public const string EnrollmentWindowOpened = "ENROLLMENT_WINDOW_OPENED"; // NEW
            public const string EnrollmentWindowClosed = "ENROLLMENT_WINDOW_CLOSED"; // NEW
            public const string FingerprintEnrollmentStarted = "FINGERPRINT_ENROLLMENT_STARTED"; // NEW
            public const string DeviceUploadSuccess = "DEVICE_UPLOAD_SUCCESS"; // NEW
            public const string DeviceCommunicationError = "DEVICE_COMMUNICATION_ERROR"; // NEW
            
            // Fingerprint events
            public const string FingerprintScanned = "FINGERPRINT_SCANNED";
            public const string FingerprintVerified = "FINGERPRINT_VERIFIED";
            public const string FingerprintVerificationFailed = "FINGERPRINT_VERIFICATION_FAILED";
            public const string FingerprintDuplicateDetected = "FINGERPRINT_DUPLICATE_DETECTED";
            
            // Pickup events
            public const string PickupRequested = "PICKUP_REQUESTED";
            public const string PickupConfirmed = "PICKUP_CONFIRMED";
            public const string PickupTimeout = "PICKUP_TIMEOUT";
            public const string PickupRequeued = "PICKUP_REQUEUED";
            
            // Admin events
            public const string AdminLoginAttempt = "ADMIN_LOGIN_ATTEMPT";
            public const string AdminLoginSuccess = "ADMIN_LOGIN_SUCCESS";
            public const string AdminLoginFailure = "ADMIN_LOGIN_FAILURE";
            public const string AdminSessionStarted = "ADMIN_SESSION_STARTED";
            public const string AdminSessionEnded = "ADMIN_SESSION_ENDED";
            public const string AdminPinChanged = "ADMIN_PIN_CHANGED";
            
            // System events
            public const string SystemStarted = "SYSTEM_STARTED";
            public const string SystemShutdown = "SYSTEM_SHUTDOWN";
            public const string DeviceConnected = "DEVICE_CONNECTED";
            public const string DeviceDisconnected = "DEVICE_DISCONNECTED";
            public const string DatabaseError = "DATABASE_ERROR";
        }

        public AuditLogService(DatabaseService db)
        {
            _db = db;
            _sessionId = Guid.NewGuid().ToString();
            _userName = WindowsIdentity.GetCurrent()?.Name ?? "Unknown";
            _machineName = Environment.MachineName;
            
            InitializeTables();
            LogSystemStart();
        }

        private void InitializeTables()
        {
            using var conn = _db.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS AuditLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    EventType TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Details TEXT,
                    Timestamp TEXT NOT NULL,
                    SessionId TEXT NOT NULL,
                    UserName TEXT NOT NULL,
                    MachineName TEXT NOT NULL,
                    StudentId INTEGER,
                    GuardianId INTEGER,
                    Success INTEGER NOT NULL DEFAULT 1,
                    ErrorMessage TEXT,
                    FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE SET NULL,
                    FOREIGN KEY (GuardianId) REFERENCES Guardians(Id) ON DELETE SET NULL
                );

                CREATE INDEX IF NOT EXISTS idx_audit_timestamp ON AuditLogs(Timestamp);
                CREATE INDEX IF NOT EXISTS idx_audit_eventtype ON AuditLogs(EventType);
                CREATE INDEX IF NOT EXISTS idx_audit_session ON AuditLogs(SessionId);
                CREATE INDEX IF NOT EXISTS idx_audit_student ON AuditLogs(StudentId);
                CREATE INDEX IF NOT EXISTS idx_audit_guardian ON AuditLogs(GuardianId);
            ";

            cmd.ExecuteNonQuery();
        }

        public void Log(
            string eventType,
            string description,
            int? studentId = null,
            int? guardianId = null,
            bool success = true,
            string? errorMessage = null,
            string? details = null)
        {
            try
            {
                using var conn = _db.OpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    INSERT INTO AuditLogs
                    (EventType, Description, Details, Timestamp, SessionId, 
                     UserName, MachineName, StudentId, GuardianId, Success, ErrorMessage)
                    VALUES (@e, @d, @details, @t, @sess, @user, @machine, @sid, @gid, @success, @err)
                ";

                cmd.Parameters.AddWithValue("@e", eventType);
                cmd.Parameters.AddWithValue("@d", description);
                cmd.Parameters.AddWithValue("@details", (object?)details ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@t", DateTime.UtcNow.ToString("o"));
                cmd.Parameters.AddWithValue("@sess", _sessionId);
                cmd.Parameters.AddWithValue("@user", _userName);
                cmd.Parameters.AddWithValue("@machine", _machineName);
                cmd.Parameters.AddWithValue("@sid", (object?)studentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gid", (object?)guardianId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@success", success ? 1 : 0);
                cmd.Parameters.AddWithValue("@err", (object?)errorMessage ?? DBNull.Value);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Fallback to event log if database logging fails
                EventLog.WriteEntry("Application", 
                    $"Audit log failed: {eventType} - {ex.Message}", 
                    EventLogEntryType.Error);
            }
        }

        public async Task LogAsync(
            string eventType,
            string description,
            int? studentId = null,
            int? guardianId = null,
            bool success = true,
            string? errorMessage = null,
            string? details = null)
        {
            await Task.Run(() => Log(eventType, description, studentId, guardianId, success, errorMessage, details));
        }

        // Convenience methods for common events
        public void LogStudentEnrolled(int studentId, string studentName, string className)
        {
            Log(EventTypes.StudentEnrolled, 
                $"Student enrolled: {studentName} ({className})", 
                studentId: studentId);
        }

        public void LogStudentEnrollmentFailed(string studentName, string error)
        {
            Log(EventTypes.StudentEnrollmentFailed, 
                $"Student enrollment failed: {studentName}", 
                success: false, 
                errorMessage: error);
        }

        public void LogGuardianEnrolled(int guardianId, string guardianName)
        {
            Log(EventTypes.GuardianEnrolled, 
                $"Guardian enrolled: {guardianName}", 
                guardianId: guardianId);
        }

        public void LogFingerprintScanned(int? fingerprintId, bool isGuardian, bool verified)
        {
            var entityType = isGuardian ? "Guardian" : "Student";
            var eventType = verified ? EventTypes.FingerprintVerified : EventTypes.FingerprintVerificationFailed;
            var description = verified ? 
                $"{entityType} fingerprint verified (ID: {fingerprintId})" :
                $"{entityType} fingerprint not recognized";
            
            Log(eventType, description, success: verified);
        }

        public void LogPickupRequested(int studentId, string studentName, int? guardianId = null)
        {
            var description = guardianId.HasValue ?
                $"Pickup requested for {studentName} by guardian" :
                $"Pickup requested for {studentName}";
            
            Log(EventTypes.PickupRequested, description, 
                studentId: studentId, guardianId: guardianId);
        }

        public void LogPickupConfirmed(int studentId, string studentName)
        {
            Log(EventTypes.PickupConfirmed, 
                $"Pickup confirmed for {studentName}", 
                studentId: studentId);
        }

        public void LogAdminLoginAttempt(bool success, string? error = null)
        {
            var eventType = success ? EventTypes.AdminLoginSuccess : EventTypes.AdminLoginFailure;
            Log(eventType, "Admin login attempt", 
                success: success, errorMessage: error);
        }

        private void LogSystemStart()
        {
            Log(EventTypes.SystemStarted, 
                "Biometric Student Pickup System started",
                details: $"Version: {GetApplicationVersion()}, Session: {_sessionId}");
        }

        private void LogSystemShutdown()
        {
            Log(EventTypes.SystemShutdown, 
                "Biometric Student Pickup System shutting down");
        }

        private string GetApplicationVersion()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return version?.ToString() ?? "Unknown";
        }

        // Enrollment cancellation methods
        public void LogStudentEnrollmentCancelled(string studentName)
        {
            Log(EventTypes.StudentEnrollmentCancelled, 
                $"Student enrollment cancelled: {studentName}", 
                success: false, 
                details: "User cancelled fingerprint enrollment");
        }

        public void LogGuardianEnrollmentCancelled(string guardianName)
        {
            Log(EventTypes.GuardianEnrollmentCancelled, 
                $"Guardian enrollment cancelled: {guardianName}", 
                success: false, 
                details: "User cancelled fingerprint enrollment");
        }

        // Device communication methods
        public void LogDeviceUploadSuccess(int fingerprintId, string entityType)
        {
            Log(EventTypes.DeviceUploadSuccess, 
                $"Fingerprint uploaded to device: {entityType} (ID: {fingerprintId})", 
                success: true,
                details: $"Fingerprint ID: {fingerprintId}, Entity: {entityType}");
        }

        public void LogDeviceCommunicationError(string operation, string error)
        {
            Log(EventTypes.DeviceCommunicationError, 
                $"Device communication failed: {operation}", 
                success: false, 
                errorMessage: error,
                details: $"Operation: {operation}");
        }

        // Enrollment window methods
        public void LogEnrollmentWindowOpened()
        {
            Log(EventTypes.EnrollmentWindowOpened, 
                "Enrollment window opened", 
                success: true);
        }

        public void LogEnrollmentWindowClosed()
        {
            Log(EventTypes.EnrollmentWindowClosed, 
                "Enrollment window closed", 
                success: true);
        }

        // Fingerprint enrollment start method
        public void LogFingerprintEnrollmentStarted(string entityName, bool isGuardian = false)
        {
            var entityType = isGuardian ? "Guardian" : "Student";
            Log(EventTypes.FingerprintEnrollmentStarted, 
                $"Starting fingerprint enrollment for {entityType}: {entityName}", 
                success: true,
                details: $"Entity: {entityName}, Type: {entityType}");
        }

        // Cleanup old logs (run periodically, e.g., weekly)
        public void CleanupOldLogs(int daysToKeep = 90)
        {
            try
            {
                using var conn = _db.OpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    DELETE FROM AuditLogs 
                    WHERE Timestamp < datetime('now', @days)
                ";
                
                cmd.Parameters.AddWithValue("@days", $"-{daysToKeep} days");
                int deleted = cmd.ExecuteNonQuery();
                
                if (deleted > 0)
                {
                    Log(EventTypes.SystemShutdown, 
                        $"Cleaned up {deleted} audit logs older than {daysToKeep} days");
                }
            }
            catch (Exception ex)
            {
                Log(EventTypes.DatabaseError, 
                    "Failed to clean up old audit logs", 
                    success: false, errorMessage: ex.Message);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                LogSystemShutdown();
                _disposed = true;
            }
        }
    }
}
