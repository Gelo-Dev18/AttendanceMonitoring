using AttendanceMonitoring.Models;
using Microsoft.Identity.Client;

namespace AttendanceMonitoring.ViewModel.Teacher
{
    public class MySecretariesViewModel
    {
        public List<AppUser> Secretaries { get; set;  } = new List<AppUser>();

        public List<SecretaryAssignment> Secretary { get; set; } = new List<SecretaryAssignment>();


    }
}
