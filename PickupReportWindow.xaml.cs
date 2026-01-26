// // using System;
// // using System.Collections.Generic;
// // using System.Diagnostics;
// // using System.IO;
// // using System.Linq;
// // using System.Windows;
// // using System.Windows.Controls;
// // using BiometricStudentPickup.Services;
// // using BiometricStudentPickup.Models;

// // namespace BiometricStudentPickup.Views
// // {
// //     public partial class PickupReportWindow : Window
// //     {
// //         private readonly PickupLogService _pickupLogService;
// //         private readonly StudentRegistry _studentRegistry;
// //         private readonly GuardianRegistry _guardianRegistry;
// //         private List<PickupLog> _allLogs = new List<PickupLog>();

// //         public PickupReportWindow(
// //             PickupLogService pickupLogService,
// //             StudentRegistry studentRegistry,
// //             GuardianRegistry guardianRegistry)
// //         {
// //             Log("=== CONSTRUCTOR START ===");

// //             try
// //             {
// //                 Log("Validating parameters...");

// //                 if (pickupLogService == null)
// //                     throw new ArgumentNullException(nameof(pickupLogService));
// //                 if (studentRegistry == null)
// //                     throw new ArgumentNullException(nameof(studentRegistry));
// //                 if (guardianRegistry == null)
// //                     throw new ArgumentNullException(nameof(guardianRegistry));

// //                 _pickupLogService = pickupLogService;
// //                 _studentRegistry = studentRegistry;
// //                 _guardianRegistry = guardianRegistry;

// //                 Log("Parameters validated successfully");

// //                 InitializeComponent();
// //                 Log("InitializeComponent completed");

// //                 // Load data when window is ready
// //                 this.Loaded += PickupReportWindow_Loaded;

// //                 Log("=== CONSTRUCTOR END ===");
// //             }
// //             catch (Exception ex)
// //             {
// //                 Log($"ERROR in constructor: {ex.Message}\n{ex.StackTrace}");
// //                 MessageBox.Show($"Error creating window: {ex.Message}", "Error");
// //                 this.Close();
// //             }
// //         }

// //         private void PickupReportWindow_Loaded(object sender, RoutedEventArgs e)
// //         {
// //             Log("=== WINDOW_LOADED START ===");

// //             try
// //             {
// //                 // First, test the database connection
// //                 TestDatabaseConnection();

// //                 // Set default date range (last 7 days)
// //                 DateFromPicker.SelectedDate = DateTime.Now.AddDays(-7);
// //                 DateToPicker.SelectedDate = DateTime.Now;

// //                 // Load all logs from service
// //                 LoadAllLogs();

// //                 // Populate student filter
// //                 PopulateStudentFilter();

// //                 // Apply initial filters to show data
// //                 ApplyFilters();

// //                 Log("=== WINDOW_LOADED END ===");
// //             }
// //             catch (Exception ex)
// //             {
// //                 Log($"ERROR in Window_Loaded: {ex.Message}\n{ex.StackTrace}");
// //                 MessageBox.Show($"Error loading data: {ex.Message}", "Error");
// //             }
// //         }

// //         // private void TestDatabaseConnection()
// //         // {
// //         //     try
// //         //     {
// //         //         Log("Testing database connection...");

// //         //         // Use the test method in PickupLogService
// //         //         _pickupLogService.TestServiceConnection();

// //         //         // Also test direct query
// //         //         using var conn = _pickupLogService.GetType()
// //         //             .GetField("_databaseService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
// //         //             .GetValue(_pickupLogService) as DatabaseService;

// //         //         if (conn != null)
// //         //         {
// //         //             using var dbConn = conn.OpenConnection();
// //         //             using var cmd = dbConn.CreateCommand();
// //         //             cmd.CommandText = "SELECT COUNT(*) FROM PickupLogs";
// //         //             var count = Convert.ToInt32(cmd.ExecuteScalar());
// //         //             Log($"Direct query: PickupLogs table has {count} records");

// //         //             if (count > 0)
// //         //             {
// //         //                 cmd.CommandText = "SELECT * FROM PickupLogs ORDER BY RequestedAt DESC LIMIT 3";
// //         //                 using var reader = cmd.ExecuteReader();
// //         //                 while (reader.Read())
// //         //                 {
// //         //                     Log($"Log entry: Id={reader["Id"]}, StudentId={reader["StudentId"]}, Status={reader["Status"]}");
// //         //                 }
// //         //             }
// //         //         }
// //         //     }
// //         //     catch (Exception ex)
// //         //     {
// //         //         Log($"Database test error: {ex.Message}");
// //         //     }
// //         // }
// //         private void TestDatabaseConnection()
// //         {
// //             try
// //             {
// //                 Log("Testing database connection...");

// //                 // Use the test method that already exists in PickupLogService
// //                 _pickupLogService.TestServiceConnection();

// //                 // Simple direct test using the service
// //                 var logs = _pickupLogService.GetPickupLogs(DateTime.Now.AddDays(-7), DateTime.Now);
// //                 Log($"Service returned {logs?.Count ?? 0} logs");

// //                 if (logs != null && logs.Any())
// //                 {
// //                     for (int i = 0; i < Math.Min(3, logs.Count); i++)
// //                     {
// //                         var log = logs[i];
// //                         Log($"Sample log {i}: Id={log.Id}, StudentId={log.StudentId}, Status={log.Status}, RequestedAt={log.RequestedAt}");
// //                     }
// //                 }
// //                 else
// //                 {
// //                     Log("WARNING: No logs found in database!");
// //                 }
// //             }
// //             catch (Exception ex)
// //             {
// //                 Log($"Database test error: {ex.Message}");
// //             }
// //         }

// //         private void LoadAllLogs()
// //         {
// //             try
// //             {
// //                 Log("Loading all pickup logs from service...");

// //                 // Get logs from service (last 30 days by default)
// //                 _allLogs = _pickupLogService.GetPickupLogs(
// //                     DateTime.Now.AddDays(-30), 
// //                     DateTime.Now
// //                 );

// //                 if (_allLogs == null)
// //                 {
// //                     Log("Service returned null");
// //                     _allLogs = new List<PickupLog>();
// //                 }
// //                 else
// //                 {
// //                     Log($"Loaded {_allLogs.Count} pickup logs from database");

// //                     // Show first few logs for debugging
// //                     if (_allLogs.Any())
// //                     {
// //                         for (int i = 0; i < Math.Min(3, _allLogs.Count); i++)
// //                         {
// //                             var log = _allLogs[i];
// //                             Log($"Log {i}: Id={log.Id}, StudentId={log.StudentId}, Status={log.Status}, RequestedAt={log.RequestedAt}");
// //                         }
// //                     }
// //                     else
// //                     {
// //                         Log("WARNING: Database returned empty pickup logs!");
// //                         MessageBox.Show("No pickup logs found in the database. The report will be empty.", 
// //                             "Info", MessageBoxButton.OK, MessageBoxImage.Information);
// //                     }
// //                 }
// //             }
// //             catch (Exception ex)
// //             {
// //                 Log($"Error in LoadAllLogs: {ex.Message}\n{ex.StackTrace}");
// //                 MessageBox.Show($"Failed to load pickup logs: {ex.Message}", "Error", 
// //                     MessageBoxButton.OK, MessageBoxImage.Error);
// //                 _allLogs = new List<PickupLog>();
// //             }
// //         }

// //         private void PopulateStudentFilter()
// //         {
// //             try
// //             {
// //                 Log("Populating student filter...");

// //                 // Add "All Students" option
// //                 var allStudentsOption = new { 
// //                     Id = 0, 
// //                     FullName = "All Students" 
// //                 };

