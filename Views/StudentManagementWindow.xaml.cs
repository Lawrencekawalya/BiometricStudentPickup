using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using BiometricStudentPickup.Models;
using BiometricStudentPickup.Services;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace BiometricStudentPickup.Views
{
    public partial class StudentManagementWindow : Window
    {
        private ObservableCollection<StudentRowViewModel> _students = new();
        private ObservableCollection<StudentRowViewModel> _filtered = new();
        private readonly DatabaseService _dbService;

        // Add tracking for open enrollment windows
        private List<EnrollmentWindow> _openEnrollmentWindows = new();

        // Make these fields nullable since they can be null in parameterless constructor
        private readonly StudentRegistry? _studentRegistry;
        private readonly GuardianRegistry? _guardianRegistry;
        private readonly GuardianStudentRegistry? _guardianStudentRegistry;
        private readonly FingerprintService? _fingerprintService;
        private readonly AuditLogService? _auditLogService;
        private readonly AdminSecurityService? _adminSecurity;
        private bool _isActuallyClosing;

        // Primary constructor with all required dependencies (like EnrollmentWindow)
        public StudentManagementWindow(
            StudentRegistry studentRegistry,
            GuardianRegistry guardianRegistry,
            GuardianStudentRegistry guardianStudentRegistry,
            FingerprintService fingerprintService,
            DatabaseService databaseService,
            AuditLogService auditLogService,
            AdminSecurityService adminSecurity)
        {
            InitializeComponent();

            // Store all dependencies (required, just like EnrollmentWindow)
            _studentRegistry = studentRegistry;
            _guardianRegistry = guardianRegistry;
            _guardianStudentRegistry = guardianStudentRegistry;
            _fingerprintService = fingerprintService;
            _dbService = databaseService;
            _auditLogService = auditLogService;
            _adminSecurity = adminSecurity;

            this.Closing += StudentManagementWindow_Closing;

            LoadStudents();
        }

        // Parameterless constructor that creates minimal functionality
        // This keeps existing code working
        public StudentManagementWindow()
        {
            InitializeComponent();

            // Create only what's essential for basic viewing
            _dbService = new DatabaseService();

            // Other services will be null - features requiring them won't work
            // They are already nullable, so no need to assign null explicitly
            // Just leave them as their default null value
            this.Closing += StudentManagementWindow_Closing;

            LoadStudents();
        }

        private async void StudentManagementWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_isActuallyClosing)
                return;

            if (Application.Current.MainWindow == this)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to exit the Student Management system?\n\n" +
                    "Note: Fingerprint database will be refreshed before closing.",
                    "Confirm Exit",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            e.Cancel = true;

            try
            {
                StatusText.Text = "Refreshing fingerprint database...";
                await RefreshRegistriesAndDeviceDB();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to refresh: {ex.Message}",
                    "Refresh Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            _isActuallyClosing = true;
            Close();
        }
        private void LoadStudents()
        {
            _students.Clear();

            try
            {
                using var conn = _dbService.OpenConnection();
                using var cmd = conn.CreateCommand();

                // Query to get students with their guardians
                cmd.CommandText = @"
                    SELECT 
                        s.Id,
                        s.FullName,
                        s.ClassName,
                        s.FingerprintId,
                        GROUP_CONCAT(g.FullName, ', ') as GuardianNames
                    FROM Students s
                    LEFT JOIN GuardianStudents gs ON s.Id = gs.StudentId
                    LEFT JOIN Guardians g ON gs.GuardianId = g.Id
                    GROUP BY s.Id, s.FullName, s.ClassName, s.FingerprintId
                    ORDER BY s.ClassName, s.FullName;
                ";

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var student = new StudentRowViewModel
                    {
                        StudentId = reader.GetInt32(0),
                        StudentName = reader.GetString(1),
                        ClassName = reader.GetString(2),
                        FingerprintId = reader.GetInt32(3),
                        Guardians = reader.IsDBNull(4) ? "No guardians assigned" : reader.GetString(4)
                    };

                    _students.Add(student);
                }

                _filtered = new ObservableCollection<StudentRowViewModel>(_students);
                StudentsGrid.ItemsSource = _filtered;
                StatusText.Text = $"✅ Ready - {_filtered.Count} students loaded";
            }
            catch (SqliteException ex)
            {
                StatusText.Text = $"❌ Database error: {ex.Message}";
                MessageBox.Show($"Failed to load students from database: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Fallback to empty collection
                _filtered = new ObservableCollection<StudentRowViewModel>();
                StudentsGrid.ItemsSource = _filtered;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"❌ Error: {ex.Message}";
                MessageBox.Show($"Error loading students: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                _filtered = new ObservableCollection<StudentRowViewModel>();
                StudentsGrid.ItemsSource = _filtered;
            }

            // Update student count in status bar
            if (StudentCountText != null)
            {
                StudentCountText.Text = _filtered.Count.ToString();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchBox.Text.ToLower();
            _filtered.Clear();

            foreach (var s in _students.Where(x =>
                         x.StudentName.ToLower().Contains(query) ||
                         x.ClassName.ToLower().Contains(query) ||
                         (x.Guardians != null && x.Guardians.ToLower().Contains(query))))
            {
                _filtered.Add(s);
            }

            StatusText.Text = $"{_filtered.Count} matching students";
        }

        private StudentRowViewModel? GetSelected(object sender)
        {
            return ((FrameworkElement)sender).DataContext as StudentRowViewModel;
        }

        // ===== ACTION BUTTON HANDLERS =====
        private void View_Click(object sender, RoutedEventArgs e)
        {
            var student = GetSelected(sender);
            if (student == null) return;

            MessageBox.Show($"Viewing student: {student.StudentName}\n" +
                           $"Class: {student.ClassName}\n" +
                           $"Fingerprint ID: {student.FingerprintId}\n" +
                           $"Guardians: {student.Guardians}",
                           "Student Details",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var student = GetSelected(sender);
            if (student == null) return;

            // Check if we have all necessary services for edit mode
            if (_fingerprintService == null || _adminSecurity == null || _studentRegistry == null)
            {
                MessageBox.Show("Edit feature requires all services to be initialized. Please use the main enrollment window instead.",
                               "Feature Not Available",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);
                return;
            }

            OpenEnrollmentWindowForEdit(student.StudentId);
        }

        private void AddStudent_Click(object sender, RoutedEventArgs e)
        {
            // Check if we have all necessary services for add mode
            if (_fingerprintService == null || _adminSecurity == null || _studentRegistry == null)
            {
                MessageBox.Show("Add student feature requires all services to be initialized. Please use the main enrollment window instead.",
                               "Feature Not Available",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);
                return;
            }

            OpenEnrollmentWindowForAdd();
        }

        private void UpdateEnrollmentLockUI()
        {
            // This method would update any UI elements related to enrollment lock status
            // You might not need this in StudentManagementWindow, but keeping it for consistency
        }

        private void OpenEnrollmentWindowForEdit(int studentId)
        {
            if (_fingerprintService == null)
            {
                MessageBox.Show(
                    "Fingerprint device not ready yet.",
                    "Please wait",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            // FIRST RUN: create PIN
            if (!_adminSecurity!.HasPin())
            {
                MessageBox.Show(
                    "Admin PIN not set. Please create one.",
                    "Setup required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                var createPin = new CreatePinDialog { Owner = this };
                if (createPin.ShowDialog() != true)
                    return;

                _adminSecurity.CreatePin(createPin.Pin);
            }

            // SESSION CHECK
            if (!_adminSecurity.IsAdminSessionActive())
            {
                var pinDialog = new PinDialog { Owner = this };
                if (pinDialog.ShowDialog() != true)
                    return;

                if (!_adminSecurity.VerifyPin(pinDialog.EnteredPin, out var error))
                {
                    MessageBox.Show(
                        error,
                        "Access denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }
            }

            // OPEN ENROLLMENT IN EDIT MODE
            var enrollmentWindow = new EnrollmentWindow(
                _studentRegistry!,
                _guardianRegistry!,
                _guardianStudentRegistry!,
                _fingerprintService,
                _dbService,
                _auditLogService!,
                studentId // Pass student ID for edit mode
            )
            {
                Owner = this
            };

            // REFRESH DATA WHEN ENROLLMENT WINDOW CLOSES
            enrollmentWindow.Closed += async (s, args) =>
            {
                _adminSecurity.ClearSession();
                UpdateEnrollmentLockUI();

                // IMPORTANT: Refresh registries and device database
                await RefreshRegistriesAndDeviceDB();
            };

            enrollmentWindow.Show();
        }

        private void OpenEnrollmentWindowForAdd()
        {
            if (_fingerprintService == null)
            {
                MessageBox.Show(
                    "Fingerprint device not ready yet.",
                    "Please wait",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            // FIRST RUN: create PIN
            if (!_adminSecurity!.HasPin())
            {
                MessageBox.Show(
                    "Admin PIN not set. Please create one.",
                    "Setup required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                var createPin = new CreatePinDialog { Owner = this };
                if (createPin.ShowDialog() != true)
                    return;

                _adminSecurity.CreatePin(createPin.Pin);
            }

            // SESSION CHECK
            if (!_adminSecurity.IsAdminSessionActive())
            {
                var pinDialog = new PinDialog { Owner = this };
                if (pinDialog.ShowDialog() != true)
                    return;

                if (!_adminSecurity.VerifyPin(pinDialog.EnteredPin, out var error))
                {
                    MessageBox.Show(
                        error,
                        "Access denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }
            }

            // OPEN ENROLLMENT IN ADD MODE
            var enrollmentWindow = new EnrollmentWindow(
                _studentRegistry!,
                _guardianRegistry!,
                _guardianStudentRegistry!,
                _fingerprintService,
                _dbService,
                _auditLogService!,
                null // No student ID means add mode
            )
            {
                Owner = this
            };

            // REFRESH DATA WHEN ENROLLMENT WINDOW CLOSES
            enrollmentWindow.Closed += async (s, args) =>
            {
                _adminSecurity.ClearSession();
                UpdateEnrollmentLockUI();

                // IMPORTANT: Refresh registries and device database
                await RefreshRegistriesAndDeviceDB();
            };

            enrollmentWindow.Show();
        }

        private async Task RefreshRegistriesAndDeviceDB()
        {
            try
            {
                // Run registry refreshes in parallel if possible
                var tasks = new List<Task>();

                if (_studentRegistry != null)
                    tasks.Add(Task.Run(() => _studentRegistry.Refresh()));

                if (_guardianRegistry != null)
                    tasks.Add(Task.Run(() => _guardianRegistry.Refresh()));

                if (_guardianStudentRegistry != null)
                    tasks.Add(Task.Run(() => _guardianStudentRegistry.Refresh()));

                // Wait for all registry refreshes
                await Task.WhenAll(tasks);

                // Reload the students list in this window (on UI thread)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadStudents();
                });

                // Device database refresh (run in background)
                if (_fingerprintService != null)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            _fingerprintService.ClearDeviceDatabase();

                            if (_studentRegistry != null)
                            {
                                foreach (var student in _studentRegistry.All)
                                {
                                    _fingerprintService.UploadTemplate(student.FingerprintId, student.FingerprintTemplate);
                                }
                            }

                            if (_guardianRegistry != null)
                            {
                                foreach (var guardian in _guardianRegistry.All)
                                {
                                    _fingerprintService.UploadTemplate(guardian.FingerprintId, guardian.FingerprintTemplate);
                                }
                            }
                        }
                        catch (Exception deviceEx)
                        {
                            _auditLogService?.Log(
                                AuditLogService.EventTypes.DeviceCommunicationError,
                                "Failed to refresh fingerprint device database",
                                details: deviceEx.Message,
                                success: false,
                                errorMessage: deviceEx.Message
                            );
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to refresh data: {ex.Message}",
                               "Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var student = GetSelected(sender);
            if (student == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete student:\n\n" +
                $"Name: {student.StudentName}\n" +
                $"Class: {student.ClassName}\n" +
                $"Fingerprint ID: {student.FingerprintId}\n\n" +
                "This action cannot be undone!",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Delete from database
                    using var conn = _dbService.OpenConnection();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM Students WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", student.StudentId);
                    cmd.ExecuteNonQuery();

                    // Remove from collections
                    _students.Remove(student);
                    _filtered.Remove(student);

                    // Update UI
                    StudentsGrid.Items.Refresh();
                    StatusText.Text = $"Deleted {student.StudentName} - {_filtered.Count} students remaining";

                    MessageBox.Show($"Student '{student.StudentName}' has been deleted.",
                                   "Success",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete student: {ex.Message}",
                                   "Error",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                }
            }
        }

        // ===== TOOLBAR BUTTON HANDLERS =====
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadStudents();
            StatusText.Text = $"✅ Refreshed - {StudentsGrid.Items.Count} students loaded";

            if (StudentCountText != null)
            {
                StudentCountText.Text = StudentsGrid.Items.Count.ToString();
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export feature coming soon!",
                           "Info",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);
        }
    }
}