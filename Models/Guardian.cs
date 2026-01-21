using System;

namespace BiometricStudentPickup.Models
{
    public class Guardian
    {
        public int LocalId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public int FingerprintId { get; init; }
        public byte[] FingerprintTemplate { get; init; } = Array.Empty<byte>();
        public bool Synced { get; set; }
    }
}
