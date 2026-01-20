using AttendanceMonitoring.Contracts;
using AttendanceMonitoring.Data;
using AttendanceMonitoring.Helper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AttendanceMonitoring.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _table;

        public BaseRepository(ApplicationDbContext context)
        {
            _context = context;
            _table = context.Set<T>();
        }
        public async Task<PaginatedResult<T>> GetPaginated(int page, int pageSize, Expression<Func<T, bool>> condition)
        {
            var count = await _table.Where(condition).CountAsync();
            var records = await _table.Where(condition)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<T>
            {
                Result = records,
                Page = page,
                TotalCount = (int)Math.Ceiling(count / (double)pageSize) // math.ceiling awalys rounds a number up to the next whole number
            };
        }
    }
}
