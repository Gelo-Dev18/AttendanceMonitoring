namespace AttendanceMonitoring.ViewModel
{
    public class AdminAttendanceReportData
    {
        public int StudentId { get; set; }
        public int StudentSectionAssignmentId { get; set; }

        public string StudentName { get; set; }
        public List<string> DailyAttendance { get; set; }
    }
}
