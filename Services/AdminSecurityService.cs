// // using System;
// // using System.IO;
// // using System.Security.Cryptography;
// // using System.Text;

// // namespace BiometricStudentPickup.Services
// // {
// //     public class AdminSecurityService
// //     {
// //         private const int SESSION_MINUTES = 5;
// //         private static readonly string PinFile =
// //             Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "admin.pin");

// //         private DateTime? _sessionExpiresAt;

// //         public bool HasPin() => File.Exists(PinFile);

// //         public void CreatePin(string pin)
// //         {
// //             File.WriteAllText(PinFile, Hash(pin));
// //         }

// //         public bool VerifyPin(string pin)
// //         {
// //             if (!HasPin()) return false;

// //             var storedHash = File.ReadAllText(PinFile);
// //             if (storedHash != Hash(pin)) return false;

// //             _sessionExpiresAt = DateTime.Now.AddMinutes(SESSION_MINUTES);
// //             return true;
// //         }

// //         public bool IsAdminSessionActive()
// //         {
// //             return _sessionExpiresAt.HasValue &&
// //                    DateTime.Now < _sessionExpiresAt.Value;
// //         }

// //         public void ClearSession()
// //         {
// //             _sessionExpiresAt = null;
// //         }

// //         private static string Hash(string input)
// //         {
// //             using var sha = SHA256.Create();
// //             var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
// //             return Convert.ToBase64String(bytes);
// //         }
// //     }
// // }

// using System;
// using System.IO;
// using System.Security.Cryptography;
// using System.Text;

// namespace BiometricStudentPickup.Services
// {
//     public class AdminSecurityService
//     {
//         private readonly AuditLogService _audit;
//         private const int SESSION_MINUTES = 5;
//         private const int MAX_ATTEMPTS = 3;
//         private const int COOLDOWN_SECONDS = 60;

//         private static readonly string PinFile =
//             Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "admin.pin");

//         private DateTime? _sessionExpiresAt;
//         private int _failedAttempts = 0;
//         private DateTime? _cooldownUntil;

//         public bool HasPin() => File.Exists(PinFile);

//         public void CreatePin(string pin)
//         {
//             File.WriteAllText(PinFile, Hash(pin));
//             ResetFailures();
//         }

//         public bool IsInCooldown()
//         {
//             if (!_cooldownUntil.HasValue) return false;

//             if (DateTime.Now >= _cooldownUntil.Value)
//             {
//                 ResetFailures();
//                 return false;
//             }

//             return true;
//         }

//         public int CooldownSecondsRemaining()
//         {
//             if (!_cooldownUntil.HasValue) return 0;
//             return Math.Max(0, (int)(_cooldownUntil.Value - DateTime.Now).TotalSeconds);
//         }

//         public bool VerifyPin(string pin, out string errorMessage)
//         {
//             errorMessage = "";

//             if (IsInCooldown())
//             {
//                 errorMessage =
//                     $"Too many failed attempts. Try again in {CooldownSecondsRemaining()} seconds.";
//                     _audit.Log(
//                         "PIN_FAILED",
//                         "Admin PIN verification failed"
//                     );
//                 return false;
//             }

//             if (!HasPin())
//             {
//                 errorMessage = "Admin PIN not set.";
//                 return false;
//             }

//             var storedHash = File.ReadAllText(PinFile);
//             if (storedHash != Hash(pin))
//             {
//                 _failedAttempts++;

//                 if (_failedAttempts >= MAX_ATTEMPTS)
//                 {
//                     _cooldownUntil = DateTime.Now.AddSeconds(COOLDOWN_SECONDS);
//                     errorMessage = "Too many failed attempts. Enrollment locked temporarily.";
//                 }
//                 else
//                 {
//                     errorMessage =
//                         $"Incorrect PIN. Attempts remaining: {MAX_ATTEMPTS - _failedAttempts}.";
//                 }

//                 return false;
//             }

