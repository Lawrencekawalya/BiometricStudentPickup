using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;

using BiometricStudentPickup.Services;
using BiometricStudentPickup.Models;

namespace BiometricStudentPickup
{
    public partial class EnrollmentWindow : Window
    {
        private readonly StudentRegistry _studentRegistry;
        private readonly GuardianRegistry _guardianRegistry;
        private readonly GuardianStudentRegistry _guardianStudentRegistry;
        private readonly FingerprintService _fingerprintService;
        private readonly DatabaseService _database;
        private readonly object _fingerprintIdLock = new object();
        private ICollectionView _studentsView;
        private ObservableCollection<Student> _studentsCollection;
        private readonly AuditLogService _auditLogService;
        private readonly int? _studentIdForEdit;

        public EnrollmentWindow(
            StudentRegistry studentRegistry,
            GuardianRegistry guardianRegistry,
            GuardianStudentRegistry guardianStudentRegistry,
            FingerprintService fingerprintService,
            DatabaseService database,
            AuditLogService auditLogService,
            int? studentIdForEdit = null)
        {
            InitializeComponent();

            _studentRegistry = studentRegistry;
            _guardianRegistry = guardianRegistry;
            _guardianStudentRegistry = guardianStudentRegistry;
            _fingerprintService = fingerprintService;
            _database = database;
            _auditLogService = auditLogService;
            _studentIdForEdit = studentIdForEdit;

            if (_studentIdForEdit.HasValue)
            {
                LoadStudentForEdit(_studentIdForEdit.Value);
            }

            // // TEMPORARY: Clear and reload device database
            // _fingerprintService.ClearDeviceDatabase();

            // // Reload all existing templates
            // foreach (var student in _studentRegistry.All)
            // {
            //     _fingerprintService.UploadTemplate(student.FingerprintId, student.FingerprintTemplate);
            // }
            // foreach (var guardian in _guardianRegistry.All)
            // {
            //     _fingerprintService.UploadTemplate(guardian.FingerprintId, guardian.FingerprintTemplate);
            // }

            // Initialize observable collection for better UI updates
            // _studentsCollection = new ObservableCollection<Student>(_studentRegistry.All);
            // StudentMultiSelect.ItemsSource = _studentsCollection;
            _studentsCollection = new ObservableCollection<Student>(_studentRegistry.All);

            _studentsView = CollectionViewSource.GetDefaultView(_studentsCollection);
            StudentMultiSelect.ItemsSource = _studentsView;

            // // Don't call UpdateButtonStates here - let them be called by TextChanged events
            // // The buttons should be enabled by default in XAML
            // Initialize button states based on current UI state
            // UpdateEnrollButtonState();
            // UpdateGuardianButtonState();
        }