// //                 var studentList = new List<object> { allStudentsOption };

// //                 // Get students from registry
// //                 if (_studentRegistry != null && _studentRegistry.All != null)
// //                 {
// //                     foreach (var student in _studentRegistry.All.OrderBy(s => s.FullName))
// //                     {
// //                         studentList.Add(new { 
// //                             Id = student.LocalId, 
// //                             FullName = student.FullName 
// //                         });
// //                     }
// //                 }

// //                 StudentFilterComboBox.ItemsSource = studentList;
// //                 StudentFilterComboBox.DisplayMemberPath = "FullName";
// //                 StudentFilterComboBox.SelectedValuePath = "Id";
// //                 StudentFilterComboBox.SelectedIndex = 0;

// //                 Log($"Populated {studentList.Count} students in filter");
// //             }
// //             catch (Exception ex)
// //             {
// //                 Log($"Error in PopulateStudentFilter: {ex.Message}");
// //             }
// //         }

// //         private void ApplyFilters()
// //         {
// //             try
// //             {
// //                 Log("Applying filters...");

// //                 if (_allLogs == null || !_allLogs.Any())
// //                 {
// //                     Log("No logs to display");
// //                     PickupDataGrid.ItemsSource = new List<object>();
// //                     return;
// //                 }

// //                 var filteredLogs = _allLogs.AsEnumerable();

// //                 // Apply date filter
// //                 if (DateFromPicker.SelectedDate.HasValue)
// //                 {
// //                     var fromDate = DateFromPicker.SelectedDate.Value.Date;
// //                     filteredLogs = filteredLogs.Where(log => 
// //                         log.RequestedAt.Date >= fromDate);
// //                     Log($"Date From filter applied: {fromDate}");
// //                 }

// //                 if (DateToPicker.SelectedDate.HasValue)
// //                 {
// //                     var toDate = DateToPicker.SelectedDate.Value.Date;
// //                     filteredLogs = filteredLogs.Where(log => 
// //                         log.RequestedAt.Date <= toDate);
// //                     Log($"Date To filter applied: {toDate}");
// //                 }

// //                 // Apply student filter
// //                 if (StudentFilterComboBox.SelectedValue != null && 
// //                     StudentFilterComboBox.SelectedValue is int studentId && 
// //                     studentId > 0)
// //                 {
// //                     filteredLogs = filteredLogs.Where(log => log.StudentId == studentId);
// //                     Log($"Student filter applied: ID={studentId}");
// //                 }

// //                 // Apply event type filter
// //                 var selectedEventType = (EventTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
// //                 Log($"Selected event type: {selectedEventType}");

// //                 if (selectedEventType != null && selectedEventType != "All")
// //                 {
// //                     string statusFilter = selectedEventType switch
// //                     {
// //                         "GuardianScan" => "Requested",
// //                         "PickupComplete" => "Completed",
// //                         "PickupTimeout" => "Timeout",
// //                         _ => selectedEventType
// //                     };

// //                     filteredLogs = filteredLogs.Where(log => log.Status == statusFilter);
// //                     Log($"Status filter applied: {statusFilter}");
// //                 }

// //                 // Sort by requested time (newest first)
// //                 filteredLogs = filteredLogs.OrderByDescending(log => log.RequestedAt);

// //                 // Transform to display format with names
// //                 var displayLogs = new List<object>();
// //                 foreach (var log in filteredLogs)
// //                 {
// //                     displayLogs.Add(new
// //                     {
// //                         Timestamp = log.RequestedAt,
// //                         EventType = MapStatusToEventType(log.Status),
// //                         StudentId = log.StudentId,
// //                         StudentName = GetStudentName(log.StudentId),
// //                         ClassName = GetClassName(log.StudentId),
// //                         GuardianId = log.GuardianId,
// //                         GuardianName = GetGuardianName(log.GuardianId),
// //                         Details = GetDetails(log)
// //                     });
// //                 }

// //                 Log($"Displaying {displayLogs.Count} records");
// //                 PickupDataGrid.ItemsSource = displayLogs;

// //                 // Update window title with count
// //                 Title = $"Pickup History Report - {displayLogs.Count} records";

// //                 Log("Filters applied successfully");
// //             }
// //             catch (Exception ex)
// //             {
// //                 Log($"Error in ApplyFilters: {ex.Message}\n{ex.StackTrace}");
// //                 MessageBox.Show($"Error applying filters: {ex.Message}", "Error");
// //                 PickupDataGrid.ItemsSource = new List<object>();
// //             }
// //         }

// //         // Helper methods
// //         private string MapStatusToEventType(string status)
// //         {
// //             if (string.IsNullOrEmpty(status))
// //                 return "Unknown";

// //             return status switch
// //             {
// //                 "Requested" => "Guardian Scan",
// //                 "Completed" => "Pickup Complete",
// //                 "Timeout" => "Pickup Timeout",
// //                 "Cancelled" => "Pickup Cancelled",
// //                 _ => status
// //             };
// //         }

// //         private string GetDetails(PickupLog log)
// //         {
// //             if (log == null)
// //                 return "No details";

// //             if (log.Status == "Requested")
// //                 return $"Pickup requested by guardian";
// //             else if (log.Status == "Completed")
// //                 return $"Pickup completed{(log.CompletedAt.HasValue ? $" at {log.CompletedAt.Value:HH:mm:ss}" : "")}";
// //             else if (log.Status == "Timeout")
// //                 return $"Pickup timed out after waiting";
// //             else
// //                 return $"Status: {log.Status}";
// //         }

// //         private string GetStudentName(int studentId)
// //         {
// //             try
// //             {
// //                 if (_studentRegistry == null || _studentRegistry.All == null)
// //                     return $"Student {studentId}";

// //                 var student = _studentRegistry.All.FirstOrDefault(s => s.LocalId == studentId);
// //                 return student?.FullName ?? $"Student {studentId}";
// //             }
// //             catch
// //             {
// //                 return $"Student {studentId}";
// //             }
// //         }

// //         private string GetClassName(int studentId)
// //         {
// //             try
// //             {
// //                 if (_studentRegistry == null || _studentRegistry.All == null)
// //                     return "Unknown";

// //                 var student = _studentRegistry.All.FirstOrDefault(s => s.LocalId == studentId);
// //                 return student?.ClassName ?? "Unknown";
// //             }
// //             catch
// //             {
// //                 return "Unknown";
// //             }
// //         }

// //         private string GetGuardianName(int? guardianId)
// //         {
// //             try
// //             {
// //                 if (guardianId == null || guardianId == 0 || _guardianRegistry == null || _guardianRegistry.All == null)
// //                     return guardianId.HasValue ? $"Guardian {guardianId}" : "Unknown";

// //                 var guardian = _guardianRegistry.All.FirstOrDefault(g => g.LocalId == guardianId.Value);
// //                 return guardian?.FullName ?? $"Guardian {guardianId}";
// //             }
// //             catch
// //             {
// //                 return guardianId.HasValue ? $"Guardian {guardianId}" : "Unknown";
// //             }
// //         }

// //         private void Log(string message)
// //         {
// //             try
// //             {
// //                 string logPath = @"C:\temp\pickup_report_debug.txt";
// //                 Directory.CreateDirectory(Path.GetDirectoryName(logPath));
// //                 File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - {message}\n");

// //                 // Also output to Debug for immediate viewing
// //                 Debug.WriteLine(message);
// //             }
// //             catch { }
// //         }

