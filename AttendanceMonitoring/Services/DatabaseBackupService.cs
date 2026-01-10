using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace AttendanceMonitoring.Services
{
    //Backup and restore uses ADO.NET
    public class DatabaseBackupService
    {
        private readonly IConfiguration _configuration; //To get connection string
        private readonly ILogger<DatabaseBackupService> _logger; //To log what's happening
        private readonly string _backupDirectory;

        //Helper method: Validate database name
        private bool IsValidDataBaseName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            //Only allow letters, numbers, underscores
            return Regex.IsMatch(name, @"^[a-zA-Z0-9_]+$");
        }

        public DatabaseBackupService(IConfiguration configuration, ILogger<DatabaseBackupService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            //path backups will be stored
            //_backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "AppData", "Backups");
            _backupDirectory = @"C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS01\MSSQL\Backup";

            ///User-friendly location: Pag need na ideploy sa school
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
        public async Task<string> SafetyBackupAsync()
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
                string backupFileName = $"{databaseName}_SAFETYBACKUP_{DateTime.Now:yyyy-MM-dd_HHmmsss}.bak";
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
            catch (Exception ex)
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

        ///<summary>
        ///Restores database from a backup file
        /// </summary>

        //need ng <RestoreResult> para makapag return ng object para gumana mismo yung REstore
        //SO actual return type ko siya
        public async Task<RestoreResult> RestoreDatabaseAsync(string backupFileName)
        {
            string safetyBackupFileName = null;

            try
            {
                //1. Validate backup file if exists
                if (string.IsNullOrEmpty(backupFileName) || !backupFileName.EndsWith(".bak"))
                {
                    throw new ArgumentNullException("Invalid backup filename");
                }

                string backupFilePath = GetBackupFilePath(backupFileName);

                if (!File.Exists(backupFilePath))
                {
                    //dollar sign - String interpoliation
                    throw new ArgumentNullException($"Backup file not found: {backupFileName}");
                }

                //2.Get database name
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                var builder = new SqlConnectionStringBuilder(connectionString);
                string databaseName = builder.InitialCatalog;

                if (!IsValidDataBaseName(databaseName))
                {
                    throw new ArgumentException($"Invalide database name{databaseName}");
                }

                _logger.LogWarning("DATABASE RESTORE STARTED - Database: {Database}, From: {Backup}", databaseName, backupFileName);

                //3. Create safety backup before restore para if ever na mag restore agad yung Admin tapos mas old data pa
                _logger.LogInformation("Creating safety backup before restore");
                var safetyBackup = await SafetyBackupAsync();
                safetyBackupFileName = safetyBackup; //check bakit di gumagana  yung safetyBackup.FileName
                _logger.LogInformation("Safety backup created: {SafetyBackup}", safetyBackupFileName);

                //4. Connect to master database
                builder.InitialCatalog = "master";
                string masterConnectionString = builder.ConnectionString;

                //Auto cleanup kahit may error
                //Hindi lang file / database = Lahat ng IDisposable objects
                //Basically, anumang resource na kailangan ng manual cleanup dapat gumamit ng using!
                using (var connection = new SqlConnection(masterConnectionString))
                {
                    await connection.OpenAsync();

                    //5.Set database to single-user mode(disconnect all users)
                    _logger.LogInformation("Setting database to singel-user mode");
                    string singelUserSql = $@"
                        ALTER DATABASE [{databaseName}]
                        SET SINGLE_USER
                        WITH ROLLBACK IMMEDIATE;
                    ";

                    using (var command = new SqlCommand(singelUserSql, connection))
                    {
                        command.CommandTimeout = 300;
                        await command.ExecuteNonQueryAsync();
                    }

                    try
                    {
                        //6. Restore database
                        _logger.LogInformation("Restoring database from: {Backup}", backupFileName);
                        string restoreSql = $@"
                            RESTORE DATABASE [{databaseName}]
                            FROM DISK = @BackupPath
                            WITH REPLACE;
                            ";

                        using (var command = new SqlCommand(restoreSql, connection))
                        {
                            command.CommandTimeout = 600; //10 minutes
                            command.Parameters.AddWithValue("@BackupPath", backupFilePath);

                            await command.ExecuteNonQueryAsync();
                        }

                        _logger.LogInformation("Database restored Successfully");
                    }
                    finally
                    {
                        //7.Set database back to multi-user mode
                        _logger.LogInformation("Setting database back to multi-user mode");
                        string multiUserSql = $@"
                            ALTER DATABASE [{databaseName}]
                            SET MULTI_USER;
                            ";

                        using (var command = new SqlCommand(multiUserSql, connection))
                        {
                            command.CommandTimeout = 300;
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                _logger.LogWarning("DATABASE RESTORE COMPLETED - Database: {Database}, From: {Backup}, Safety Backup: {SafetyBackup}",
                    //this three are paremeters to display the value of the placeholder ex."{Database}"
                    databaseName, backupFileName, safetyBackupFileName);

                return new RestoreResult
                {
                    Success = true,
                    DatabaseName = databaseName,
                    RestoredFrom = backupFileName,
                    SafetyBackupCreated = safetyBackupFileName,
                    RestoredAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DATABASE RESTORED FAILED - Backup: {Backup}", backupFileName);

                string errorMessage = "Failed to restore database";

                if (!string.IsNullOrEmpty(safetyBackupFileName))
                {
                    errorMessage += $"Your current data was backup up to: {safetyBackupFileName}";
                }

                throw new Exception(errorMessage, ex);
            }
        }
    } 

    //Simple class to hold backup file info

    //public class BackupFileInfo
    //{
    //    public string FileName { get; set; }
    //    public DateTime CreatedDate { get; set; }
    //    public double SizeMB { get; set; }

    //}

    
}
