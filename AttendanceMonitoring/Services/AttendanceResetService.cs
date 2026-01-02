
using AttendanceMonitoring.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AttendanceMonitoring.Services
{
                 //Child Class            //Parent class
    public class AttendanceResetService : BackgroundService 
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AttendanceResetService> _logger;
        //private readonly IConfiguration _configuration;

        public AttendanceResetService(IServiceProvider serviceProvider, ILogger<AttendanceResetService> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            //_configuration = configuration;
        }

        //Overrides method from BackgroundService class
                                    //Yung ExecuteAsync is galing sa BackgroundService class
                                                //CancellationToken = signals kung kailan dapat huminto yung service(Pag nag shutdown yung app)
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //protected means:
        //Pwede lang gamitin sa:
        //Loob ng parent class
        //Loob ng child class
        {
            //prints the message in console/logs once the project started
            _logger.LogInformation("Attendance Cleanup Service started");

            //Loop //!stopToken.IsCancellationRequested = patuloy na tatakbo habang hindi pa nag request ng cancellation
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now; //store current date
                var nextDay = DateTime.Today.AddDays(1); //variable to set target time to run the reset
                var delay = nextDay - now; // subtract time from target time

                _logger.LogInformation("Next cleanup scheduled at: {NextRun}", nextDay);
                                //how long to wait 
                                        //Para ma-cancel yung wait kapag mag stop yung app
                await Task.Delay(delay, stoppingToken); 
                await ResetAttendanceData();//call resetattendancedata. The actual reset operation

            }
        }

        private async Task ResetAttendanceData()
        {

            _logger.LogInformation("Starting attendance reset at {Time}", DateTime.Now);
            //create temporary scope for services
            using var scope = _serviceProvider.CreateScope();//create temporay scope to get Dbcontext
            //Can be use to query into the database
                                                 //GetRequiredService - get instance ng DbContext
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); //Dbcontext is available cause it is registered in Program.cs

            _logger.LogInformation("Attendance reset completed - ready for new day");

        }


    }
}
