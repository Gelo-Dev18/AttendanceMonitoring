namespace AttendanceMonitoring.Services
{
    public class RestoreResult
    {
        public bool Success { get; set; }
        public string DatabaseName { get; set; }
        public string RestoredFrom { get; set; }
        public string SafetyBackupCreated { get; set; }
        public DateTime RestoredAt { get; set; }
    }
}