// //         // Event handlers
// //         private void ApplyFilters_Click(object sender, RoutedEventArgs e)
// //         {
// //             Log("ApplyFilters_Click called");
// //             ApplyFilters();
// //         }

// //         private void ExportToCsv_Click(object sender, RoutedEventArgs e)
// //         {
// //             try
// //             {
// //                 Log("ExportToCsv_Click called");

// //                 var saveFileDialog = new Microsoft.Win32.SaveFileDialog
// //                 {
// //                     Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
// //                     FileName = $"PickupReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
// //                     DefaultExt = ".csv"
// //                 };

// //                 if (saveFileDialog.ShowDialog() == true)
// //                 {
// //                     var logs = PickupDataGrid.ItemsSource as IEnumerable<dynamic>;
// //                     if (logs == null || !logs.Any())
// //                     {
// //                         MessageBox.Show("No data to export.", "Export", 
// //                             MessageBoxButton.OK, MessageBoxImage.Information);
// //                         return;
// //                     }

// //                     ExportPickupLogsToCsv(logs, saveFileDialog.FileName);

// //                     MessageBox.Show($"Data exported successfully to:\n{saveFileDialog.FileName}", 
// //                         "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
// //                 }
// //             }
// //             catch (Exception ex)
// //             {
// //                 Log($"Export error: {ex.Message}");
// //                 MessageBox.Show($"Error exporting to CSV:\n{ex.Message}", 
// //                     "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
// //             }
// //         }

// //         private void ExportPickupLogsToCsv(IEnumerable<dynamic> logs, string filePath)
// //         {
// //             try
// //             {
// //                 using (var writer = new StreamWriter(filePath))
// //                 {
// //                     writer.WriteLine("Timestamp,EventType,StudentId,StudentName,ClassName,GuardianId,GuardianName,Details");

// //                     foreach (var log in logs)
// //                     {
// //                         writer.WriteLine(
// //                             $"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\"," +
// //                             $"\"{log.EventType}\"," +
// //                             $"\"{log.StudentId}\"," +
// //                             $"\"{log.StudentName}\"," +
// //                             $"\"{log.ClassName}\"," +
// //                             $"\"{log.GuardianId}\"," +
// //                             $"\"{log.GuardianName}\"," +
// //                             $"\"{log.Details}\"");
// //                     }
// //                 }

// //                 Log($"Exported CSV to: {filePath}");
// //             }
// //             catch (Exception ex)
// //             {
// //                 Log($"CSV export error: {ex.Message}");
// //                 throw;
// //             }
// //         }

// //         private void PrintReport_Click(object sender, RoutedEventArgs e)
// //         {
// //             Log("PrintReport_Click called");
// //             MessageBox.Show("Print functionality would be implemented here", "Info", 
// //                 MessageBoxButton.OK, MessageBoxImage.Information);
// //         }

// //         private void CloseButton_Click(object sender, RoutedEventArgs e)
// //         {
// //             Log("CloseButton_Click called");
// //             this.Close();
// //         }
// //     }
// // }

// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using System.Windows;
// using System.Windows.Controls;
// using BiometricStudentPickup.Services;
// using BiometricStudentPickup.Models;

// namespace BiometricStudentPickup.Views
// {
//     public partial class PickupReportWindow : Window
//     {
//         private readonly PickupLogService? _pickupLogService;  // Add nullable
//         private readonly StudentRegistry? _studentRegistry;    // Add nullable  
//         private readonly GuardianRegistry? _guardianRegistry;  // Add nullable
//         private List<PickupLog> _allLogs = new List<PickupLog>();

//         public PickupReportWindow(
//             PickupLogService pickupLogService,
//             StudentRegistry studentRegistry,
//             GuardianRegistry guardianRegistry)
//         {
//             Log("=== CONSTRUCTOR START ===");

//             try
//             {
//                 Log("Validating parameters...");

//                 if (pickupLogService == null)
//                     throw new ArgumentNullException(nameof(pickupLogService));
//                 if (studentRegistry == null)
//                     throw new ArgumentNullException(nameof(studentRegistry));
//                 if (guardianRegistry == null)
//                     throw new ArgumentNullException(nameof(guardianRegistry));

//                 _pickupLogService = pickupLogService;
//                 _studentRegistry = studentRegistry;
//                 _guardianRegistry = guardianRegistry;

//                 Log("Parameters validated successfully");

//                 InitializeComponent();
//                 Log("InitializeComponent completed");

//                 // Load data when window is ready
//                 this.Loaded += PickupReportWindow_Loaded;

//                 Log("=== CONSTRUCTOR END ===");
//             }
//             catch (Exception ex)
//             {
//                 Log($"ERROR in constructor: {ex.Message}\n{ex.StackTrace}");
//                 MessageBox.Show($"Error creating window: {ex.Message}", "Error");
//                 this.Close();
//             }
//         }

//         private void PickupReportWindow_Loaded(object sender, RoutedEventArgs e)
//         {
//             Log("=== WINDOW_LOADED START ===");

//             try
//             {
//                 // First, test the database connection
//                 TestDatabaseConnection();

//                 // Set default date range (last 7 days)
//                 DateFromPicker.SelectedDate = DateTime.Now.AddDays(-7);
//                 DateToPicker.SelectedDate = DateTime.Now;

//                 // Load all logs from service
//                 LoadAllLogs();

//                 // Populate student filter
//                 PopulateStudentFilter();

//                 // Initialize Event Type filter if not done in XAML
//                 InitializeEventTypeFilter();

//                 // Apply initial filters to show data
//                 ApplyFilters();

//                 Log("=== WINDOW_LOADED END ===");
//             }
//             catch (Exception ex)
//             {
//                 Log($"ERROR in Window_Loaded: {ex.Message}\n{ex.StackTrace}");
//                 MessageBox.Show($"Error loading data: {ex.Message}", "Error");
//             }
//         }

//         private void InitializeEventTypeFilter()
//         {
//             try
//             {
//                 Log("Initializing Event Type filter...");

//                 // Clear existing items
//                 EventTypeComboBox.Items.Clear();

//                 // Add event type options
//                 var eventTypes = new List<ComboBoxItem>
//                 {
//                     new ComboBoxItem { Content = "All", Tag = "All" },
//                     new ComboBoxItem { Content = "Guardian Scan", Tag = "GuardianScan" },
//                     new ComboBoxItem { Content = "Pickup Complete", Tag = "PickupComplete" },
//                     new ComboBoxItem { Content = "Pickup Timeout", Tag = "PickupTimeout" },
//                     new ComboBoxItem { Content = "Pickup Cancelled", Tag = "PickupCancelled" }
//                 };

//                 foreach (var item in eventTypes)
//                 {
//                     EventTypeComboBox.Items.Add(item);
//                 }

//                 // Select "All" by default
//                 EventTypeComboBox.SelectedIndex = 0;

//                 Log("Event Type filter initialized");
//             }
//             catch (Exception ex)
//             {
//                 Log($"Error initializing Event Type filter: {ex.Message}");
//             }
//         }

//         private void TestDatabaseConnection()
//         {
//             try
//             {
//                 Log("Testing database connection...");

//                 if (_pickupLogService == null)
//                 {
//                     Log("ERROR: PickupLogService is null!");
//                     return;
//                 }

//                 // Use the test method that already exists in PickupLogService
//                 _pickupLogService.TestServiceConnection();

//                 // Simple direct test using the service
//                 var logs = _pickupLogService.GetPickupLogs(DateTime.Now.AddDays(-7), DateTime.Now);
//                 Log($"Service returned {logs?.Count ?? 0} logs");

