namespace AttendanceMonitoring.Services
{
    public interface IActivityLogService
    {
        Task LogActivity(string actionType, string entityName, string entityId, string userId, string schoolId, string details, string username);
    }
}