//             // SUCCESS
//             ResetFailures();
//             _sessionExpiresAt = DateTime.Now.AddMinutes(SESSION_MINUTES);
//             _audit.Log(
//                 "PIN_SUCCESS",
//                 "Admin PIN verified successfully"
//             );
//             return true;
//         }

//         public bool IsAdminSessionActive()
//         {
//             return _sessionExpiresAt.HasValue &&
//                    DateTime.Now < _sessionExpiresAt.Value;
//         }

//         public void ClearSession()
//         {
//             _sessionExpiresAt = null;
//         }

//         private void ResetFailures()
//         {
//             _failedAttempts = 0;
//             _cooldownUntil = null;
//         }

//         private static string Hash(string input)
//         {
//             using var sha = SHA256.Create();
//             var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
//             return Convert.ToBase64String(bytes);
//         }

//         public AdminSecurityService(AuditLogService audit)
//         {
//             _audit = audit;
//         }
//     }
// }

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BiometricStudentPickup.Services
{
    public class AdminSecurityService
    {
        private readonly AuditLogService _audit;

        private const int SESSION_MINUTES = 5;
        private const int MAX_ATTEMPTS = 3;
        private const int COOLDOWN_SECONDS = 60;

        private static readonly string PinFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "admin.pin");

        private DateTime? _sessionExpiresAt;
        private int _failedAttempts = 0;
        private DateTime? _cooldownUntil;

        public AdminSecurityService(AuditLogService audit)
        {
            _audit = audit;
        }

        public bool HasPin() => File.Exists(PinFile);

        public void CreatePin(string pin)
        {
            File.WriteAllText(PinFile, Hash(pin));
            ResetFailures();

            _audit.Log(
                "PIN_CREATED",
                "Admin PIN created"
            );
        }

        public bool IsInCooldown()
        {
            if (!_cooldownUntil.HasValue) return false;

            if (DateTime.Now >= _cooldownUntil.Value)
            {
                ResetFailures();
                return false;
            }

            return true;
        }

        public int CooldownSecondsRemaining()
        {
            if (!_cooldownUntil.HasValue) return 0;
            return Math.Max(0, (int)(_cooldownUntil.Value - DateTime.Now).TotalSeconds);
        }

        public bool VerifyPin(string pin, out string errorMessage)
        {
            errorMessage = "";

            if (IsInCooldown())
            {
                errorMessage =
                    $"Too many failed attempts. Try again in {CooldownSecondsRemaining()} seconds.";

                _audit.Log(
                    "PIN_BLOCKED_COOLDOWN",
                    "Admin PIN entry blocked due to cooldown"
                );

                return false;
            }

            if (!HasPin())
            {
                errorMessage = "Admin PIN not set.";
                return false;
            }

            var storedHash = File.ReadAllText(PinFile);

            if (storedHash != Hash(pin))
            {
                _failedAttempts++;

                _audit.Log(
                    "PIN_FAILED",
                    "Admin PIN verification failed"
                );

                if (_failedAttempts >= MAX_ATTEMPTS)
                {
                    _cooldownUntil = DateTime.Now.AddSeconds(COOLDOWN_SECONDS);
                    errorMessage = "Too many failed attempts. Enrollment locked temporarily.";
                }
                else
                {
                    errorMessage =
                        $"Incorrect PIN. Attempts remaining: {MAX_ATTEMPTS - _failedAttempts}.";
                }

                return false;
            }

            // SUCCESS
            ResetFailures();
            _sessionExpiresAt = DateTime.Now.AddMinutes(SESSION_MINUTES);

            _audit.Log(
                "PIN_SUCCESS",
                "Admin PIN verified successfully"
            );

            return true;
        }

        public bool IsAdminSessionActive()
        {
            return _sessionExpiresAt.HasValue &&
                   DateTime.Now < _sessionExpiresAt.Value;
        }

        public void ClearSession()
        {
            _sessionExpiresAt = null;

            _audit.Log(
                "ADMIN_SESSION_CLEARED",
                "Admin session ended"
            );
        }

        private void ResetFailures()
        {
            _failedAttempts = 0;
            _cooldownUntil = null;
        }

        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}