//                 if (logs != null && logs.Any())
//                 {
//                     for (int i = 0; i < Math.Min(3, logs.Count); i++)
//                     {
//                         var log = logs[i];
//                         Log($"Sample log {i}: Id={log.Id}, StudentId={log.StudentId}, Status={log.Status}, RequestedAt={log.RequestedAt}");
//                     }
//                 }
//                 else
//                 {
//                     Log("WARNING: No logs found in database!");
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Log($"Database test error: {ex.Message}");
//             }
//         }

//         private void LoadAllLogs()
//         {
//             try
//             {
//                 Log("Loading all pickup logs from service...");

//                 if (_pickupLogService == null)
//                 {
//                     Log("ERROR: PickupLogService is null!");
//                     _allLogs = new List<PickupLog>();
//                     return;
//                 }

//                 // Get logs from service (last 30 days by default)
//                 _allLogs = _pickupLogService.GetPickupLogs(
//                     DateTime.Now.AddDays(-30), 
//                     DateTime.Now
//                 );

//                 if (_allLogs == null)
//                 {
//                     Log("Service returned null");
//                     _allLogs = new List<PickupLog>();
//                 }
//                 else
//                 {
//                     Log($"Loaded {_allLogs.Count} pickup logs from database");

//                     // Show first few logs for debugging
//                     if (_allLogs.Any())
//                     {
//                         for (int i = 0; i < Math.Min(3, _allLogs.Count); i++)
//                         {
//                             var log = _allLogs[i];
//                             Log($"Log {i}: Id={log.Id}, StudentId={log.StudentId}, Status={log.Status}, RequestedAt={log.RequestedAt}");
//                         }
//                     }
//                     else
//                     {
//                         Log("WARNING: Database returned empty pickup logs!");
//                         MessageBox.Show("No pickup logs found in the database. The report will be empty.", 
//                             "Info", MessageBoxButton.OK, MessageBoxImage.Information);
//                     }
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Log($"Error in LoadAllLogs: {ex.Message}\n{ex.StackTrace}");
//                 MessageBox.Show($"Failed to load pickup logs: {ex.Message}", "Error", 
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//                 _allLogs = new List<PickupLog>();
//             }
//         }

//         private void PopulateStudentFilter()
//         {
//             try
//             {
//                 Log("Populating student filter...");

//                 // Add "All Students" option
//                 var allStudentsOption = new { 
//                     Id = 0, 
//                     FullName = "All Students" 
//                 };

//                 var studentList = new List<object> { allStudentsOption };

//                 // Get students from registry
//                 if (_studentRegistry != null && _studentRegistry.All != null)
//                 {
//                     foreach (var student in _studentRegistry.All.OrderBy(s => s.FullName))
//                     {
//                         studentList.Add(new { 
//                             Id = student.LocalId, 
//                             FullName = student.FullName 
//                         });
//                     }
//                 }

//                 StudentFilterComboBox.ItemsSource = studentList;
//                 StudentFilterComboBox.DisplayMemberPath = "FullName";
//                 StudentFilterComboBox.SelectedValuePath = "Id";
//                 StudentFilterComboBox.SelectedIndex = 0;

//                 Log($"Populated {studentList.Count} students in filter");
//             }
//             catch (Exception ex)
//             {
//                 Log($"Error in PopulateStudentFilter: {ex.Message}");
//             }
//         }

//         // private void ApplyFilters()
//         // {
//         //     try
//         //     {
//         //         Log("=== APPLY FILTERS START ===");

//         //         if (_allLogs == null || !_allLogs.Any())
//         //         {
//         //             Log("No logs to display");
//         //             PickupDataGrid.ItemsSource = new List<object>();
//         //             return;
//         //         }

//         //         Log($"Total logs in memory: {_allLogs.Count}");

//         //         var filteredLogs = _allLogs.AsEnumerable();

//         //         // Debug: Show first few logs before filtering
//         //         Log("Sample logs before filtering:");
//         //         foreach (var log in _allLogs.Take(3))
//         //         {
//         //             Log($"  - StudentId={log.StudentId}, Status={log.Status}, RequestedAt={log.RequestedAt}");
//         //         }

//         //         // Apply date filter - FIXED: Use Date property correctly
//         //         if (DateFromPicker.SelectedDate.HasValue)
//         //         {
//         //             var fromDate = DateFromPicker.SelectedDate.Value.Date;
//         //             filteredLogs = filteredLogs.Where(log => log.RequestedAt.Date >= fromDate);
//         //             Log($"Date From filter applied: {fromDate:yyyy-MM-dd}");
//         //             Log($"  Logs after from date filter: {filteredLogs.Count()}");
//         //         }

//         //         if (DateToPicker.SelectedDate.HasValue)
//         //         {
//         //             var toDate = DateToPicker.SelectedDate.Value.Date;
//         //             filteredLogs = filteredLogs.Where(log => log.RequestedAt.Date <= toDate);
//         //             Log($"Date To filter applied: {toDate:yyyy-MM-dd}");
//         //             Log($"  Logs after to date filter: {filteredLogs.Count()}");
//         //         }

//         //         // Apply student filter - FIXED: Get selected value properly
//         //         if (StudentFilterComboBox.SelectedValue != null)
//         //         {
//         //             Log($"SelectedValue type: {StudentFilterComboBox.SelectedValue.GetType().Name}");
//         //             Log($"SelectedValue: {StudentFilterComboBox.SelectedValue}");

//         //             // Try different ways to get the student ID
//         //             int studentId = 0;

//         //             if (StudentFilterComboBox.SelectedValue is int)
//         //             {
//         //                 studentId = (int)StudentFilterComboBox.SelectedValue;
//         //             }
//         //             else if (StudentFilterComboBox.SelectedItem != null)
//         //             {
//         //                 // Try to get ID from SelectedItem using reflection
//         //                 var itemType = StudentFilterComboBox.SelectedItem.GetType();
//         //                 var idProperty = itemType.GetProperty("Id");
//         //                 if (idProperty != null)
//         //                 {
//         //                     var idValue = idProperty.GetValue(StudentFilterComboBox.SelectedItem);
//         //                     if (idValue is int)
//         //                     {
//         //                         studentId = (int)idValue;
//         //                     }
//         //                 }
//         //             }

//         //             Log($"Parsed studentId: {studentId}");

//         //             if (studentId > 0)
//         //             {
//         //                 filteredLogs = filteredLogs.Where(log => log.StudentId == studentId);
//         //                 Log($"Student filter applied: ID={studentId}");
//         //                 Log($"  Logs after student filter: {filteredLogs.Count()}");
//         //             }
//         //         }
//         //         else
//         //         {
//         //             Log("StudentFilterComboBox.SelectedValue is null");
//         //         }

//         //         // Apply event type filter - FIXED: Better event type handling
//         //         if (EventTypeComboBox.SelectedItem != null)
//         //         {
//         //             Log($"EventTypeComboBox SelectedItem: {EventTypeComboBox.SelectedItem}");

//         //             string selectedEventType = "";

//         //             // Try different ways to get the event type
//         //             if (EventTypeComboBox.SelectedItem is ComboBoxItem comboItem)
//         //             {
//         //                 selectedEventType = comboItem.Tag?.ToString() ?? comboItem.Content?.ToString() ?? "";
//         //                 Log($"ComboBoxItem - Tag: {comboItem.Tag}, Content: {comboItem.Content}");
//         //             }
//         //             else if (EventTypeComboBox.SelectedItem is string)
//         //             {
//         //                 selectedEventType = EventTypeComboBox.SelectedItem.ToString();
//         //             }

//         //             Log($"Selected event type: '{selectedEventType}'");