        private void LoadStudentForEdit(int studentId)
        {
            try
            {
                using var conn = _database.OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT FullName, ClassName, FingerprintId 
            FROM Students 
            WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", studentId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    // Populate the form fields
                    StudentNameTextBox.Text = reader.GetString(0);
                    StudentClassTextBox.Text = reader.GetString(1);

                    var fingerprintId = reader.GetInt32(2);

                    // Set window title for edit mode
                    this.Title = "Edit Student Enrollment";

                    // Change button text and behavior
                    if (EnrollStudentButton != null)
                    {
                        EnrollStudentButton.Content = "Update Student";
                        // Optional: Change button color for edit mode
                        EnrollStudentButton.Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(59, 130, 246)); // Blue color
                    }

                    // Load guardians for this student
                    LoadGuardiansForStudent(studentId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load student data: {ex.Message}",
                               "Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }
        // private void LoadStudentForEdit(int studentId)
        // {
        //     try
        //     {
        //         using var conn = _database.OpenConnection(); // Changed from _databaseService to _database
        //         using var cmd = conn.CreateCommand();
        //         cmd.CommandText = @"
        //     SELECT FullName, ClassName, FingerprintId 
        //     FROM Students 
        //     WHERE Id = @id";
        //         cmd.Parameters.AddWithValue("@id", studentId);

        //         using var reader = cmd.ExecuteReader();
        //         if (reader.Read())
        //         {
        //             // Populate the form fields
        //             StudentNameTextBox.Text = reader.GetString(0);
        //             StudentClassTextBox.Text = reader.GetString(1); // Changed from ClassNameTextBox to StudentClassTextBox

        //             // You might want to store the fingerprint ID and template for reference
        //             // but you shouldn't display the fingerprint template
        //             var fingerprintId = reader.GetInt32(2);

        //             // Set window title for edit mode
        //             this.Title = "Edit Student Enrollment";

        //             // You could also change button text
        //             if (EnrollStudentButton != null)
        //                 EnrollStudentButton.Content = "Update Student";

        //             // Load guardians for this student
        //             LoadGuardiansForStudent(studentId);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         MessageBox.Show($"Failed to load student data: {ex.Message}",
        //                        "Error",
        //                        MessageBoxButton.OK,
        //                        MessageBoxImage.Error);
        //     }
        // }

        private void LoadGuardiansForStudent(int studentId)
        {
            try
            {
                using var conn = _database.OpenConnection(); // Changed from _databaseService to _database
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT g.Id, g.FullName, g.FingerprintId
            FROM Guardians g
            INNER JOIN GuardianStudents gs ON g.Id = gs.GuardianId
            WHERE gs.StudentId = @studentId";
                cmd.Parameters.AddWithValue("@studentId", studentId);

                // Load guardians into your UI (depends on how your EnrollmentWindow displays guardians)
                // This might involve populating a listbox or datagrid
                // Note: You'll need to implement this based on how your UI works
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load guardians: {ex.Message}",
                               "Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }



        // ============================
        // STUDENT ENROLLMENT
        // ============================
        // Update the EnrollStudent_Click method to handle edit mode
        private async void EnrollStudent_Click(object sender, RoutedEventArgs e)
        {
            // Disable button to prevent multiple clicks
            var button = sender as Button;
            if (button == null) return;

            button.IsEnabled = false;
            Cursor = Cursors.Wait;

            // Use nullable reference types to avoid warnings
            Student? student = null;
            int fingerprintId = 0;

            try
            {
                var name = StudentNameTextBox.Text.Trim();
                var cls = StudentClassTextBox.Text.Trim();

                // Enhanced input validation
                if (!ValidateInput(name, cls, out string validationMessage))
                {
                    // AUDIT LOG: Validation failed
                    _auditLogService.Log(AuditEventTypes.StudentEnrollmentFailed,
                        $"Student enrollment validation failed: {validationMessage}",
                        success: false);

                    MessageBox.Show(validationMessage, "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // CHECK IF WE'RE IN EDIT MODE
                if (_studentIdForEdit.HasValue)
                {
                    // EDIT MODE: Update existing student
                    await UpdateExistingStudent(_studentIdForEdit.Value, name, cls);
                    return;
                }

                // ADD MODE: Create new student (original logic)
                // 1️⃣ Capture fingerprint template
                byte[] template = await _fingerprintService.EnrollAsync();

                // 2️⃣ Check for duplicate fingerprint in device DB
                int? existingId = await _fingerprintService.VerifyAsync();
                if (existingId != null)
                {
                    // AUDIT LOG: Duplicate fingerprint detected
                    _auditLogService.Log(AuditEventTypes.FingerprintDuplicateDetected,
                        $"Duplicate fingerprint detected during student enrollment: {name}",
                        success: false);

                    MessageBox.Show(
                        "This fingerprint is already enrolled.",
                        "Duplicate Fingerprint",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // 3️⃣ Generate GLOBAL fingerprint ID with thread safety
                lock (_fingerprintIdLock)
                {
                    fingerprintId = _database.GetNextFingerprintId();
                }

                // 4️⃣ Persist in SQLite and memory
                student = _studentRegistry.Add(
                    name,
                    cls,
                    fingerprintId,
                    template
                );

                try
                {
                    // 5️⃣ Upload to device with timeout
                    await UploadTemplateWithTimeoutAsync(fingerprintId, template);
                }
                catch (Exception uploadEx)
                {
                    // Rollback using registry's Remove method
                    bool removed = false;
                    if (student != null)
                    {
                        removed = _studentRegistry.Remove(student.LocalId);

                        if (removed)
                        {
                            // Update UI
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var studentToRemove = _studentsCollection.FirstOrDefault(s => s.LocalId == student!.LocalId);
                                if (studentToRemove != null)
                                {
                                    _studentsCollection.Remove(studentToRemove);
                                }
                            });
                        }
                    }

                    // AUDIT LOG: Device upload failed
                    _auditLogService.LogStudentEnrollmentFailed(name, uploadEx.Message);

                    MessageBox.Show(
                        $"Failed to upload fingerprint to device:\n{uploadEx.Message}\n\n" +
                        (removed ? "Enrollment has been rolled back." : "Enrollment may not have been fully rolled back."),
                        "Device Communication Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return;
                }

                // 6️⃣ Update UI efficiently
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _studentsCollection.Add(student!);
                    StudentNameTextBox.Clear();
                    StudentClassTextBox.Clear();
                });

                // AUDIT LOG: Student enrollment success
                _auditLogService.LogStudentEnrolled(student!.LocalId, student!.FullName, student!.ClassName);

                MessageBox.Show($"Student enrolled successfully: {student!.FullName}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                // Fingerprint enrollment was cancelled by user
                string studentName = StudentNameTextBox.Text.Trim();
                // AUDIT LOG: Student enrollment cancelled
                _auditLogService.Log(AuditEventTypes.StudentEnrollmentCancelled,
                    $"Student enrollment cancelled: {studentName}",
                    success: false);

                MessageBox.Show("Fingerprint enrollment was cancelled.",
                    "Enrollment Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // AUDIT LOG: Student enrollment failed
                _auditLogService.Log(AuditEventTypes.StudentEnrollmentFailed,
                    $"Student enrollment failed: {ex.Message}",
                    success: false);

                // If we created a student but failed later, try to clean up (only in add mode)
                if (student != null && !_studentIdForEdit.HasValue)
                {
                    bool removed = _studentRegistry.Remove(student.LocalId);
                    if (removed)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var studentToRemove = _studentsCollection.FirstOrDefault(s => s.LocalId == student.LocalId);
                            if (studentToRemove != null)
                            {
                                _studentsCollection.Remove(studentToRemove);
                            }
                        });
                    }
                }

                MessageBox.Show(
                    $"Student enrollment failed:\n{ex.Message}",
                    "Enrollment Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                // SIMPLE RE-ENABLE - no complex logic
                Application.Current.Dispatcher.Invoke(() =>
                {
                    button.IsEnabled = true;
                    Cursor = Cursors.Arrow;

                    // Force button state update
                    CommandManager.InvalidateRequerySuggested();
                });
            }
        }

        // ADD THIS NEW METHOD for updating existing student
        // private async Task UpdateExistingStudent(int studentId, string newName, string newClassName)
        private async Task UpdateExistingStudent(int studentId, string newName, string newClassName)
        {
            try
            {
                // Get the existing student from database
                Student? existingStudent = null;
                using (var conn = _database.OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT FullName, ClassName, FingerprintId, FingerprintTemplate
                FROM Students 
                WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", studentId);

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        existingStudent = new Student
                        {
                            LocalId = studentId,
                            FullName = reader.GetString(0),
                            ClassName = reader.GetString(1),
                            FingerprintId = reader.GetInt32(2),
                            FingerprintTemplate = (byte[])reader["FingerprintTemplate"]
                        };
                    }
                }

                if (existingStudent == null)
                {
                    MessageBox.Show($"Student with ID {studentId} not found.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // This is now valid in an async method
                }

                // Update the student in database
                using (var conn = _database.OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                UPDATE Students 
                SET FullName = @name, 
                    ClassName = @class,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = @id";

                    cmd.Parameters.AddWithValue("@name", newName);
                    cmd.Parameters.AddWithValue("@class", newClassName);
                    cmd.Parameters.AddWithValue("@id", studentId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        MessageBox.Show("Failed to update student record.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return; // This is now valid in an async method
                    }
                }

                // Update in memory registry
                _studentRegistry.Refresh();

                // Update UI collection
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Instead of modifying existing student, find the updated one from registry
                    var updatedStudent = _studentRegistry.All.FirstOrDefault(s => s.LocalId == studentId);
                    if (updatedStudent != null)
                    {
                        // Find and replace the student in the collection
                        var oldStudent = _studentsCollection.FirstOrDefault(s => s.LocalId == studentId);
                        if (oldStudent != null)
                        {
                            int index = _studentsCollection.IndexOf(oldStudent);
                            if (index >= 0)
                            {
                                // Replace with the updated student from registry
                                _studentsCollection[index] = updatedStudent;
                            }
                        }

                        // Refresh the collection view
                        _studentsView.Refresh();
                    }

                    // Clear form and close window
                    StudentNameTextBox.Clear();
                    StudentClassTextBox.Clear();

                    // Close the window since edit is complete
                    this.Close();
                });

                // AUDIT LOG: Student updated
                _auditLogService.Log(
                    "STUDENT_UPDATED",
                    $"Student updated: {existingStudent.FullName} → {newName}",
                    studentId: studentId,
                    success: true,
                    details: $"Class: {existingStudent.ClassName} → {newClassName}"
                );

                MessageBox.Show($"Student updated successfully!\n\n" +
                               $"Name: {existingStudent.FullName} → {newName}\n" +
                               $"Class: {existingStudent.ClassName} → {newClassName}",
                    "Update Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _auditLogService.Log(
                    "STUDENT_UPDATE_FAILED",
                    $"Failed to update student: {ex.Message}",
                    studentId: studentId,
                    success: false,
                    errorMessage: ex.Message
                );

                MessageBox.Show($"Failed to update student: {ex.Message}",
                    "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
        // private Task UpdateExistingStudent(int studentId, string newName, string newClassName)
        // {
        //     try
        //     {
        //         // Get the existing student from database
        //         Student? existingStudent = null;
        //         using (var conn = _database.OpenConnection())
        //         using (var cmd = conn.CreateCommand())
        //         {
        //             cmd.CommandText = @"
        //         SELECT FullName, ClassName, FingerprintId, FingerprintTemplate
        //         FROM Students 
        //         WHERE Id = @id";
        //             cmd.Parameters.AddWithValue("@id", studentId);

        //             using var reader = cmd.ExecuteReader();
        //             if (reader.Read())
        //             {
        //                 existingStudent = new Student
        //                 {
        //                     LocalId = studentId,
        //                     FullName = reader.GetString(0),
        //                     ClassName = reader.GetString(1),
        //                     FingerprintId = reader.GetInt32(2),
        //                     FingerprintTemplate = (byte[])reader["FingerprintTemplate"]
        //                 };
        //             }
        //         }

        //         if (existingStudent == null)
        //         {
        //             MessageBox.Show($"Student with ID {studentId} not found.",
        //                 "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //             return;
        //         }

        //         // Update the student in database
        //         using (var conn = _database.OpenConnection())
        //         using (var cmd = conn.CreateCommand())
        //         {
        //             cmd.CommandText = @"
        //         UPDATE Students 
        //         SET FullName = @name, 
        //             ClassName = @class,
        //             UpdatedAt = CURRENT_TIMESTAMP
        //         WHERE Id = @id";

        //             cmd.Parameters.AddWithValue("@name", newName);
        //             cmd.Parameters.AddWithValue("@class", newClassName);
        //             cmd.Parameters.AddWithValue("@id", studentId);

        //             int rowsAffected = cmd.ExecuteNonQuery();

        //             if (rowsAffected == 0)
        //             {
        //                 MessageBox.Show("Failed to update student record.",
        //                     "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                 return;
        //             }
        //         }

        //         // Update in memory registry
        //         // var registryStudent = _studentRegistry.All.FirstOrDefault(s => s.LocalId == studentId);
        //         // if (registryStudent != null)
        //         // {
        //         //     registryStudent.FullName = newName;
        //         //     registryStudent.ClassName = newClassName;

        //         // Refresh the registry
        //         _studentRegistry.Refresh();
        //         // }

        //         // Update UI collection
        //         Application.Current.Dispatcher.Invoke(() =>
        //         {
        //             // Instead of modifying existing student, find the updated one from registry
        //             var updatedStudent = _studentRegistry.All.FirstOrDefault(s => s.LocalId == studentId);
        //             if (updatedStudent != null)
        //             {
        //                 // Find and replace the student in the collection
        //                 var oldStudent = _studentsCollection.FirstOrDefault(s => s.LocalId == studentId);
        //                 if (oldStudent != null)
        //                 {
        //                     int index = _studentsCollection.IndexOf(oldStudent);
        //                     if (index >= 0)
        //                     {
        //                         // Replace with the updated student from registry
        //                         _studentsCollection[index] = updatedStudent;
        //                     }
        //                 }

        //                 // Refresh the collection view
        //                 _studentsView.Refresh();
        //             }

        //             // Clear form and close window
        //             StudentNameTextBox.Clear();
        //             StudentClassTextBox.Clear();

        //             // Close the window since edit is complete
        //             this.Close();
        //         });

        //         // AUDIT LOG: Student updated
        //         _auditLogService.Log(
        //             "STUDENT_UPDATED",
        //             $"Student updated: {existingStudent.FullName} → {newName}",
        //             studentId: studentId,
        //             success: true,
        //             details: $"Class: {existingStudent.ClassName} → {newClassName}"
        //         );

        //         MessageBox.Show($"Student updated successfully!\n\n" +
        //                        $"Name: {existingStudent.FullName} → {newName}\n" +
        //                        $"Class: {existingStudent.ClassName} → {newClassName}",
        //             "Update Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        //         return Task.CompletedTask;
        //     }
        //     catch (Exception ex)
        //     {
        //         _auditLogService.Log(
        //             "STUDENT_UPDATE_FAILED",
        //             $"Failed to update student: {ex.Message}",
        //             studentId: studentId,
        //             success: false,
        //             errorMessage: ex.Message
        //         );

        //         MessageBox.Show($"Failed to update student: {ex.Message}",
        //             "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //         throw;
        //     }
        // }
        // private async void EnrollStudent_Click(object sender, RoutedEventArgs e)
        // {
        //     // Disable button to prevent multiple clicks
        //     var button = sender as Button;
        //     if (button == null) return;

        //     button.IsEnabled = false;
        //     Cursor = Cursors.Wait; // Optional: Show wait cursor

        //     // Use nullable reference types to avoid warnings
        //     Student? student = null;
        //     int fingerprintId = 0;

        //     try
        //     {
        //         var name = StudentNameTextBox.Text.Trim();
        //         var cls = StudentClassTextBox.Text.Trim();

        //         // Enhanced input validation
        //         if (!ValidateInput(name, cls, out string validationMessage))
        //         {
        //             // AUDIT LOG: Validation failed
        //             _auditLogService.Log(AuditEventTypes.StudentEnrollmentFailed,
        //                 $"Student enrollment validation failed: {validationMessage}",
        //                 success: false);

        //             MessageBox.Show(validationMessage, "Validation Error",
        //                 MessageBoxButton.OK, MessageBoxImage.Warning);
        //             return;
        //         }

        //         // 1️⃣ Capture fingerprint template
        //         byte[] template = await _fingerprintService.EnrollAsync();

        //         // 2️⃣ Check for duplicate fingerprint in device DB
        //         int? existingId = await _fingerprintService.VerifyAsync();
        //         if (existingId != null)
        //         {
        //             // AUDIT LOG: Duplicate fingerprint detected
        //             _auditLogService.Log(AuditEventTypes.FingerprintDuplicateDetected,
        //                 $"Duplicate fingerprint detected during student enrollment: {name}",
        //                 success: false);

        //             MessageBox.Show(
        //                 "This fingerprint is already enrolled.",
        //                 "Duplicate Fingerprint",
        //                 MessageBoxButton.OK,
        //                 MessageBoxImage.Warning
        //             );
        //             return;
        //         }

        //         // 3️⃣ Generate GLOBAL fingerprint ID with thread safety
        //         lock (_fingerprintIdLock)
        //         {
        //             fingerprintId = _database.GetNextFingerprintId();
        //         }

        //         // 4️⃣ Persist in SQLite and memory
        //         student = _studentRegistry.Add(
        //             name,
        //             cls,
        //             fingerprintId,
        //             template
        //         );

        //         try
        //         {
        //             // 5️⃣ Upload to device with timeout
        //             await UploadTemplateWithTimeoutAsync(fingerprintId, template);
        //         }
        //         catch (Exception uploadEx)
        //         {
        //             // Rollback using registry's Remove method
        //             bool removed = false;
        //             if (student != null)
        //             {
        //                 removed = _studentRegistry.Remove(student.LocalId);

        //                 if (removed)
        //                 {
        //                     // Update UI
        //                     Application.Current.Dispatcher.Invoke(() =>
        //                     {
        //                         var studentToRemove = _studentsCollection.FirstOrDefault(s => s.LocalId == student!.LocalId);
        //                         if (studentToRemove != null)
        //                         {
        //                             _studentsCollection.Remove(studentToRemove);
        //                         }
        //                     });
        //                 }
        //             }

        //             // AUDIT LOG: Device upload failed
        //             _auditLogService.LogStudentEnrollmentFailed(name, uploadEx.Message);

        //             MessageBox.Show(
        //                 $"Failed to upload fingerprint to device:\n{uploadEx.Message}\n\n" +
        //                 (removed ? "Enrollment has been rolled back." : "Enrollment may not have been fully rolled back."),
        //                 "Device Communication Error",
        //                 MessageBoxButton.OK,
        //                 MessageBoxImage.Error
        //             );
        //             return;
        //         }

        //         // 6️⃣ Update UI efficiently
        //         Application.Current.Dispatcher.Invoke(() =>
        //         {
        //             _studentsCollection.Add(student!);
        //             StudentNameTextBox.Clear();
        //             StudentClassTextBox.Clear();
        //             // DON'T call UpdateEnrollButtonState() here - let text changed events handle it
        //         });

        //         // AUDIT LOG: Student enrollment success
        //         _auditLogService.LogStudentEnrolled(student!.LocalId, student!.FullName, student!.ClassName);

        //         MessageBox.Show($"Student enrolled successfully: {student!.FullName}",
        //             "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        //     }
        //     catch (OperationCanceledException)
        //     {
        //         // Fingerprint enrollment was cancelled by user
        //         string studentName = StudentNameTextBox.Text.Trim();
        //         // AUDIT LOG: Student enrollment cancelled
        //         _auditLogService.Log(AuditEventTypes.StudentEnrollmentCancelled,
        //             $"Student enrollment cancelled: {studentName}",
        //             success: false);
        //         // Fingerprint enrollment was cancelled by user
        //         MessageBox.Show("Fingerprint enrollment was cancelled.",
        //             "Enrollment Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
        //     }
        //     catch (Exception ex)
        //     {
        //         // AUDIT LOG: Student enrollment failed
        //         _auditLogService.Log(AuditEventTypes.StudentEnrollmentFailed,
        //             $"Student enrollment failed: {ex.Message}",
        //             success: false);
        //         // If we created a student but failed later, try to clean up
        //         if (student != null)
        //         {
        //             bool removed = _studentRegistry.Remove(student.LocalId);
        //             if (removed)
        //             {
        //                 Application.Current.Dispatcher.Invoke(() =>
        //                 {
        //                     var studentToRemove = _studentsCollection.FirstOrDefault(s => s.LocalId == student.LocalId);
        //                     if (studentToRemove != null)
        //                     {
        //                         _studentsCollection.Remove(studentToRemove);
        //                     }
        //                 });
        //             }
        //         }

        //         MessageBox.Show(
        //             $"Student enrollment failed:\n{ex.Message}",
        //             "Enrollment Error",
        //             MessageBoxButton.OK,
        //             MessageBoxImage.Error
        //         );
        //     }
        //     finally
        //     {
        //         // SIMPLE RE-ENABLE - no complex logic
        //         Application.Current.Dispatcher.Invoke(() =>
        //         {
        //             button.IsEnabled = true;
        //             Cursor = Cursors.Arrow; // Restore cursor

        //             // Force button state update
        //             CommandManager.InvalidateRequerySuggested();
        //         });
        //     }
        // }

        // ============================
        // GUARDIAN ENROLLMENT
        // ============================
        private async void EnrollGuardian_Click(object sender, RoutedEventArgs e)
        {
            // Disable button to prevent multiple clicks
            var button = sender as Button;
            if (button == null) return;

            button.IsEnabled = false;
            Cursor = Cursors.Wait; // Optional: Show wait cursor

            // Use nullable reference types to avoid warnings
            Guardian? guardian = null;
            int fingerprintId = 0;
            var selectedStudentIds = System.Array.Empty<int>();

            try
            {
                var name = GuardianNameTextBox.Text.Trim();

                selectedStudentIds = StudentMultiSelect.SelectedItems
                    .Cast<Student>()
                    .Select(s => s.LocalId)
                    .ToArray();

                // Enhanced input validation
                if (!ValidateInput(name, "N/A", out string validationMessage, isGuardian: true))
                {
                    // AUDIT LOG: Guardian validation failed
                    _auditLogService.Log(AuditEventTypes.GuardianEnrollmentFailed,
                        $"Guardian enrollment validation failed: {validationMessage}",
                        success: false);

                    MessageBox.Show(validationMessage, "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (selectedStudentIds.Length == 0)
                {
                    // AUDIT LOG: No students selected for guardian
                    _auditLogService.Log(AuditEventTypes.GuardianEnrollmentFailed,
                        "Guardian enrollment failed: No students selected",
                        success: false);

                    MessageBox.Show("Select at least one student",
                        "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 1️⃣ Capture fingerprint template
                byte[] template = await _fingerprintService.EnrollAsync();

                // 2️⃣ Check for duplicate fingerprint (SAME AS STUDENT CHECK)
                int? existingId = await _fingerprintService.VerifyAsync();
                if (existingId != null)
                {
                    // AUDIT LOG: Duplicate fingerprint for guardian
                    _auditLogService.Log(AuditEventTypes.FingerprintDuplicateDetected,
                        $"Duplicate fingerprint detected during guardian enrollment: {name}",
                        success: false);

                    MessageBox.Show(
                        "This fingerprint is already enrolled.",
                        "Duplicate Fingerprint",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // 3️⃣ Generate GLOBAL fingerprint ID with thread safety
                lock (_fingerprintIdLock)
                {
                    fingerprintId = _database.GetNextFingerprintId();
                }

                // 4️⃣ Persist guardian
                guardian = _guardianRegistry.Add(
                    name,
                    fingerprintId,
                    template
                );

                try
                {
                    // 5️⃣ Upload to device with timeout
                    await UploadTemplateWithTimeoutAsync(fingerprintId, template);
                }
                catch (Exception uploadEx)
                {
                    // Rollback using registry's Remove method
                    bool removed = false;
                    if (guardian != null)
                    {
                        removed = _guardianRegistry.Remove(guardian.LocalId);
                    }

                    MessageBox.Show(
                        $"Failed to upload fingerprint to device:\n{uploadEx.Message}\n\n" +
                        (removed ? "Enrollment has been rolled back." : "Enrollment may not have been fully rolled back."),
                        "Device Communication Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return;
                }

                // 6️⃣ Link guardian to students
                if (guardian != null)
                {
                    _guardianStudentRegistry.Link(guardian.LocalId, selectedStudentIds);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    GuardianNameTextBox.Clear();
                    StudentMultiSelect.UnselectAll();
                    // DON'T call UpdateGuardianButtonState() here - let events handle it
                });

                // AUDIT LOG: Guardian enrollment success
                _auditLogService.LogGuardianEnrolled(guardian!.LocalId, guardian!.FullName);

                MessageBox.Show($"Guardian enrolled successfully: {guardian!.FullName}\n" +
                               $"Linked to {selectedStudentIds.Length} student(s)",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                // Fingerprint enrollment was cancelled by user
                string guardianName = GuardianNameTextBox.Text.Trim();
                // AUDIT LOG: Guardian enrollment cancelled
                _auditLogService.Log(AuditEventTypes.GuardianEnrollmentCancelled,
                    $"Guardian enrollment cancelled: {guardianName}",
                    success: false);
                // Fingerprint enrollment was cancelled by user
                MessageBox.Show("Fingerprint enrollment was cancelled.",
                    "Enrollment Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clean up if guardian was created
                if (guardian != null)
                {
                    _guardianRegistry.Remove(guardian.LocalId);
                }
            }
            catch (Exception ex)
            {
                // AUDIT LOG: Guardian enrollment failed
                _auditLogService.Log(AuditEventTypes.GuardianEnrollmentFailed,
                    $"Guardian enrollment failed: {ex.Message}",
                    success: false);
                // If we created a guardian but failed later, try to clean up
                if (guardian != null)
                {
                    _guardianRegistry.Remove(guardian.LocalId);
                }

                MessageBox.Show(
                    $"Guardian enrollment failed:\n{ex.Message}",
                    "Enrollment Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                // SIMPLE RE-ENABLE - no complex logic
                Application.Current.Dispatcher.Invoke(() =>
                {
                    button.IsEnabled = true;
                    Cursor = Cursors.Arrow; // Restore cursor

                    // Force button state update
                    CommandManager.InvalidateRequerySuggested();
                });
            }
        }

        // ============================
        // HELPER METHODS
        // ============================
        private void StudentSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = StudentSearchBox.Text?.Trim().ToLower();

            _studentsView.Filter = item =>
            {
                if (string.IsNullOrEmpty(searchText))
                    return true;

                var student = item as Student;
                return student != null &&
                    student.FullName.ToLower().Contains(searchText);
            };
        }

        /// <summary>
        /// Validates user input with proper sanitization
        /// </summary>
        private bool ValidateInput(string name, string className, out string message, bool isGuardian = false)
        {
            // Check for empty/null
            if (string.IsNullOrWhiteSpace(name))
            {
                message = isGuardian ? "Enter guardian name" : "Enter student name";
                return false;
            }

            if (!isGuardian && string.IsNullOrWhiteSpace(className))
            {
                message = "Enter student class";
                return false;
            }

            // Validate name length and format
            if (name.Length > 100)
            {
                message = "Name cannot exceed 100 characters";
                return false;
            }

            if (!isGuardian && className.Length > 50)
            {
                message = "Class name cannot exceed 50 characters";
                return false;
            }

            // Allow only letters, spaces, hyphens, and apostrophes
            if (!Regex.IsMatch(name, @"^[a-zA-Z\s\-'.]+$"))
            {
                message = "Name can only contain letters, spaces, hyphens, and apostrophes";
                return false;
            }

            // For students, validate class name
            if (!isGuardian && !Regex.IsMatch(className, @"^[a-zA-Z0-9\s\-]+$"))
            {
                message = "Class name can only contain letters, numbers, spaces, and hyphens";
                return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// Safely uploads fingerprint template with timeout handling
        /// </summary>
        private async Task UploadTemplateWithTimeoutAsync(int fingerprintId, byte[] template)
        {
            var uploadTask = Task.Run(() => _fingerprintService.UploadTemplate(fingerprintId, template));
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));

            var completedTask = await Task.WhenAny(uploadTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Fingerprint upload timed out after 30 seconds");
            }

            // Propagate any exceptions from the upload task
            await uploadTask;
        }

        /// <summary>
        /// Clean up resources when window closes
        /// </summary>
        // protected override void OnClosed(EventArgs e)
        // {
        //     // Dispose of any unmanaged resources in services if needed
        //     if (_fingerprintService is IDisposable disposable)
        //     {
        //         disposable.Dispose();
        //     }

        //     base.OnClosed(e);
        // }

        // ============================
        // UI EVENT HANDLERS
        // ============================

        private void StudentNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateEnrollButtonState();
        }

        private void StudentClassTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateEnrollButtonState();
        }

        private void GuardianNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateGuardianButtonState();
        }

        private void StudentMultiSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGuardianButtonState();
        }

        /// <summary>
        /// Updates the enabled state of the student enrollment button
        /// </summary>
        private void UpdateEnrollButtonState()
        {
            var name = StudentNameTextBox.Text.Trim();
            var cls = StudentClassTextBox.Text.Trim();

            // Enable button only if both fields have text
            bool shouldEnable = !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(cls);

            // Update button state if it's different
            if (EnrollStudentButton.IsEnabled != shouldEnable)
            {
                EnrollStudentButton.IsEnabled = shouldEnable;
            }
        }

        /// <summary>
        /// Updates the enabled state of the guardian enrollment button
        /// </summary>
        private void UpdateGuardianButtonState()
        {
            var name = GuardianNameTextBox.Text.Trim();
            var hasSelectedStudents = StudentMultiSelect.SelectedItems.Count > 0;

            // Enable button only if name has text AND at least one student is selected
            bool shouldEnable = !string.IsNullOrWhiteSpace(name) && hasSelectedStudents;

            // Update button state if it's different
            if (EnrollGuardianButton.IsEnabled != shouldEnable)
            {
                EnrollGuardianButton.IsEnabled = shouldEnable;
            }
        }
    }
}