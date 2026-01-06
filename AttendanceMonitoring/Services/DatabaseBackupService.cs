using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace AttendanceMonitoring.Services
{
    public class DatabaseBackupService
    {
        private readonly IConfiguration _configuration; //To get connection string
        private readonly ILogger<DatabaseBackupService> _logger; //To log what's happening
        private readonly string _backupDirectory;

        public DatabaseBackupService(IConfiguration configuration, ILogger<DatabaseBackupService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            //path backups will be stored
            //_backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "AppData", "Backups");
            _backupDirectory = @"C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS01\MSSQL\Backup";

            ///User-friendly location
            //_backupDirectory = @"C:\AttendanceMonitoring\Backups";


            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
                _logger.LogInformation("Created backup directory: {Path}", _backupDirectory);
            }
        }

        //Creates a database backup and returns the filename
        public async Task<string> CreateBackupAsync()
        {
            try
            {
                //1. Get Connection string
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                //2.Extract database name from connection string
                var builder = new SqlConnectionStringBuilder(connectionString);
                string databaseName = builder.InitialCatalog;

                //3.Validate database name(security!)
                //Prevent SQL injection
                if (!IsValidDataBaseName(databaseName))
                {
                    throw new ArgumentException($"Invalid database name: {databaseName}");
                }

                //4.Create backup filename with timestamp
                string backupFileName = $"{databaseName}_Backup_{DateTime.Now:yyyy-MM-dd_HHmmsss}.bak";
                string backupFullPath = Path.Combine(_backupDirectory, backupFileName);

                _logger.LogInformation("Starting backup: {FileName}", backupFileName);

                //5.Connect to SQL Server's 'master' database
                //REQUIRED for backup Command
                builder.InitialCatalog = "master";
                string masterConnectionString = builder.ConnectionString;

                using (var connection = new SqlConnection(masterConnectionString))
                {
                    await connection.OpenAsync();

                    //6.Execute T-SQL Backup COMMAND
                    string backupSql = $@"
                        BACKUP DATABASE [{databaseName}] 
                        TO DISK = @BackupPath 
                        WITH FORMAT, INIT;";

                    using (var command = new SqlCommand(backupSql, connection))
                    {
                        command.CommandTimeout = 300;
                        command.Parameters.AddWithValue("@BackupPath", backupFullPath);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation("Backup completed: {FileName}", backupFileName);
                return backupFileName;

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Backup failed");
                throw new Exception("Failed to create database backup", ex);
            }
        }

        //Gets the full path of a backup file
        public string GetBackupFilePath(string filename)
        {
            //Validate filename
            if(string.IsNullOrEmpty(filename) || !filename.EndsWith(".bak"))
            {
                throw new ArgumentException("Invalid backup filename");
            }

            //Security check for directory traversal attempts
            //To prevent malicious attackers sends malicious.exe na may .. Lkaya yung validation is makakatulong
            if(filename.Contains("..") || filename.Contains("/") || filename.Contains("\\"))
            {
                throw new ArgumentException("Ivalid characters in filename");
            }
            return Path.Combine(_backupDirectory, filename);
        }

        //Lists of all available backup files
        public List<BackupFileInfo> GetAllBackups()
        {
            var backups = new List<BackupFileInfo>();

            try
            {
                if (!Directory.Exists(_backupDirectory))
                {
                    return backups;
                }

                //Get all .bak files
                var files = Directory.GetFiles(_backupDirectory, "*.bak");

                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);

                    backups.Add(new BackupFileInfo
                    {
                        FileName = fileInfo.Name,
                        CreatedDate = fileInfo.CreationTime,
                        SizeMB = Math.Round(fileInfo.Length / 1024.0 / 1024.0, 2)
                    });
                }

                return backups.OrderByDescending(b => b.CreatedDate).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list backups");
                return backups;
            }
        }
        //Helper method: Validate database name
        private bool IsValidDataBaseName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            //Only allow letters, numbers, underscores
            return Regex.IsMatch(name, @"^[a-zA-Z0-9_]+$");
        }
    }

    //Simple class to hold backup file info

    //Tanong kong para san ito
    public class BackupFileInfo
    {
        public string FileName { get; set; }
        public DateTime CreatedDate { get; set; }
        public double SizeMB { get; set; }

    }

    
}
