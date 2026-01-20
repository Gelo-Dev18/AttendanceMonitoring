using AttendanceMonitoring.Helper;
using System.Linq.Expressions;

namespace AttendanceMonitoring.Contracts
{
    public interface IBaseRepository<T>
    {
        Task<PaginatedResult<T>> GetPaginated(int page, int pageSize, Expression<Func<T, bool>> condition);
    }
}