//         //             if (!string.IsNullOrEmpty(selectedEventType) && selectedEventType != "All")
//         //             {
//         //                 string statusFilter = selectedEventType switch
//         //                 {
//         //                     "GuardianScan" => "Requested",
//         //                     "PickupComplete" => "Completed",
//         //                     "PickupTimeout" => "Timeout",
//         //                     "PickupCancelled" => "Cancelled",
//         //                     "Guardian Scan" => "Requested",  // Handle display name
//         //                     "Pickup Complete" => "Completed",
//         //                     "Pickup Timeout" => "Timeout",
//         //                     "Pickup Cancelled" => "Cancelled",
//         //                     _ => selectedEventType
//         //                 };

//         //                 Log($"Status filter to apply: '{statusFilter}'");

//         //                 filteredLogs = filteredLogs.Where(log => 
//         //                     !string.IsNullOrEmpty(log.Status) && 
//         //                     log.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));

//         //                 Log($"Logs after event type filter: {filteredLogs.Count()}");

//         //                 // Debug: Show what logs passed the filter
//         //                 var filteredList = filteredLogs.ToList();
//         //                 if (filteredList.Any())
//         //                 {
//         //                     Log($"Sample filtered logs (status='{statusFilter}'):");
//         //                     foreach (var log in filteredList.Take(3))
//         //                     {
//         //                         Log($"  - StudentId={log.StudentId}, Status={log.Status}");
//         //                     }
//         //                 }
//         //             }
//         //         }
//         //         else
//         //         {
//         //             Log("EventTypeComboBox.SelectedItem is null");
//         //         }

//         //         // Sort by requested time (newest first)
//         //         filteredLogs = filteredLogs.OrderByDescending(log => log.RequestedAt);

//         //         // Transform to display format with names
//         //         var displayLogs = new List<object>();
//         //         var filteredListFinal = filteredLogs.ToList();

//         //         Log($"Final filtered count: {filteredListFinal.Count}");

//         //         foreach (var log in filteredListFinal)
//         //         {
//         //             displayLogs.Add(new
//         //             {
//         //                 Timestamp = log.RequestedAt,
//         //                 EventType = MapStatusToEventType(log.Status),
//         //                 StudentId = log.StudentId,
//         //                 StudentName = GetStudentName(log.StudentId),
//         //                 ClassName = GetClassName(log.StudentId),
//         //                 GuardianId = log.GuardianId,
//         //                 GuardianName = GetGuardianName(log.GuardianId),
//         //                 Details = GetDetails(log)
//         //             });
//         //         }

//         //         Log($"Displaying {displayLogs.Count} records");

//         //         // Update DataGrid
//         //         PickupDataGrid.ItemsSource = displayLogs;

//         //         // Update window title with count
//         //         Title = $"Pickup History Report - {displayLogs.Count} records";

//         //         Log("=== APPLY FILTERS END ===");
//         //     }
//         //     catch (Exception ex)
//         //     {
//         //         Log($"ERROR in ApplyFilters: {ex.Message}\n{ex.StackTrace}");
//         //         MessageBox.Show($"Error applying filters: {ex.Message}", "Error");
//         //         PickupDataGrid.ItemsSource = new List<object>();
//         //     }
//         // }
//         // Add to the event handlers section
//         private void ClearFilters_Click(object sender, RoutedEventArgs e)
//         {
//             try
//             {
//                 Log("Clearing filters...");

//                 // Reset date filters to default (last 7 days)
//                 DateFromPicker.SelectedDate = DateTime.Now.AddDays(-7);
//                 DateToPicker.SelectedDate = DateTime.Now;

//                 // Reset student filter to "All Students"
//                 if (StudentFilterComboBox.Items.Count > 0)
//                 {
//                     StudentFilterComboBox.SelectedIndex = 0;
//                 }

//                 // Reset event type filter to "All Events"
//                 if (EventTypeComboBox.Items.Count > 0)
//                 {
//                     EventTypeComboBox.SelectedIndex = 0;
//                 }

//                 // Re-apply filters with cleared values
//                 ApplyFilters();

//                 Log("Filters cleared");
//             }
//             catch (Exception ex)
//             {
//                 Log($"Error clearing filters: {ex.Message}");
//                 MessageBox.Show($"Error clearing filters: {ex.Message}", "Error");
//             }
//         }

//         // Update the ApplyFilters method to handle LocalId correctly:
//         private void ApplyFilters()
//         {
//             try
//             {
//                 Log("=== APPLY FILTERS START ===");

//                 if (_allLogs == null || !_allLogs.Any())
//                 {
//                     Log("No logs to display");
//                     PickupDataGrid.ItemsSource = new List<object>();
//                     return;
//                 }

//                 Log($"Total logs in memory: {_allLogs.Count}");

//                 var filteredLogs = _allLogs.AsEnumerable();

//                 // Apply date filter
//                 if (DateFromPicker.SelectedDate.HasValue)
//                 {
//                     var fromDate = DateFromPicker.SelectedDate.Value.Date;
//                     filteredLogs = filteredLogs.Where(log => log.RequestedAt.Date >= fromDate);
//                     Log($"Date From filter applied: {fromDate:yyyy-MM-dd}");
//                 }

//                 if (DateToPicker.SelectedDate.HasValue)
//                 {
//                     var toDate = DateToPicker.SelectedDate.Value.Date;
//                     filteredLogs = filteredLogs.Where(log => log.RequestedAt.Date <= toDate);
//                     Log($"Date To filter applied: {toDate:yyyy-MM-dd}");
//                 }

//                 // Apply student filter - FIXED: Use LocalId
//                 var selectedStudent = StudentFilterComboBox.SelectedItem;
//                 if (selectedStudent != null)
//                 {
//                     // Get LocalId using reflection
//                     var itemType = selectedStudent.GetType();
//                     var localIdProperty = itemType.GetProperty("LocalId");
//                     if (localIdProperty != null)
//                     {
//                         var localIdValue = localIdProperty.GetValue(selectedStudent);
//                         if (localIdValue is int studentId && studentId > 0)
//                         {
//                             filteredLogs = filteredLogs.Where(log => log.StudentId == studentId);
//                             Log($"Student filter applied: LocalId={studentId}");
//                         }
//                     }
//                 }

//                 // Apply event type filter
//                 if (EventTypeComboBox.SelectedItem is ComboBoxItem selectedComboItem)
//                 {
//                     var tag = selectedComboItem.Tag?.ToString();
//                     Log($"Selected event type tag: '{tag}'");

//                     if (!string.IsNullOrEmpty(tag) && tag != "All")
//                     {
//                         string statusFilter = tag switch
//                         {
//                             "GuardianScan" => "Requested",
//                             "PickupComplete" => "Completed", 
//                             "PickupTimeout" => "Timeout",
//                             "Requested" => "Requested",  // Added for "Pickup Requested"
//                             _ => tag
//                         };

//                         Log($"Status filter to apply: '{statusFilter}'");

//                         filteredLogs = filteredLogs.Where(log => 
//                             !string.IsNullOrEmpty(log.Status) && 
//                             log.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
//                     }
//                 }

//                 // Sort by requested time (newest first)
//                 filteredLogs = filteredLogs.OrderByDescending(log => log.RequestedAt);

//                 // Transform to display format with names
//                 var displayLogs = new List<object>();
//                 foreach (var log in filteredLogs)
//                 {
//                     displayLogs.Add(new
//                     {
//                         Timestamp = log.RequestedAt,
//                         EventType = MapStatusToEventType(log.Status),
//                         StudentId = log.StudentId,
//                         StudentName = GetStudentName(log.StudentId),
//                         ClassName = GetClassName(log.StudentId),
//                         GuardianId = log.GuardianId,
//                         GuardianName = GetGuardianName(log.GuardianId),
//                         Details = GetDetails(log)
//                     });
//                 }

