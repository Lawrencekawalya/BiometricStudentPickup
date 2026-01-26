namespace BiometricStudentPickup.Models
{
    public class StudentRowViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int FingerprintId { get; set; }
        public string Guardians { get; set; } = string.Empty;
    }
}
