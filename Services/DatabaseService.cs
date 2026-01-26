using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace BiometricStudentPickup.Services
{
    public class DatabaseService
    {
        private static readonly string DbFolder =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "BiometricStudentPickup"
            );

        private static readonly string DbPath =
            Path.Combine(DbFolder, "biometric.db");

        private readonly string _connectionString;

        public DatabaseService()
        {
            Directory.CreateDirectory(DbFolder);
            _connectionString = $"Data Source={DbPath}";
            
            // First, run migration to update existing database
            MigrateDatabase();
            
            // Then initialize (creates tables if they don't exist)
            Initialize();
        }

        public SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private void Initialize()
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                -- Enable foreign key support (if not already enabled)
                PRAGMA foreign_keys = ON;

                -- Students table
                CREATE TABLE IF NOT EXISTS Students (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    ClassName TEXT NOT NULL,
                    FingerprintId INTEGER NOT NULL UNIQUE,
                    FingerprintTemplate BLOB NOT NULL,
                    Synced INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                -- Guardians table
                CREATE TABLE IF NOT EXISTS Guardians (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    FingerprintId INTEGER NOT NULL UNIQUE,
                    FingerprintTemplate BLOB NOT NULL,
                    Synced INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                -- GuardianStudents junction table
                CREATE TABLE IF NOT EXISTS GuardianStudents (
                    GuardianId INTEGER NOT NULL,
                    StudentId INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (GuardianId, StudentId)
                );

                -- AuditLogs table - SIMPLIFIED VERSION that matches your AuditLogService
                CREATE TABLE IF NOT EXISTS AuditLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    EventType TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Details TEXT,
                    Timestamp TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    SessionId TEXT NOT NULL,
                    UserName TEXT NOT NULL,
                    MachineName TEXT NOT NULL,
                    StudentId INTEGER,
                    GuardianId INTEGER,
                    Success INTEGER NOT NULL DEFAULT 1,
                    ErrorMessage TEXT
                );

                -- Pickup Logs table
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

                -- FingerprintCounter table
                CREATE TABLE IF NOT EXISTS FingerprintCounter (
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    NextFingerprintId INTEGER NOT NULL
                );

                -- Initialize FingerprintCounter if empty
                INSERT OR IGNORE INTO FingerprintCounter (Id, NextFingerprintId)
                VALUES (1, 1);
            ";

            cmd.ExecuteNonQuery();
        }

        public int GetNextFingerprintId()
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();

            using var select = conn.CreateCommand();
            select.CommandText =
                "SELECT NextFingerprintId FROM FingerprintCounter WHERE Id = 1";
            select.Transaction = tx;

            int nextId = Convert.ToInt32(select.ExecuteScalar());

            using var update = conn.CreateCommand();
            update.CommandText =
                "UPDATE FingerprintCounter SET NextFingerprintId = @n WHERE Id = 1";
            update.Parameters.AddWithValue("@n", nextId + 1);
            update.Transaction = tx;
            update.ExecuteNonQuery();

            tx.Commit();
            return nextId;
        }

        private void MigrateDatabase()
        {
            try
            {
                using var conn = OpenConnection();
                
                // Check if we need to migrate by seeing if Success column exists
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = @"
                    SELECT COUNT(*) FROM pragma_table_info('AuditLogs') WHERE name = 'Success';
                ";
                
                var successColumnExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                
                if (!successColumnExists)
                {
                    // We need to migrate the AuditLogs table
                    using var migrateCmd = conn.CreateCommand();
                    
                    // SQLite doesn't support adding multiple columns in one statement easily
                    // We'll create a new table and copy data
                    migrateCmd.CommandText = @"
                        -- Create a backup of the old table
                        CREATE TABLE IF NOT EXISTS AuditLogs_Backup AS SELECT * FROM AuditLogs;
                        
                        -- Drop the old table
                        DROP TABLE IF EXISTS AuditLogs;
                        
                        -- Create the new table with all required columns
                        CREATE TABLE AuditLogs (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            EventType TEXT NOT NULL,
                            Description TEXT NOT NULL,
                            Details TEXT,
                            Timestamp TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            SessionId TEXT NOT NULL DEFAULT 'legacy',
                            UserName TEXT NOT NULL DEFAULT 'system',
                            MachineName TEXT NOT NULL DEFAULT 'unknown',
                            StudentId INTEGER,
                            GuardianId INTEGER,
                            Success INTEGER NOT NULL DEFAULT 1,
                            ErrorMessage TEXT
                        );
                        
                        -- Copy data from backup, filling in default values for new columns
                        INSERT INTO AuditLogs (Id, EventType, Description, Timestamp, StudentId, GuardianId)
                        SELECT Id, EventType, Description, Timestamp, StudentId, GuardianId 
                        FROM AuditLogs_Backup;
                        
                        -- Drop the backup table
                        DROP TABLE IF EXISTS AuditLogs_Backup;
                    ";
                    
                    migrateCmd.ExecuteNonQuery();
                    Console.WriteLine("Database migrated: Added new columns to AuditLogs table.");
                }
                
                // Check and migrate other tables if needed
                CheckAndAddColumn(conn, "Students", "CreatedAt", "TEXT DEFAULT CURRENT_TIMESTAMP");
                CheckAndAddColumn(conn, "Students", "UpdatedAt", "TEXT DEFAULT CURRENT_TIMESTAMP");
                CheckAndAddColumn(conn, "Guardians", "CreatedAt", "TEXT DEFAULT CURRENT_TIMESTAMP");
                CheckAndAddColumn(conn, "Guardians", "UpdatedAt", "TEXT DEFAULT CURRENT_TIMESTAMP");
                
            }
            catch (Exception ex)
            {
                // Log but don't crash - the Initialize() will create tables if needed
                Console.WriteLine($"Migration note: {ex.Message}");
            }
        }
        
        private void CheckAndAddColumn(SqliteConnection conn, string tableName, string columnName, string columnDefinition)
        {
            try
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = $@"
                    SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = '{columnName}';
                ";
                
                var columnExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                
                if (!columnExists)
                {
                    using var addCmd = conn.CreateCommand();
                    addCmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
                    addCmd.ExecuteNonQuery();
                    Console.WriteLine($"Added column {columnName} to {tableName} table.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to check/add column {columnName} to {tableName}: {ex.Message}");
            }
        }
    }
}