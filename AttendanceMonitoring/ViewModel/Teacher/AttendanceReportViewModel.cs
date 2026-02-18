using AttendanceMonitoring.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AttendanceMonitoring.ViewModel.Teacher
{
    public class AttendanceReportViewModel
    {
        public List<SelectListItem> teacherClass { get; set; }
        public List<SelectListItem> academicPeriod { get; set; }

        public List<AttendanceReportData> StudentAttendance { get; set; }
        public int? SelectedAcademicPeriod { get; set; }
        public int? SelectedTeacherAssignment { get; set; }
        public string? SelectedAttendanceStatus { get; set; } = null;
        public List<DateTime> DateRange { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        //public List<SelectListItem> assignedGrade { get; set; }
        //public List<SelectListItem> assignedSection { get; set; }
        //public List<SelectListItem> assignedTrack { get; set; }


    }
}
