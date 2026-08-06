using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HubBuilders.ProjectConnection
{
    class projectconnection
    {
        // Lock file path (location to store the lock status)
        private static string lockFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RealEstateBuilders", "lockfile.txt");

        // Initialize the ConnectionString property dynamically
        public static string ConnectionString { get; internal set; } = conReturn();

        // Method to return the connection string for SQLite
        public static string conReturn()
        {
            // Check if the application is locked by checking the existence of lockfile.txt
            if (File.Exists(lockFilePath))
            {
                Console.WriteLine("Application is locked. Please unlock it before proceeding.");
                throw new InvalidOperationException("The application is locked.");
            }

            // Get the path for the SQLite database in AppData folder
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbFolderPathAppData = Path.Combine(appDataFolder, "RealEstateBuilders");

            // Ensure the directory exists in AppData
            if (!Directory.Exists(dbFolderPathAppData))
            {
                Directory.CreateDirectory(dbFolderPathAppData);
            }

            string dbFilePath = Path.Combine(dbFolderPathAppData, "RealEstateBuilders.db");

            // Ensure the database file exists in AppData folder
            if (!File.Exists(dbFilePath))
            {
                try
                {
                    string binFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RealEstateBuilders.db");
                    if (File.Exists(binFolderPath))
                    {
                        File.Copy(binFolderPath, dbFilePath);
                        Console.WriteLine("Database copied from bin to AppData.");
                    }
                    else
                    {
                        // Create empty DB file so app can start; schema helpers will create tables
                        using (FileStream fs = File.Create(dbFilePath)) { }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error copying database file: " + ex.Message);
                    try
                    {
                        if (!File.Exists(dbFilePath))
                            using (FileStream fs = File.Create(dbFilePath)) { }
                    }
                    catch { }
                }
            }

            // Return the SQLite connection string for the AppData database file
            return $@"Data Source={dbFilePath};Version=3;";
        }
    }
}
