using AttendanceMonitoring.Data;
using AttendanceMonitoring.Models;

namespace AttendanceMonitoring.Services
{
    public class ActivityLogServices : IActivityLogService
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogServices(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActivity(string actionType, string entityName, string entityId, string userId, int schoolId, string details, string username)
        {
          

            var log = new ActivityLog
            {
                UserId = userId,
                SchoolId = schoolId,
                Username = username,
                ActionType = actionType,
                EntityName = entityName,
                EntityId = entityId,
                Details = details ?? $"{actionType}",
                TimeActivityCommited = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
            
        }
    }
}
