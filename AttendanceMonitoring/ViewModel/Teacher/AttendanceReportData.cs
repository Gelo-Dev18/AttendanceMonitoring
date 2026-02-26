namespace AttendanceMonitoring.ViewModel.Teacher
{
    public class AttendanceReportData
    {
        public int StudentId { get; set; }
        public int StudentSectionAssignmentId { get; set; }
        public string StudentName { get; set; }
        public List<string> DailyAttendance { get; set; }
    }
}