//                 Log($"Displaying {displayLogs.Count} records");

//                 // Update DataGrid
//                 PickupDataGrid.ItemsSource = displayLogs;

//                 // Update window title with count
//                 Title = $"Pickup History Report - {displayLogs.Count} records";

//                 Log("=== APPLY FILTERS END ===");
//             }
//             catch (Exception ex)
//             {
//                 Log($"ERROR in ApplyFilters: {ex.Message}\n{ex.StackTrace}");
//                 MessageBox.Show($"Error applying filters: {ex.Message}", "Error");
//                 PickupDataGrid.ItemsSource = new List<object>();
//             }
//         }


//         // Helper methods
//         private string MapStatusToEventType(string status)
//         {
//             if (string.IsNullOrEmpty(status))
//                 return "Unknown";

//             return status switch
//             {
//                 "Requested" => "Guardian Scan",
//                 "Completed" => "Pickup Complete",
//                 "Timeout" => "Pickup Timeout",
//                 "Cancelled" => "Pickup Cancelled",
//                 _ => status
//             };
//         }

//         private string GetDetails(PickupLog log)
//         {
//             if (log == null)
//                 return "No details";

//             if (log.Status == "Requested")
//                 return $"Pickup requested by guardian";
//             else if (log.Status == "Completed")
//                 return $"Pickup completed{(log.CompletedAt.HasValue ? $" at {log.CompletedAt.Value:HH:mm:ss}" : "")}";
//             else if (log.Status == "Timeout")
//                 return $"Pickup timed out after waiting";
//             else
//                 return $"Status: {log.Status}";
//         }

//         private string GetStudentName(int studentId)
//         {
//             try
//             {
//                 if (_studentRegistry == null || _studentRegistry.All == null)
//                     return $"Student {studentId}";

//                 var student = _studentRegistry.All.FirstOrDefault(s => s.LocalId == studentId);
//                 return student?.FullName ?? $"Student {studentId}";
//             }
//             catch
//             {
//                 return $"Student {studentId}";
//             }
//         }

//         private string GetClassName(int studentId)
//         {
//             try
//             {
//                 if (_studentRegistry == null || _studentRegistry.All == null)
//                     return "Unknown";

//                 var student = _studentRegistry.All.FirstOrDefault(s => s.LocalId == studentId);
//                 return student?.ClassName ?? "Unknown";
//             }
//             catch
//             {
//                 return "Unknown";
//             }
//         }

//         private string GetGuardianName(int? guardianId)
//         {
//             try
//             {
//                 if (guardianId == null || guardianId == 0 || _guardianRegistry == null || _guardianRegistry.All == null)
//                     return guardianId.HasValue ? $"Guardian {guardianId}" : "Unknown";

//                 var guardian = _guardianRegistry.All.FirstOrDefault(g => g.LocalId == guardianId.Value);
//                 return guardian?.FullName ?? $"Guardian {guardianId}";
//             }
//             catch
//             {
//                 return guardianId.HasValue ? $"Guardian {guardianId}" : "Unknown";
//             }
//         }

//         private void Log(string message)
//         {
//             // Write to file
//             try
//             {
//                 string logPath = @"C:\temp\pickup_report_debug.txt";
//                 // Fix the null warning: ensure logPath is not null
//                 string? directoryPath = Path.GetDirectoryName(logPath);
//                 if (!string.IsNullOrEmpty(directoryPath))
//                 {
//                     Directory.CreateDirectory(directoryPath);
//                 }
//                 File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - {message}\n");

//                 // Also output to Debug for immediate viewing
//                 Debug.WriteLine(message);
//             }
//             catch { }
//         }

//         // Event handlers
//         private void ApplyFilters_Click(object sender, RoutedEventArgs e)
//         {
//             Log("ApplyFilters_Click called");
//             ApplyFilters();
//         }

//         private void ExportToCsv_Click(object sender, RoutedEventArgs e)
//         {
//             try
//             {
//                 Log("ExportToCsv_Click called");

//                 var saveFileDialog = new Microsoft.Win32.SaveFileDialog
//                 {
//                     Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
//                     FileName = $"PickupReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
//                     DefaultExt = ".csv"
//                 };

//                 if (saveFileDialog.ShowDialog() == true)
//                 {
//                     var logs = PickupDataGrid.ItemsSource as IEnumerable<dynamic>;
//                     if (logs == null || !logs.Any())
//                     {
//                         MessageBox.Show("No data to export.", "Export", 
//                             MessageBoxButton.OK, MessageBoxImage.Information);
//                         return;
//                     }

//                     ExportPickupLogsToCsv(logs, saveFileDialog.FileName);

//                     MessageBox.Show($"Data exported successfully to:\n{saveFileDialog.FileName}", 
//                         "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Log($"Export error: {ex.Message}");
//                 MessageBox.Show($"Error exporting to CSV:\n{ex.Message}", 
//                     "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//         }

//         private void ExportPickupLogsToCsv(IEnumerable<dynamic> logs, string filePath)
//         {
//             try
//             {
//                 using (var writer = new StreamWriter(filePath))
//                 {
//                     writer.WriteLine("Timestamp,EventType,StudentId,StudentName,ClassName,GuardianId,GuardianName,Details");

//                     foreach (var log in logs)
//                     {
//                         writer.WriteLine(
//                             $"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\"," +
//                             $"\"{log.EventType}\"," +
//                             $"\"{log.StudentId}\"," +
//                             $"\"{log.StudentName}\"," +
//                             $"\"{log.ClassName}\"," +
//                             $"\"{log.GuardianId}\"," +
//                             $"\"{log.GuardianName}\"," +
//                             $"\"{log.Details}\"");
//                     }
//                 }

//                 Log($"Exported CSV to: {filePath}");
//             }
//             catch (Exception ex)
//             {
//                 Log($"CSV export error: {ex.Message}");
//                 throw;
//             }
//         }

//         private void PrintReport_Click(object sender, RoutedEventArgs e)
//         {
//             Log("PrintReport_Click called");
//             MessageBox.Show("Print functionality would be implemented here", "Info", 
//                 MessageBoxButton.OK, MessageBoxImage.Information);
//         }

//         private void CloseButton_Click(object sender, RoutedEventArgs e)
//         {
//             Log("CloseButton_Click called");
//             this.Close();
//         }
//     }
// }


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BiometricStudentPickup.Services;
using BiometricStudentPickup.Models;

namespace BiometricStudentPickup.Views
{
    public partial class PickupReportWindow : Window
    {
        private readonly PickupLogService? _pickupLogService;
        private readonly StudentRegistry? _studentRegistry;
        private readonly GuardianRegistry? _guardianRegistry;
        private List<PickupLog> _allLogs = new List<PickupLog>();

        public PickupReportWindow(
            PickupLogService pickupLogService,
            StudentRegistry studentRegistry,
            GuardianRegistry guardianRegistry)
        {
            Log("=== CONSTRUCTOR START ===");

            try
            {
                Log("Validating parameters...");

                if (pickupLogService == null)
                    throw new ArgumentNullException(nameof(pickupLogService));
                if (studentRegistry == null)
                    throw new ArgumentNullException(nameof(studentRegistry));
                if (guardianRegistry == null)
                    throw new ArgumentNullException(nameof(guardianRegistry));

                _pickupLogService = pickupLogService;
                _studentRegistry = studentRegistry;
                _guardianRegistry = guardianRegistry;

                Log("Parameters validated successfully");

                InitializeComponent();
                Log("InitializeComponent completed");

                // Load data when window is ready
                this.Loaded += PickupReportWindow_Loaded;

                Log("=== CONSTRUCTOR END ===");
            }
            catch (Exception ex)
            {
                Log($"ERROR in constructor: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error creating window: {ex.Message}", "Error");
                this.Close();
            }
        }

