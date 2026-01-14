using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoring.Helper
{
    public class PaginatedRequest
    {
        public const int ITEM_PER_PAGE = 10;

        [FromQuery(Name = "p")] //For model binding from URl query string. Ginagamit for custom parameter names lalo na for shorter parameter names
        public int PageNumber { get; set; } = 1;
        [FromQuery(Name = "s")]
        public string? SearchKeyword { get; set;}
    }
}
