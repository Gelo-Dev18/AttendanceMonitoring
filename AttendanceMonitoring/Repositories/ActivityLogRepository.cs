using AttendanceMonitoring.Contracts;
using AttendanceMonitoring.Data;
using AttendanceMonitoring.Helper;
using AttendanceMonitoring.Models;
using AttendanceMonitoring.Services;

namespace AttendanceMonitoring.Repositories
{
    public class ActivityLogRepository: BaseRepository<ActivityLog>, IActivityLogRepository
    {
        public ActivityLogRepository(ApplicationDbContext context) : base(context)
        {

        }
        public async Task<PaginatedResult<ActivityLog>> GetPaginated(int page, int pageSize, string keyword = null)
        {
            return await GetPaginated(page, pageSize, 
                t => t.Username.Contains(keyword ?? string.Empty) || 
                t.ActionType.Contains(keyword ?? string.Empty) ||
                t.Details.Contains(keyword ?? string.Empty) ||
                t.SchoolId.ToString().Contains(keyword ?? string.Empty));
        }
    }
}
