using AttendanceMonitoring.Helper;
using AttendanceMonitoring.Services;

namespace AttendanceMonitoring.ViewModel
{
    public class BackupViewModel
    {
        public PaginatedResult<BackupFileInfo> PaginatedBackups { get; set; }

        public List<BackupFileInfo> RecentBackupsForRestore { get; set; }
        public string SearchKeyword { get; set; }
    }
}
