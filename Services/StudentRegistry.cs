// // using System.Collections.Generic;
// // using System.Linq;
// // using BiometricStudentPickup.Models;

// // namespace BiometricStudentPickup.Services
// // {
// //     public class StudentRegistry
// //     {
// //         private readonly List<Student> _students = new();
// //         private int _nextId = 1;

// //         public IReadOnlyList<Student> All => _students;

// //         public Student Add(string fullName, string className, int fingerprintId)
// //         {
// //             var student = new Student
// //             {
// //                 LocalId = _nextId++,
// //                 FullName = fullName,
// //                 ClassName = className,
// //                 FingerprintId = fingerprintId,
// //                 Synced = false
// //             };

// //             _students.Add(student);
// //             return student;
// //         }

// //         public Student? FindByFingerprint(int fingerprintId)
// //         {
// //             return _students.FirstOrDefault(s => s.FingerprintId == fingerprintId);
// //         }
// //     }
// // }

// using System;
// using System.Collections.Generic;
// using System.Linq;
// // using System.Data.SQLite;
// using Microsoft.Data.Sqlite;
// using BiometricStudentPickup.Models;

// namespace BiometricStudentPickup.Services
// {
//     public class StudentRegistry
//     {
//         private readonly List<Student> _students = new();
//         private readonly DatabaseService _db;

//         public IReadOnlyList<Student> All => _students;

//         public StudentRegistry(DatabaseService db)
//         {
//             _db = db;
//             LoadAll();
//         }

//         private void LoadAll()
//         {
//             _students.Clear();

//             using var conn = _db.OpenConnection();
//             using var cmd = conn.CreateCommand();

//             cmd.CommandText = "SELECT Id, FullName, ClassName, FingerprintId, Synced FROM Students";

//             using var reader = cmd.ExecuteReader();
//             while (reader.Read())
//             {
//                 _students.Add(new Student
//                 {
//                     LocalId = reader.GetInt32(0),
//                     FullName = reader.GetString(1),
//                     ClassName = reader.GetString(2),
//                     FingerprintId = reader.GetInt32(3),
//                     Synced = reader.GetInt32(4) == 1
//                 });
//             }
//         }

//         public Student Add(string fullName, string className, int fingerprintId)
//         {
//             using var conn = _db.OpenConnection();
//             using var cmd = conn.CreateCommand();

//             cmd.CommandText = @"
//                 INSERT INTO Students (FullName, ClassName, FingerprintId, Synced)
//                 VALUES (@name, @class, @fp, 0);
//                 SELECT last_insert_rowid();
//             ";

//             cmd.Parameters.AddWithValue("@name", fullName);
//             cmd.Parameters.AddWithValue("@class", className);
//             cmd.Parameters.AddWithValue("@fp", fingerprintId);

//             // var id = (long)cmd.ExecuteScalar();
//             var result = cmd.ExecuteScalar();

//             if (result == null || result == DBNull.Value)
//             {
//                 throw new InvalidOperationException("Failed to retrieve inserted ID.");
//             }

//             var id = (long)result;

//             var student = new Student
//             {
//                 LocalId = (int)id,
//                 FullName = fullName,
//                 ClassName = className,
//                 FingerprintId = fingerprintId,
//                 Synced = false
//             };

//             _students.Add(student);
//             return student;
//         }

//         public Student? FindByFingerprint(int fingerprintId)
//         {
//             return _students.FirstOrDefault(s => s.FingerprintId == fingerprintId);
//         }
//     }
// }


using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using BiometricStudentPickup.Models;

namespace BiometricStudentPickup.Services
{
    public class StudentRegistry
    {
        private readonly List<Student> _students = new();
        private readonly DatabaseService _db;

        public IReadOnlyList<Student> All => _students;

        public StudentRegistry(DatabaseService db)
        {
            _db = db;
            LoadAll();
        }

