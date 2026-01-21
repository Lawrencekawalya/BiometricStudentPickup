// using System.Collections.Generic;
// using System.Linq;

// namespace BiometricStudentPickup.Services
// {
//     public class GuardianStudentRegistry
//     {
//         private readonly List<(int GuardianId, int StudentId)> _links = new();

//         public void Link(int guardianLocalId, IEnumerable<int> studentLocalIds)
//         {
//             foreach (var studentId in studentLocalIds)
//             {
//                 if (_links.Any(l => l.GuardianId == guardianLocalId && l.StudentId == studentId))
//                     continue;

//                 _links.Add((guardianLocalId, studentId));
//             }
//         }

//         public IEnumerable<int> GetStudentsForGuardian(int guardianLocalId)
//         {
//             return _links
//                 .Where(l => l.GuardianId == guardianLocalId)
//                 .Select(l => l.StudentId);
//         }

//         public IEnumerable<int> GetGuardiansForStudent(int studentLocalId)
//         {
//             return _links
//                 .Where(l => l.StudentId == studentLocalId)
//                 .Select(l => l.GuardianId);
//         }
//     }
// }

using System.Collections.Generic;
using System.Linq;
// using System.Data.SQLite;
using Microsoft.Data.Sqlite;

namespace BiometricStudentPickup.Services
{
    public class GuardianStudentRegistry
    {
        private readonly List<(int GuardianId, int StudentId)> _links = new();
        private readonly DatabaseService _db;

        public GuardianStudentRegistry(DatabaseService db)
        {
            _db = db;
            LoadAll();
        }

        private void LoadAll()
        {
            _links.Clear();

            using var conn = _db.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText =
                "SELECT GuardianId, StudentId FROM GuardianStudents";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                _links.Add((
                    reader.GetInt32(0),
                    reader.GetInt32(1)
                ));
            }
        }

        public void Link(int guardianLocalId, IEnumerable<int> studentLocalIds)
        {
            using var conn = _db.OpenConnection();
            using var tx = conn.BeginTransaction();

            foreach (var studentId in studentLocalIds)
            {
                if (_links.Any(l =>
                        l.GuardianId == guardianLocalId &&
                        l.StudentId == studentId))
                    continue;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO GuardianStudents
                    (GuardianId, StudentId, CreatedAt)
                    VALUES (@g, @s, @t)
                ";

                cmd.Parameters.AddWithValue("@g", guardianLocalId);
                cmd.Parameters.AddWithValue("@s", studentId);
                cmd.Parameters.AddWithValue("@t", System.DateTime.UtcNow.ToString("o"));

                cmd.ExecuteNonQuery();
                _links.Add((guardianLocalId, studentId));
            }

            tx.Commit();
        }

        public IEnumerable<int> GetStudentsForGuardian(int guardianLocalId)
        {
            return _links
                .Where(l => l.GuardianId == guardianLocalId)
                .Select(l => l.StudentId);
        }

        public IEnumerable<int> GetGuardiansForStudent(int studentLocalId)
        {
            return _links
                .Where(l => l.StudentId == studentLocalId)
                .Select(l => l.GuardianId);
        }

        public void Refresh()
        {
            // Reload relationships from database
            LoadAll(); // If you have this method
            // OR reinitialize the cache/dictionary
        }
    }
}
