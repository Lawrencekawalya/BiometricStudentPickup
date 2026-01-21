// PickupLogService.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Sqlite;
using BiometricStudentPickup.Models;

namespace BiometricStudentPickup.Services
{
    public class PickupLogService
    {
        private readonly DatabaseService _databaseService;
        private readonly AuditLogService _auditLogService;

        public PickupLogService(DatabaseService databaseService, AuditLogService auditLogService)
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
                CREATE TABLE IF NOT EXISTS PickupLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentId INTEGER NOT NULL,
                    GuardianId INTEGER,
                    RequestedAt TEXT NOT NULL,
                    CompletedAt TEXT,
                    Status TEXT NOT NULL DEFAULT 'Completed',
                    Duration TEXT,
                    FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE CASCADE,
                    FOREIGN KEY (GuardianId) REFERENCES Guardians(Id) ON DELETE SET NULL
                );

                CREATE INDEX IF NOT EXISTS idx_pickuplogs_studentid ON PickupLogs(StudentId);
                CREATE INDEX IF NOT EXISTS idx_pickuplogs_guardianid ON PickupLogs(GuardianId);
                CREATE INDEX IF NOT EXISTS idx_pickuplogs_requestedat ON PickupLogs(RequestedAt);
                CREATE INDEX IF NOT EXISTS idx_pickuplogs_completedat ON PickupLogs(CompletedAt);
            ";

