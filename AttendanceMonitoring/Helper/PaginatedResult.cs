namespace AttendanceMonitoring.Helper
{
    public class PaginatedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public string? SearchKeyword {get;set;}
        public IEnumerable<T>? Result { get; set; }
    }
}
