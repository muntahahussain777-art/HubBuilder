using HubBuilders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HubBuilders.View
{
    public partial class Backup_RestoreForm : Sample
    {
        public Backup_RestoreForm()
        {
            InitializeComponent();
            Load += Backup_RestoreForm_Load;
        }

        private void Backup_RestoreForm_Load(object sender, EventArgs e)
        {
            try { WireAutoBackupUi(); } catch { }
        }

        private ComboBox _cmbInterval;
        private Label _lblAuto;

        private void WireAutoBackupUi()
        {
            if (_cmbInterval != null) return;
            Height += 92;
            _lblAuto = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(19, 292),
                Text = "Auto backup: " + (string.IsNullOrWhiteSpace(HubAutoBackup.Root)
                    ? "(drive select karein)"
                    : HubAutoBackup.Root + "HubBuilders\\yyyy-MM-dd")
            };
            _cmbInterval = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI Semibold", 10F),
                Location = new Point(19, 318),
                Size = new Size(200, 28)
            };
            _cmbInterval.Items.AddRange(new object[] { "After each entry", "Every 3 hours", "Every 1 day" });
            string iv = HubAutoBackup.Interval;
            _cmbInterval.SelectedIndex = iv == "entry" ? 0 : iv == "3h" ? 1 : 2;
            _cmbInterval.SelectedIndexChanged += (s, ev) =>
            {
                int i = _cmbInterval.SelectedIndex;
                HubAutoBackup.Interval = i == 0 ? "entry" : i == 1 ? "3h" : "1d";
            };
            var btnDrive = new Guna.UI2.WinForms.Guna2Button
            {
                Animated = true,
                AutoRoundedCorners = true,
                BorderRadius = 15,
                FillColor = Color.FromArgb(112, 51, 255),
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(230, 316),
                Size = new Size(170, 33),
                Text = "Choose Drive"
            };
            btnDrive.Click += (s, ev) =>
            {
                HubAutoBackup.PromptDrive(this);
                _lblAuto.Text = "Auto backup: " + (string.IsNullOrWhiteSpace(HubAutoBackup.Root)
                    ? "(drive select karein)"
                    : HubAutoBackup.Root + "HubBuilders\\yyyy-MM-dd");
            };
            Controls.Add(_lblAuto);
            Controls.Add(_cmbInterval);
            Controls.Add(btnDrive);
            _lblAuto.BringToFront();
            _cmbInterval.BringToFront();
            btnDrive.BringToFront();
        }
        public static string ConnectionString => conReturn();

        // Method to return the connection string for SQLite
        public static string conReturn()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbFolderPathAppData = Path.Combine(appDataFolder, "RealEstateBuilders");
            string dbFilePath = Path.Combine(dbFolderPathAppData, "RealEstateBuilders.db");

            // Ensure directory exists and the database file exists
            if (!File.Exists(dbFilePath))
            {
                if (!Directory.Exists(dbFolderPathAppData))
                    Directory.CreateDirectory(dbFolderPathAppData);

                using (FileStream fs = File.Create(dbFilePath))
                {
                    fs.Close();
                }
            }

            return $@"Data Source={dbFilePath};Version=3;";
        }

        // Backup Button: Backs up the database
        private void btnBackup_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBackupPath.Text))
            {
                MessageBox.Show("Please select a valid backup path.");
                return;
            }

            string backupFilePath = Path.Combine(txtBackupPath.Text, "RealEstateBuilders_backup.db");

            try
            {
                BackupDatabase(backupFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during backup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Restore Button: Restores the database from a backup file
        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRestorePath.Text))
            {
                MessageBox.Show("Please select a valid restore file.");
                return;
            }

            if (MessageBox.Show(
                "Client ka purana data restore hoga (RealEstateBuilders.db).\n" +
                "Restore ke baad app band karke dubara chalayein.\n\nContinue?",
                "Restore Client Data",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                RestoreDatabase(txtRestorePath.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during restore: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Method to backup the SQLite database file to a new location using SQLite Backup API
        public static void BackupDatabase(string backupFilePath)
        {
            string databaseFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RealEstateBuilders", "RealEstateBuilders.db");

            try
            {
                // Create a backup connection
                using (SQLiteConnection sourceConnection = new SQLiteConnection($@"Data Source={databaseFilePath};Version=3;"))
                using (SQLiteConnection backupConnection = new SQLiteConnection($@"Data Source={backupFilePath};Version=3;"))
                {
                    sourceConnection.Open();
                    backupConnection.Open();

                    // Start the backup process
                    sourceConnection.BackupDatabase(backupConnection, "main", "main", -1, null, 0);

                    MessageBox.Show("Backup completed successfully.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during backup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Method to restore the SQLite database file from a backup
        public static void RestoreDatabase(string restoreFilePath)
        {
            string databaseFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RealEstateBuilders", "RealEstateBuilders.db");

            try
            {
                if (!File.Exists(restoreFilePath))
                    throw new FileNotFoundException("Restore file not found.", restoreFilePath);

                // Close shared app connection so file is not locked
                HubBuilders.AppSafety.SafeClose(MainClass.con);
                GC.Collect();
                GC.WaitForPendingFinalizers();

                File.Copy(restoreFilePath, databaseFilePath, true);
                MainClass.EnsureAdditiveSchemasAfterRestore();
                try { HubBuilders.HubSyncService.NotifyDatabaseRestored(); } catch { }
                MessageBox.Show(
                    "Restore completed successfully.\nPurana data load ho gaya.\nPlease restart the app.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during restore: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Close the form (optional)
        private void guna2Button1_Click(object sender, EventArgs e)
        {
            this.Hide();
        }



        private void btnBrowseBackup_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    txtBackupPath.Text = folderBrowser.SelectedPath;
                    try
                    {
                        if (string.IsNullOrWhiteSpace(HubAutoBackup.Root))
                        {
                            string drive = Path.GetPathRoot(folderBrowser.SelectedPath);
                            if (!string.IsNullOrWhiteSpace(drive)) HubAutoBackup.Root = drive;
                        }
                    }
                    catch { }
                }
            }
        }

        // Browse Button for restore file
        private void btnBrowseRestore_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "SQLite Database Files (*.db)|*.db";
                openFileDialog.Title = "Select the SQLite Database File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtRestorePath.Text = openFileDialog.FileName;
                }
            }
        }
    }
}