            cmd.ExecuteNonQuery();
        }

        public void LogPickupRequest(int studentId, int? guardianId = null)
        {
            try
            {
                using var conn = _databaseService.OpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    INSERT INTO PickupLogs (StudentId, GuardianId, RequestedAt, Status)
                    VALUES (@studentId, @guardianId, @requestedAt, 'Requested')
                ";

                cmd.Parameters.AddWithValue("@studentId", studentId);
                cmd.Parameters.AddWithValue("@guardianId", guardianId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@requestedAt", DateTime.Now.ToString("o"));

                cmd.ExecuteNonQuery();

                // Also log to audit
                _auditLogService.Log("PICKUP_REQUESTED", 
                    $"Pickup requested for student ID: {studentId}", 
                    studentId: studentId, 
                    guardianId: guardianId);
            }
            catch (Exception ex)
            {
                _auditLogService.Log("PICKUP_LOG_ERROR", 
                    $"Failed to log pickup request: {ex.Message}", 
                    success: false, 
                    errorMessage: ex.Message);
            }
        }

        public void LogPickupCompletion(int studentId, int? guardianId = null, DateTime? requestedAt = null)
        {
            try
            {
                var completedAt = DateTime.Now;
                TimeSpan? duration = null;
                
                if (requestedAt.HasValue)
                {
                    duration = completedAt - requestedAt.Value;
                }

                using var conn = _databaseService.OpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    UPDATE PickupLogs 
                    SET CompletedAt = @completedAt, 
                        Status = 'Completed',
                        Duration = @duration
                    WHERE StudentId = @studentId 
                    AND Status = 'Requested'
                    ORDER BY RequestedAt DESC
                    LIMIT 1
                ";

                cmd.Parameters.AddWithValue("@studentId", studentId);
                cmd.Parameters.AddWithValue("@completedAt", completedAt.ToString("o"));
                cmd.Parameters.AddWithValue("@duration", duration?.ToString() ?? (object)DBNull.Value);

                int rowsAffected = cmd.ExecuteNonQuery();

                // If no matching request found, create a new completed log
                if (rowsAffected == 0)
                {
                    LogPickupRequest(studentId, guardianId);
                    LogPickupCompletion(studentId, guardianId, DateTime.Now.AddMinutes(-1));
                }
                else
                {
                    // Log to audit
                    _auditLogService.Log("PICKUP_COMPLETED", 
                        $"Pickup completed for student ID: {studentId}", 
                        studentId: studentId, 
                        guardianId: guardianId);
                }
            }
            catch (Exception ex)
            {
                _auditLogService.Log("PICKUP_LOG_ERROR", 
                    $"Failed to log pickup completion: {ex.Message}", 
                    success: false, 
                    errorMessage: ex.Message);
            }
        }

        public void LogPickupTimeout(int studentId)
        {
            try
            {
                using var conn = _databaseService.OpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    UPDATE PickupLogs 
                    SET Status = 'Timeout'
                    WHERE StudentId = @studentId 
                    AND Status = 'Requested'
                    ORDER BY RequestedAt DESC
                    LIMIT 1
                ";

                cmd.Parameters.AddWithValue("@studentId", studentId);

                cmd.ExecuteNonQuery();

                // Log to audit
                _auditLogService.Log("PICKUP_TIMEOUT", 
                    $"Pickup timeout for student ID: {studentId}", 
                    studentId: studentId);
            }
            catch (Exception ex)
            {
                _auditLogService.Log("PICKUP_LOG_ERROR", 
                    $"Failed to log pickup timeout: {ex.Message}", 
                    success: false, 
                    errorMessage: ex.Message);
            }
        }

        // public List<PickupLog> GetPickupLogs(DateTime? startDate = null, DateTime? endDate = null)
        // {
        //     var logs = new List<PickupLog>();

        //     using var conn = _databaseService.OpenConnection();
        //     using var cmd = conn.CreateCommand();

        //     string query = @"
        //         SELECT pl.*, s.FullName as StudentName, g.FullName as GuardianName
        //         FROM PickupLogs pl
        //         LEFT JOIN Students s ON pl.StudentId = s.Id
        //         LEFT JOIN Guardians g ON pl.GuardianId = g.Id
        //         WHERE 1=1
        //     ";

        //     if (startDate.HasValue)
        //     {
        //         query += " AND DATE(pl.RequestedAt) >= DATE(@startDate)";
        //         cmd.Parameters.AddWithValue("@startDate", startDate.Value.ToString("yyyy-MM-dd"));
        //     }

        //     if (endDate.HasValue)
        //     {
        //         query += " AND DATE(pl.RequestedAt) <= DATE(@endDate)";
        //         cmd.Parameters.AddWithValue("@endDate", endDate.Value.ToString("yyyy-MM-dd"));
        //     }

        //     query += " ORDER BY pl.RequestedAt DESC";

        //     cmd.CommandText = query;

        //     using var reader = cmd.ExecuteReader();
        //     while (reader.Read())
        //     {
        //         var log = new PickupLog
        //         {
        //             Id = reader.GetInt32(0),
        //             StudentId = reader.GetInt32(1),
        //             GuardianId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
        //             RequestedAt = DateTime.Parse(reader.GetString(3)),
        //             Status = reader.GetString(6)
        //         };

        //         if (!reader.IsDBNull(4))
        //         {
        //             log.CompletedAt = DateTime.Parse(reader.GetString(4));
        //         }

        //         if (!reader.IsDBNull(7))
        //         {
        //             log.Duration = TimeSpan.Parse(reader.GetString(7));
        //         }

        //         logs.Add(log);
        //     }

        //     return logs;
        // }
        public List<PickupLog> GetPickupLogs(DateTime? startDate = null, DateTime? endDate = null)
        {
            var logs = new List<PickupLog>();

            try
            {
                Debug.WriteLine($"=== GetPickupLogs START ===");
                
                using var conn = _databaseService.OpenConnection();
                using var cmd = conn.CreateCommand();

                string query = @"
                    SELECT 
                        pl.Id,
                        pl.StudentId,
                        pl.GuardianId,
                        pl.RequestedAt,
                        pl.CompletedAt,
                        pl.Status,
                        pl.Duration,
                        s.FullName as StudentName,
                        g.FullName as GuardianName
                    FROM PickupLogs pl
                    LEFT JOIN Students s ON pl.StudentId = s.Id
                    LEFT JOIN Guardians g ON pl.GuardianId = g.Id
                    WHERE 1=1
                ";

                if (startDate.HasValue)
                {
                    query += " AND DATE(pl.RequestedAt) >= DATE(@startDate)";
                    cmd.Parameters.AddWithValue("@startDate", startDate.Value.ToString("yyyy-MM-dd"));
                }

                if (endDate.HasValue)
                {
                    query += " AND DATE(pl.RequestedAt) <= DATE(@endDate)";
                    cmd.Parameters.AddWithValue("@endDate", endDate.Value.ToString("yyyy-MM-dd"));
                }

                query += " ORDER BY pl.RequestedAt DESC";

                cmd.CommandText = query;
                
                Debug.WriteLine($"Executing query: {query}");

                using var reader = cmd.ExecuteReader();
                int rowCount = 0;
                
                Debug.WriteLine($"Database columns returned: {reader.FieldCount}");
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Debug.WriteLine($"  Column {i}: {reader.GetName(i)}");
                }
                
                while (reader.Read())
                {
                    rowCount++;
                    try
                    {
                        var log = new PickupLog
                        {
                            // Column indexes with JOINs:
                            Id = reader.GetInt32(0),                      // pl.Id
                            StudentId = reader.GetInt32(1),               // pl.StudentId
                            Status = reader.GetString(5)                  // pl.Status (INDEX 5, not 6!)
                        };

                        // GuardianId (index 2, nullable)
                        if (!reader.IsDBNull(2))
                        {
                            log.GuardianId = reader.GetInt32(2);
                        }

                        // RequestedAt (index 3)
                        log.RequestedAt = DateTime.Parse(reader.GetString(3));

                        // CompletedAt (index 4, nullable)
                        if (!reader.IsDBNull(4))
                        {
                            log.CompletedAt = DateTime.Parse(reader.GetString(4));
                        }

                        // Duration (index 6, nullable) - NOTE: Index 6, not 7!
                        if (!reader.IsDBNull(6))
                        {
                            log.Duration = TimeSpan.Parse(reader.GetString(6));
                        }
                        
                        // You could also get the names if you add properties to PickupLog:
                        // if (!reader.IsDBNull(7)) log.StudentName = reader.GetString(7);
                        // if (!reader.IsDBNull(8)) log.GuardianName = reader.GetString(8);

                        logs.Add(log);
                        
                        if (rowCount <= 3) // Debug first 3 records
                        {
                            Debug.WriteLine($"  Log [{rowCount}]: Id={log.Id}, StudentId={log.StudentId}, " +
                                        $"Status={log.Status}, RequestedAt={log.RequestedAt}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"  ERROR parsing row {rowCount}: {ex.Message}");
                    }
                }
                
                Debug.WriteLine($"=== GetPickupLogs END: Loaded {rowCount} records ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR in GetPickupLogs: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return logs;
        }

        // public List<PickupLog> GetStudentPickupHistory(int studentId)
        // {
        //     var logs = new List<PickupLog>();

        //     using var conn = _databaseService.OpenConnection();
        //     using var cmd = conn.CreateCommand();

        //     cmd.CommandText = @"
        //         SELECT * FROM PickupLogs 
        //         WHERE StudentId = @studentId
        //         ORDER BY RequestedAt DESC
        //         LIMIT 50
        //     ";

        //     cmd.Parameters.AddWithValue("@studentId", studentId);

        //     using var reader = cmd.ExecuteReader();
        //     while (reader.Read())
        //     {
        //         var log = new PickupLog
        //         {
        //             Id = reader.GetInt32(0),
        //             StudentId = reader.GetInt32(1),
        //             GuardianId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
        //             RequestedAt = DateTime.Parse(reader.GetString(3)),
        //             Status = reader.GetString(6)
        //         };

        //         if (!reader.IsDBNull(4))
        //         {
        //             log.CompletedAt = DateTime.Parse(reader.GetString(4));
        //         }

        //         if (!reader.IsDBNull(7))
        //         {
        //             log.Duration = TimeSpan.Parse(reader.GetString(7));
        //         }

        //         logs.Add(log);
        //     }

        //     return logs;
        // }
        public List<PickupLog> GetStudentPickupHistory(int studentId)
        {
            var logs = new List<PickupLog>();

            try
            {
                using var conn = _databaseService.OpenConnection();
                using var cmd = conn.CreateCommand();

                // This query doesn't have JOINs, so indexes are different!
                cmd.CommandText = @"
                    SELECT * FROM PickupLogs 
                    WHERE StudentId = @studentId
                    ORDER BY RequestedAt DESC
                    LIMIT 50
                ";

                cmd.Parameters.AddWithValue("@studentId", studentId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var log = new PickupLog
                    {
                        // NO JOINS - original table indexes:
                        Id = reader.GetInt32(0),                      // Id
                        StudentId = reader.GetInt32(1),               // StudentId
                        Status = reader.GetString(5)                  // Status (index 5)
                    };

                    if (!reader.IsDBNull(2))  // GuardianId
                    {
                        log.GuardianId = reader.GetInt32(2);
                    }

                    log.RequestedAt = DateTime.Parse(reader.GetString(3));  // RequestedAt

                    if (!reader.IsDBNull(4))  // CompletedAt
                    {
                        log.CompletedAt = DateTime.Parse(reader.GetString(4));
                    }

                    if (!reader.IsDBNull(6))  // Duration (index 6)
                    {
                        log.Duration = TimeSpan.Parse(reader.GetString(6));
                    }

                    logs.Add(log);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetStudentPickupHistory: {ex.Message}");
            }

            return logs;
        }

        public int GetTodayPickupCount()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            using var conn = _databaseService.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT COUNT(*) FROM PickupLogs 
                WHERE DATE(RequestedAt) = @today 
                AND Status = 'Completed'
            ";

            cmd.Parameters.AddWithValue("@today", today);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // Add this method to PickupLogService.cs
        public void TestServiceConnection()
        {
            try
            {
                using var conn = _databaseService.OpenConnection();
                using var cmd = conn.CreateCommand();
                
                cmd.CommandText = "SELECT COUNT(*) FROM PickupLogs";
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                
                Debug.WriteLine($"PickupLogService.TestServiceConnection: Table has {count} records");
                
                if (count > 0)
                {
                    cmd.CommandText = "SELECT * FROM PickupLogs LIMIT 3";
                    using var reader = cmd.ExecuteReader();
                    int recordNum = 1;
                    while (reader.Read())
                    {
                        Debug.WriteLine($"  Record {recordNum}:");
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader[i];
                            Debug.WriteLine($"    [{i}] {reader.GetName(i)} = {value} (Type: {value?.GetType().Name})");
                        }
                        recordNum++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PickupLogService.TestServiceConnection error: {ex.Message}");
            }
        }

        public void CleanupOldLogs(int daysToKeep = 90)
        {
            try
            {
                using var conn = _databaseService.OpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    DELETE FROM PickupLogs 
                    WHERE DATE(RequestedAt) < DATE('now', @days)
                ";
                
                cmd.Parameters.AddWithValue("@days", $"-{daysToKeep} days");
                int deleted = cmd.ExecuteNonQuery();
                
                if (deleted > 0)
                {
                    _auditLogService.Log("PICKUP_LOGS_CLEANED", 
                        $"Cleaned up {deleted} pickup logs older than {daysToKeep} days");
                }
            }
            catch (Exception ex)
            {
                _auditLogService.Log("PICKUP_LOG_ERROR", 
                    "Failed to clean up old pickup logs", 
                    success: false, errorMessage: ex.Message);
            }
        }

        public DatabaseService GetDatabaseService()
        {
            return _databaseService;
        }
    }
}