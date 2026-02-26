namespace AttendanceMonitoring.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string SchoolId { get; set; }
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string ActionType { get; set; }
        public string Details { get; set; }
        public DateTime TimeActivityCommited { get; set; }

        public DateTime CreatedAt { get; set; } 
    }
}
