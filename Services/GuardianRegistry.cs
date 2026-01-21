// // using System.Collections.Generic;
// // using System.Linq;
// // using BiometricStudentPickup.Models;

// // namespace BiometricStudentPickup.Services
// // {
// //     public class GuardianRegistry
// //     {
// //         private readonly List<Guardian> _guardians = new();
// //         private int _nextId = 1;

// //         public IReadOnlyList<Guardian> All => _guardians;

// //         public Guardian Add(string fullName, int fingerprintId)
// //         {
// //             var guardian = new Guardian
// //             {
// //                 LocalId = _nextId++,
// //                 FullName = fullName,
// //                 FingerprintId = fingerprintId,
// //                 Synced = false
// //             };

// //             _guardians.Add(guardian);
// //             return guardian;
// //         }

// //         public Guardian? FindByFingerprint(int fingerprintId)
// //         {
// //             return _guardians.FirstOrDefault(g => g.FingerprintId == fingerprintId);
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
//     public class GuardianRegistry
//     {
//         private readonly List<Guardian> _guardians = new();
//         private readonly DatabaseService _db;

//         public IReadOnlyList<Guardian> All => _guardians;

//         public GuardianRegistry(DatabaseService db)
//         {
//             _db = db;
//             LoadAll();
//         }

//         private void LoadAll()
//         {
//             _guardians.Clear();

//             using var conn = _db.OpenConnection();
//             using var cmd = conn.CreateCommand();

//             cmd.CommandText =
//                 "SELECT Id, FullName, FingerprintId, Synced FROM Guardians";

//             using var reader = cmd.ExecuteReader();
//             while (reader.Read())
//             {
//                 _guardians.Add(new Guardian
//                 {
//                     LocalId = reader.GetInt32(0),
//                     FullName = reader.GetString(1),
//                     FingerprintId = reader.GetInt32(2),
//                     Synced = reader.GetInt32(3) == 1
//                 });
//             }
//         }

//         public Guardian Add(string fullName, int fingerprintId)
//         {
//             using var conn = _db.OpenConnection();
//             using var cmd = conn.CreateCommand();

//             cmd.CommandText = @"
//                 INSERT INTO Guardians (FullName, FingerprintId, Synced)
//                 VALUES (@name, @fp, 0);
//                 SELECT last_insert_rowid();
//             ";

//             cmd.Parameters.AddWithValue("@name", fullName);
//             cmd.Parameters.AddWithValue("@fp", fingerprintId);

//             // var id = (long)cmd.ExecuteScalar();
//             var result = cmd.ExecuteScalar();

//             if (result == null || result == DBNull.Value)
//             {
//                 throw new InvalidOperationException("Failed to retrieve inserted ID.");
//             }

//             var id = (long)result;

//             var guardian = new Guardian
//             {
//                 LocalId = (int)id,
//                 FullName = fullName,
//                 FingerprintId = fingerprintId,
//                 Synced = false
//             };

//             _guardians.Add(guardian);
//             return guardian;
//         }

//         public Guardian? FindByFingerprint(int fingerprintId)
//         {
//             return _guardians.FirstOrDefault(g => g.FingerprintId == fingerprintId);
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
    public class GuardianRegistry
    {
        private readonly List<Guardian> _guardians = new();
        private readonly DatabaseService _db;

        public IReadOnlyList<Guardian> All => _guardians;

        public GuardianRegistry(DatabaseService db)
        {
            _db = db;
            LoadAll();
        }

        private void LoadAll()
        {
            _guardians.Clear();

            using var conn = _db.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT
                    Id,
                    FullName,
                    FingerprintId,
                    FingerprintTemplate,
                    Synced
                FROM Guardians
                ORDER BY FingerprintId ASC
            ";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                _guardians.Add(new Guardian
                {
                    LocalId = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    FingerprintId = reader.GetInt32(2),
                    FingerprintTemplate = (byte[])reader["FingerprintTemplate"],
                    Synced = reader.GetInt32(4) == 1
                });
            }
        }

        // public Guardian Add(string fullName, byte[] template)
        // {
        //     int fingerprintId = GetNextFingerprintId();

        //     using var conn = _db.OpenConnection();
        //     using var cmd = conn.CreateCommand();

        //     cmd.CommandText = @"
        //         INSERT INTO Guardians
        //             (FullName, FingerprintId, FingerprintTemplate, Synced)
        //         VALUES
        //             (@name, @fpId, @template, 0);
        //         SELECT last_insert_rowid();
        //     ";

        //     cmd.Parameters.AddWithValue("@name", fullName);
        //     cmd.Parameters.AddWithValue("@fpId", fingerprintId);
        //     cmd.Parameters.AddWithValue("@template", template);

        //     var result = cmd.ExecuteScalar();
        //     if (result == null || result == DBNull.Value)
        //         throw new InvalidOperationException("Failed to retrieve inserted guardian ID.");

        //     var guardian = new Guardian
        //     {
        //         LocalId = (int)(long)result,
        //         FullName = fullName,
        //         FingerprintId = fingerprintId,
        //         FingerprintTemplate = template,
        //         Synced = false
        //     };

        //     _guardians.Add(guardian);
        //     return guardian;
        // }
        public Guardian Add(
            string fullName,
            int fingerprintId,
            byte[] fingerprintTemplate
        )
        {
            using var conn = _db.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                INSERT INTO Guardians 
                (FullName, FingerprintId, FingerprintTemplate, Synced)
                VALUES (@name, @fp, @tpl, 0);
                SELECT last_insert_rowid();
            ";

            cmd.Parameters.AddWithValue("@name", fullName);
            cmd.Parameters.AddWithValue("@fp", fingerprintId);
            cmd.Parameters.Add("@tpl", SqliteType.Blob).Value = fingerprintTemplate;

            // var id = (long)cmd.ExecuteScalar();
            var result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException("Failed to retrieve inserted ID.");
            }

            var id = (long)result;


            var guardian = new Guardian
            {
                LocalId = (int)id,
                FullName = fullName,
                FingerprintId = fingerprintId,
                FingerprintTemplate = fingerprintTemplate,
                Synced = false
            };

            _guardians.Add(guardian);
            return guardian;
        }


        public Guardian? FindByFingerprint(int fingerprintId)
        {
            return _guardians.FirstOrDefault(g => g.FingerprintId == fingerprintId);
        }

        public Guardian? FindByTemplate(byte[] template)
        {
            return _guardians.FirstOrDefault(
                g => g.FingerprintTemplate.SequenceEqual(template)
            );
        }

        private int GetNextFingerprintId()
        {
            return _guardians.Count == 0
                ? 1
                : _guardians.Max(g => g.FingerprintId) + 1;
        }

        public bool Remove(int guardianId)
        {
            using var conn = _db.OpenConnection();
            using var cmd = conn.CreateCommand();
            
            // First delete guardian-student relationships
            cmd.CommandText = "DELETE FROM GuardianStudents WHERE GuardianId = @id";
            cmd.Parameters.AddWithValue("@id", guardianId);
            cmd.ExecuteNonQuery();
            
            // Then delete the guardian
            cmd.CommandText = "DELETE FROM Guardians WHERE Id = @id";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@id", guardianId);
            int rowsAffected = cmd.ExecuteNonQuery();
            
            if (rowsAffected > 0)
            {
                // Remove from in-memory list
                var guardian = _guardians.FirstOrDefault(g => g.LocalId == guardianId);
                if (guardian != null)
                {
                    _guardians.Remove(guardian);
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
