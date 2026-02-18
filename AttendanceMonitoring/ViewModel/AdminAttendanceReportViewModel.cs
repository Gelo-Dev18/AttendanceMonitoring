using AttendanceMonitoring.ViewModel;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AttendanceMonitoring.ViewModel
{
    public class AdminAttendanceReportViewModel
    {
        public List<SelectListItem> teacherList { get; set; }
        public List<SelectListItem> teacherClass { get; set; }
        public List<SelectListItem> academicPeriod { get; set; }
        public List<AdminAttendanceReportData> StudentAttendance { get; set; }
        public string? SelectedTeacher { get; set; }
        public int? SelectedAcademicPeriod { get; set; }
        public int? SelectedTeacherAssignment { get; set; }
        public string? SelectedAttendanceStatus { get; set; } = null;
        public List<DateTime> DateRange { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