        private void PickupReportWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Log("=== WINDOW_LOADED START ===");

            try
            {
                // First, test the database connection
                TestDatabaseConnection();

                // Set default date range (last 7 days)
                DateFromPicker.SelectedDate = DateTime.Now.AddDays(-7);
                DateToPicker.SelectedDate = DateTime.Now;

                // Load all logs from service
                LoadAllLogs();

                // Populate student filter
                PopulateStudentFilter();

                // Apply initial filters to show data
                ApplyFilters();

                Log("=== WINDOW_LOADED END ===");
            }
            catch (Exception ex)
            {
                Log($"ERROR in Window_Loaded: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error loading data: {ex.Message}", "Error");
            }
        }

        private void TestDatabaseConnection()
        {
            try
            {
                Log("Testing database connection...");

                if (_pickupLogService == null)
                {
                    Log("ERROR: PickupLogService is null!");
                    return;
                }

                // Use the test method that already exists in PickupLogService
                _pickupLogService.TestServiceConnection();

                // Simple direct test using the service
                var logs = _pickupLogService.GetPickupLogs(DateTime.Now.AddDays(-7), DateTime.Now);
                Log($"Service returned {logs?.Count ?? 0} logs");

                if (logs != null && logs.Any())
                {
                    for (int i = 0; i < Math.Min(3, logs.Count); i++)
                    {
                        var log = logs[i];
                        Log($"Sample log {i}: Id={log.Id}, StudentId={log.StudentId}, Status={log.Status}, RequestedAt={log.RequestedAt}");
                    }
                }
                else
                {
                    Log("WARNING: No logs found in database!");
                }
            }
            catch (Exception ex)
            {
                Log($"Database test error: {ex.Message}");
            }
        }

