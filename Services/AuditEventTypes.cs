namespace BiometricStudentPickup.Services
{
    public static class AuditEventTypes
    {
        // Enrollment events
        public const string StudentEnrolled = "STUDENT_ENROLLED";
        public const string StudentEnrollmentFailed = "STUDENT_ENROLLMENT_FAILED";
        public const string StudentEnrollmentCancelled = "STUDENT_ENROLLMENT_CANCELLED";
        public const string GuardianEnrolled = "GUARDIAN_ENROLLED";
        public const string GuardianEnrollmentFailed = "GUARDIAN_ENROLLMENT_FAILED";
        public const string GuardianStudentLinked = "GUARDIAN_STUDENT_LINKED";
        public const string GuardianStudentUnlinked = "GUARDIAN_STUDENT_UNLINKED";
        public const string GuardianEnrollmentCancelled = "GUARDIAN_ENROLLMENT_CANCELLED";

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

        // Attendance events
        public const string AttendanceRecorded = "ATTENDANCE_RECORDED";
        public const string AttendanceDuplicate = "ATTENDANCE_DUPLICATE";
        public const string AttendanceError = "ATTENDANCE_ERROR";
    }
}