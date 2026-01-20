using AttendanceMonitoring.Helper;
using AttendanceMonitoring.Models;

namespace AttendanceMonitoring.Contracts
{
    public interface IActivityLogRepository: IBaseRepository<ActivityLog>
    {
        Task<PaginatedResult<ActivityLog>> GetPaginated(int page, int pageSize, string keyword = null);
    }
}