        private void LoadAllLogs()
        {
            try
            {
                Log("Loading all pickup logs from service...");

                if (_pickupLogService == null)
                {
                    Log("ERROR: PickupLogService is null!");
                    _allLogs = new List<PickupLog>();
                    return;
                }

                // Get logs from service (last 30 days by default)
                _allLogs = _pickupLogService.GetPickupLogs(
                    DateTime.Now.AddDays(-30),
                    DateTime.Now
                );

                if (_allLogs == null)
                {
                    Log("Service returned null");
                    _allLogs = new List<PickupLog>();
                }
                else
                {
                    Log($"Loaded {_allLogs.Count} pickup logs from database");

                    // Show first few logs for debugging
                    if (_allLogs.Any())
                    {
                        for (int i = 0; i < Math.Min(3, _allLogs.Count); i++)
                        {
                            var log = _allLogs[i];
                            Log($"Log {i}: Id={log.Id}, StudentId={log.StudentId}, Status={log.Status}, RequestedAt={log.RequestedAt}");
                        }
                    }
                    else
                    {
                        Log("WARNING: Database returned empty pickup logs!");
                        MessageBox.Show("No pickup logs found in the database. The report will be empty.",
                            "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error in LoadAllLogs: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Failed to load pickup logs: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _allLogs = new List<PickupLog>();
            }
        }

        private void PopulateStudentFilter()
        {
            try
            {
                Log("Populating student filter...");

                // Add "All Students" option
                var allStudentsOption = new
                {
                    LocalId = 0,
                    FullName = "All Students"
                };

                var studentList = new List<object> { allStudentsOption };

                // Get students from registry
                if (_studentRegistry != null && _studentRegistry.All != null)
                {
                    foreach (var student in _studentRegistry.All.OrderBy(s => s.FullName))
                    {
                        studentList.Add(new
                        {
                            LocalId = student.LocalId,
                            FullName = student.FullName
                        });
                    }
                }

                StudentFilterComboBox.ItemsSource = studentList;
                StudentFilterComboBox.DisplayMemberPath = "FullName";
                StudentFilterComboBox.SelectedValuePath = "LocalId";
                StudentFilterComboBox.SelectedIndex = 0;

                Log($"Populated {studentList.Count} students in filter");
            }
            catch (Exception ex)
            {
                Log($"Error in PopulateStudentFilter: {ex.Message}");
            }
        }

        private void ShowSummary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("ShowSummary_Click called");

                if (_allLogs == null || !_allLogs.Any())
                {
                    MessageBox.Show("No pickup data available to show summary.",
                        "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Calculate summary statistics
                int totalPickups = _allLogs.Count;
                int completedPickups = _allLogs.Count(l => l.Status == "Completed");
                int timeoutPickups = _allLogs.Count(l => l.Status == "Timeout");
                int requestedPickups = _allLogs.Count(l => l.Status == "Requested");
                int cancelledPickups = _allLogs.Count(l => l.Status == "Cancelled");

                // Calculate completion rate
                double completionRate = totalPickups > 0 ?
                    (double)completedPickups / totalPickups * 100 : 0;

                // Get date range of logs
                var earliestLog = _allLogs.Min(l => l.RequestedAt);
                var latestLog = _allLogs.Max(l => l.RequestedAt);

                // Get unique counts
                int uniqueStudents = _allLogs.Select(l => l.StudentId).Distinct().Count();
                int uniqueGuardians = _allLogs.Select(l => l.GuardianId).Distinct().Count();

                // Create summary message
                string summary = $"📊 PICKUP REPORT SUMMARY\n" +
                                 $"========================\n\n" +
                                 $"📅 Date Range: {earliestLog:yyyy-MM-dd} to {latestLog:yyyy-MM-dd}\n" +
                                 $"📈 Total Pickup Events: {totalPickups:N0}\n\n" +
                                 $"📊 Event Breakdown:\n" +
                                 $"   • Completed: {completedPickups:N0} ({completionRate:F1}%)\n" +
                                 $"   • Timeout: {timeoutPickups:N0}\n" +
                                 $"   • Requested: {requestedPickups:N0}\n" +
                                 $"   • Cancelled: {cancelledPickups:N0}\n\n" +
                                 $"👥 Unique Participants:\n" +
                                 $"   • Students: {uniqueStudents:N0}\n" +
                                 $"   • Guardians: {uniqueGuardians:N0}\n\n" +
                                 $"⏱️ Average Pickup Time: Coming Soon\n" +
                                 $"🏆 Most Active Guardian: Coming Soon";

                MessageBox.Show(summary, "Pickup Report Summary",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"Error in ShowSummary_Click: {ex.Message}");
                MessageBox.Show($"Error showing summary: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                Log("=== APPLY FILTERS START ===");

                if (_allLogs == null || !_allLogs.Any())
                {
                    Log("No logs to display");
                    PickupDataGrid.ItemsSource = new List<object>();
                    return;
                }

                Log($"Total logs in memory: {_allLogs.Count}");

                var filteredLogs = _allLogs.AsEnumerable();

                // Apply date filter
                if (DateFromPicker.SelectedDate.HasValue)
                {
                    var fromDate = DateFromPicker.SelectedDate.Value.Date;
                    filteredLogs = filteredLogs.Where(log => log.RequestedAt.Date >= fromDate);
                    Log($"Date From filter applied: {fromDate:yyyy-MM-dd}");
                }

                if (DateToPicker.SelectedDate.HasValue)
                {
                    var toDate = DateToPicker.SelectedDate.Value.Date;
                    filteredLogs = filteredLogs.Where(log => log.RequestedAt.Date <= toDate);
                    Log($"Date To filter applied: {toDate:yyyy-MM-dd}");
                }

                // Apply student filter
                var selectedStudent = StudentFilterComboBox.SelectedItem;
                if (selectedStudent != null)
                {
                    // Get LocalId using reflection
                    var itemType = selectedStudent.GetType();
                    var localIdProperty = itemType.GetProperty("LocalId");
                    if (localIdProperty != null)
                    {
                        var localIdValue = localIdProperty.GetValue(selectedStudent);
                        if (localIdValue is int studentId && studentId > 0)
                        {
                            filteredLogs = filteredLogs.Where(log => log.StudentId == studentId);
                            Log($"Student filter applied: LocalId={studentId}");
                        }
                    }
                }

                // Apply event type filter
                if (EventTypeComboBox.SelectedItem is ComboBoxItem selectedComboItem)
                {
                    var tag = selectedComboItem.Tag?.ToString();
                    Log($"Selected event type tag: '{tag}'");

                    if (!string.IsNullOrEmpty(tag) && tag != "All")
                    {
                        string statusFilter = tag switch
                        {
                            "GuardianScan" => "Requested",
                            "PickupComplete" => "Completed",
                            "PickupTimeout" => "Timeout",
                            "Requested" => "Requested",
                            _ => tag
                        };

                        Log($"Status filter to apply: '{statusFilter}'");

                        filteredLogs = filteredLogs.Where(log =>
                            !string.IsNullOrEmpty(log.Status) &&
                            log.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
                    }
                }

                // Sort by requested time (newest first)
                filteredLogs = filteredLogs.OrderByDescending(log => log.RequestedAt);

                // Transform to display format with names
                var displayLogs = new List<object>();
                foreach (var log in filteredLogs)
                {
                    displayLogs.Add(new
                    {
                        Timestamp = log.RequestedAt,
                        EventType = MapStatusToEventType(log.Status),
                        StudentId = log.StudentId,
                        StudentName = GetStudentName(log.StudentId),
                        ClassName = GetClassName(log.StudentId),
                        GuardianId = log.GuardianId,
                        GuardianName = GetGuardianName(log.GuardianId),
                        Details = GetDetails(log)
                    });
                }

                Log($"Displaying {displayLogs.Count} records");

                // Update DataGrid
                PickupDataGrid.ItemsSource = displayLogs;

                // Update window title with count
                Title = $"Pickup History Report - {displayLogs.Count} records";

                Log("=== APPLY FILTERS END ===");
            }
            catch (Exception ex)
            {
                Log($"ERROR in ApplyFilters: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error applying filters: {ex.Message}", "Error");
                PickupDataGrid.ItemsSource = new List<object>();
            }
        }

        // Helper methods
        private string MapStatusToEventType(string status)
        {
            if (string.IsNullOrEmpty(status))
                return "Unknown";

            return status switch
            {
                "Requested" => "Guardian Scan",
                "Completed" => "Pickup Complete",
                "Timeout" => "Pickup Timeout",
                "Cancelled" => "Pickup Cancelled",
                _ => status
            };
        }

        private string GetDetails(PickupLog log)
        {
            if (log == null)
                return "No details";

            if (log.Status == "Requested")
                return $"Pickup requested by guardian";
            else if (log.Status == "Completed")
                return $"Pickup completed{(log.CompletedAt.HasValue ? $" at {log.CompletedAt.Value:HH:mm:ss}" : "")}";
            else if (log.Status == "Timeout")
                return $"Pickup timed out after waiting";
            else
                return $"Status: {log.Status}";
        }

        private string GetStudentName(int studentId)
        {
            try
            {
                if (_studentRegistry == null || _studentRegistry.All == null)
                    return $"Student {studentId}";

                var student = _studentRegistry.All.FirstOrDefault(s => s.LocalId == studentId);
                return student?.FullName ?? $"Student {studentId}";
            }
            catch
            {
                return $"Student {studentId}";
            }
        }

        private string GetClassName(int studentId)
        {
            try
            {
                if (_studentRegistry == null || _studentRegistry.All == null)
                    return "Unknown";

                var student = _studentRegistry.All.FirstOrDefault(s => s.LocalId == studentId);
                return student?.ClassName ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetGuardianName(int? guardianId)
        {
            try
            {
                if (guardianId == null || guardianId == 0 || _guardianRegistry == null || _guardianRegistry.All == null)
                    return guardianId.HasValue ? $"Guardian {guardianId}" : "Unknown";

                var guardian = _guardianRegistry.All.FirstOrDefault(g => g.LocalId == guardianId.Value);
                return guardian?.FullName ?? $"Guardian {guardianId}";
            }
            catch
            {
                return guardianId.HasValue ? $"Guardian {guardianId}" : "Unknown";
            }
        }

        private void Log(string message)
        {
            // Write to file
            try
            {
                string logPath = @"C:\temp\pickup_report_debug.txt";
                string? directoryPath = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - {message}\n");

                // Also output to Debug for immediate viewing
                Debug.WriteLine(message);
            }
            catch { }
        }

        // Event handlers
        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            Log("ApplyFilters_Click called");
            ApplyFilters();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("Clearing filters...");

                // Reset date filters to default (last 7 days)
                DateFromPicker.SelectedDate = DateTime.Now.AddDays(-7);
                DateToPicker.SelectedDate = DateTime.Now;

                // Reset student filter to "All Students"
                if (StudentFilterComboBox.Items.Count > 0)
                {
                    StudentFilterComboBox.SelectedIndex = 0;
                }

                // Reset event type filter to "All Events"
                if (EventTypeComboBox.Items.Count > 0)
                {
                    EventTypeComboBox.SelectedIndex = 0;
                }

                // Re-apply filters with cleared values
                ApplyFilters();

                Log("Filters cleared");
            }
            catch (Exception ex)
            {
                Log($"Error clearing filters: {ex.Message}");
                MessageBox.Show($"Error clearing filters: {ex.Message}", "Error");
            }
        }

        private void ExportToCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("ExportToCsv_Click called");

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    FileName = $"PickupReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    DefaultExt = ".csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var logs = PickupDataGrid.ItemsSource as IEnumerable<dynamic>;
                    if (logs == null || !logs.Any())
                    {
                        MessageBox.Show("No data to export.", "Export",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    ExportPickupLogsToCsv(logs, saveFileDialog.FileName);

                    MessageBox.Show($"Data exported successfully to:\n{saveFileDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log($"Export error: {ex.Message}");
                MessageBox.Show($"Error exporting to CSV:\n{ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportPickupLogsToCsv(IEnumerable<dynamic> logs, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("Timestamp,EventType,StudentId,StudentName,ClassName,GuardianId,GuardianName,Details");

                    foreach (var log in logs)
                    {
                        writer.WriteLine(
                            $"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\"," +
                            $"\"{log.EventType}\"," +
                            $"\"{log.StudentId}\"," +
                            $"\"{log.StudentName}\"," +
                            $"\"{log.ClassName}\"," +
                            $"\"{log.GuardianId}\"," +
                            $"\"{log.GuardianName}\"," +
                            $"\"{log.Details}\"");
                    }
                }

                Log($"Exported CSV to: {filePath}");
            }
            catch (Exception ex)
            {
                Log($"CSV export error: {ex.Message}");
                throw;
            }
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            Log("PrintReport_Click called");
            MessageBox.Show("Coming Soon!!!", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Log("CloseButton_Click called");
            this.Close();
        }
    }
}