        private void LoadAll()
        {
            _students.Clear();

            using var conn = _db.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT 
                    Id,
                    FullName,
                    ClassName,
                    FingerprintId,
                    FingerprintTemplate,
                    Synced
                FROM Students
                ORDER BY FingerprintId ASC
            ";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                _students.Add(new Student
                {
                    LocalId = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    ClassName = reader.GetString(2),
                    FingerprintId = reader.GetInt32(3),
                    FingerprintTemplate = (byte[])reader["FingerprintTemplate"],
                    Synced = reader.GetInt32(5) == 1
                });
            }
        }

        // public Student Add(string fullName, string className, byte[] template)
        // {
        //     // Generate fingerprint ID inside registry
        //     int fingerprintId = GetNextFingerprintId();

        //     using var conn = _db.OpenConnection();
        //     using var cmd = conn.CreateCommand();

        //     cmd.CommandText = @"
        //         INSERT INTO Students 
        //             (FullName, ClassName, FingerprintId, FingerprintTemplate, Synced)
        //         VALUES 
        //             (@name, @class, @fpId, @template, 0);
        //         SELECT last_insert_rowid();
        //     ";

        //     cmd.Parameters.AddWithValue("@name", fullName);
        //     cmd.Parameters.AddWithValue("@class", className);
        //     cmd.Parameters.AddWithValue("@fpId", fingerprintId);
        //     cmd.Parameters.AddWithValue("@template", template);

        //     var result = cmd.ExecuteScalar();
        //     if (result == null || result == DBNull.Value)
        //         throw new InvalidOperationException("Failed to retrieve inserted student ID.");

        //     var student = new Student
        //     {
        //         LocalId = (int)(long)result,
        //         FullName = fullName,
        //         ClassName = className,
        //         FingerprintId = fingerprintId,
        //         FingerprintTemplate = template,
        //         Synced = false
        //     };

        //     _students.Add(student);
        //     return student;
        // }
        public Student Add(
            string fullName,
            string className,
            int fingerprintId,
            byte[] fingerprintTemplate
        )
        {
            using var conn = _db.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                INSERT INTO Students 
                (FullName, ClassName, FingerprintId, FingerprintTemplate, Synced)
                VALUES (@name, @class, @fp, @tpl, 0);
                SELECT last_insert_rowid();
            ";

            cmd.Parameters.AddWithValue("@name", fullName);
            cmd.Parameters.AddWithValue("@class", className);
            cmd.Parameters.AddWithValue("@fp", fingerprintId);
            cmd.Parameters.Add("@tpl", SqliteType.Blob).Value = fingerprintTemplate;

            // var id = (long)cmd.ExecuteScalar();
            var result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException("Failed to retrieve inserted ID.");
            }

            var id = (long)result;

            var student = new Student
            {
                LocalId = (int)id,
                FullName = fullName,
                ClassName = className,
                FingerprintId = fingerprintId,
                FingerprintTemplate = fingerprintTemplate,
                Synced = false
            };

            _students.Add(student);
            return student;
        }


        public Student? FindByFingerprint(int fingerprintId)
        {
            return _students.FirstOrDefault(s => s.FingerprintId == fingerprintId);
        }

        public Student? FindByTemplate(byte[] template)
        {
            return _students.FirstOrDefault(
                s => s.FingerprintTemplate.SequenceEqual(template)
            );
        }

        private int GetNextFingerprintId()
        {
            return _students.Count == 0
                ? 1
                : _students.Max(s => s.FingerprintId) + 1;
        }

        public bool Remove(int studentId)
        {
            using var conn = _db.OpenConnection();
            using var cmd = conn.CreateCommand();
            
            cmd.CommandText = "DELETE FROM Students WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", studentId);
            int rowsAffected = cmd.ExecuteNonQuery();
            
            if (rowsAffected > 0)
            {
                // Remove from in-memory list
                var student = _students.FirstOrDefault(s => s.LocalId == studentId);
                if (student != null)
                {
                    _students.Remove(student);
                    
                    // Also need to clean up guardian-student relationships
                    using var conn2 = _db.OpenConnection();
                    using var cmd2 = conn2.CreateCommand();
                    cmd2.CommandText = "DELETE FROM GuardianStudents WHERE StudentId = @studentId";
                    cmd2.Parameters.AddWithValue("@studentId", studentId);
                    cmd2.ExecuteNonQuery();
                }
            }
            
            return rowsAffected > 0;
        }

        public void Refresh()
        {
            LoadAll(); // This method already exists and reloads from database
        }
    }
}
