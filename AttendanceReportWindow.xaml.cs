// // AttendanceReportWindow.xaml.cs
// using System;
// using System.Collections.ObjectModel;
// using System.Windows;
// using System.Windows.Controls;
// using BiometricStudentPickup.Models;
// using BiometricStudentPickup.Services;

// namespace BiometricStudentPickup
// {
//     public partial class AttendanceReportWindow : Window
//     {
//         private readonly AttendanceService _attendanceService;
//         private readonly StudentRegistry _studentRegistry;
//         private ObservableCollection<AttendanceRecord> _attendanceRecords;

//         public class AttendanceRecord
//         {
//             public string StudentName { get; set; }
//             public string ClassName { get; set; }
//             public string Date { get; set; }
//             public string TimeIn { get; set; }
//             public string Status { get; set; }
//         }

//         public AttendanceReportWindow(AttendanceService attendanceService, StudentRegistry studentRegistry)
//         {
//             InitializeComponent();

//             _attendanceService = attendanceService;
//             _studentRegistry = studentRegistry;
//             _attendanceRecords = new ObservableCollection<AttendanceRecord>();

//             AttendanceDataGrid.ItemsSource = _attendanceRecords;

//             // Load today's attendance by default
//             LoadTodayAttendance();
//         }

//         private void LoadTodayAttendance()
//         {
//             try
//             {
//                 _attendanceRecords.Clear();
//                 var today = DateTime.Today;
//                 var attendance = _attendanceService.GetAttendanceByDate(today);

//                 foreach (var record in attendance)
//                 {
//                     var student = _studentRegistry.FindById(record.StudentId);
//                     if (student != null)
//                     {
//                         _attendanceRecords.Add(new AttendanceRecord
//                         {
//                             StudentName = student.FullName,
//                             ClassName = student.ClassName,
//                             Date = record.Date.ToString("yyyy-MM-dd"),
//                             TimeIn = record.TimeIn.ToString("HH:mm:ss"),
//                             Status = "Present"
//                         });
//                     }
//                 }

//                 // Add absent students
//                 var presentStudentIds = _attendanceService.GetTodayPresentStudentIds();
//                 foreach (var student in _studentRegistry.All)
//                 {
//                     if (!presentStudentIds.Contains(student.LocalId))
//                     {
//                         _attendanceRecords.Add(new AttendanceRecord
//                         {
//                             StudentName = student.FullName,
//                             ClassName = student.ClassName,
//                             Date = today.ToString("yyyy-MM-dd"),
//                             TimeIn = "N/A",
//                             Status = "Absent"
//                         });
//                     }
//                 }

//                 // Update summary
//                 UpdateSummary(today);
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show($"Error loading attendance: {ex.Message}", "Error", 
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//         }

//         private void UpdateSummary(DateTime date)
//         {
//             int totalStudents = _studentRegistry.All.Count;
//             int presentCount = _attendanceService.GetTodayAttendanceCount();
//             int absentCount = totalStudents - presentCount;

//             DateLabel.Content = $"Date: {date:yyyy-MM-dd}";
//             SummaryLabel.Content = $"Total: {totalStudents} | Present: {presentCount} | Absent: {absentCount}";
//         }

//         private void TodayButton_Click(object sender, RoutedEventArgs e)
//         {
//             LoadTodayAttendance();
//         }

//         private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
//         {
//             if (DatePicker.SelectedDate.HasValue)
//             {
//                 LoadAttendanceForDate(DatePicker.SelectedDate.Value);
//             }
//         }

//         private void LoadAttendanceForDate(DateTime date)
//         {
//             try
//             {
//                 _attendanceRecords.Clear();
//                 var attendance = _attendanceService.GetAttendanceByDate(date);

//                 foreach (var record in attendance)
//                 {
//                     var student = _studentRegistry.FindById(record.StudentId);
//                     if (student != null)
//                     {
//                         _attendanceRecords.Add(new AttendanceRecord
//                         {
//                             StudentName = student.FullName,
//                             ClassName = student.ClassName,
//                             Date = record.Date.ToString("yyyy-MM-dd"),
//                             TimeIn = record.TimeIn.ToString("HH:mm:ss"),
//                             Status = "Present"
//                         });
//                     }
//                 }

//                 UpdateSummary(date);
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show($"Error loading attendance: {ex.Message}", "Error", 
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//         }

//         private void ExportButton_Click(object sender, RoutedEventArgs e)
//         {
//             try
//             {
//                 var date = DatePicker.SelectedDate ?? DateTime.Today;
//                 var dateStr = date.ToString("yyyy-MM-dd");
//                 var fileName = $"Attendance_{dateStr}.csv";

//                 using var writer = new System.IO.StreamWriter(fileName);
//                 writer.WriteLine("Student Name,Class,Date,Time In,Status");

//                 foreach (var record in _attendanceRecords)
//                 {
//                     writer.WriteLine($"\"{record.StudentName}\",{record.ClassName},{record.Date},{record.TimeIn},{record.Status}");
//                 }

//                 writer.Close();
//                 MessageBox.Show($"Attendance report exported to: {fileName}", "Export Complete", 
//                     MessageBoxButton.OK, MessageBoxImage.Information);
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show($"Error exporting: {ex.Message}", "Export Error", 
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//         }

//         private void CloseButton_Click(object sender, RoutedEventArgs e)
//         {
//             this.Close();
//         }
//     }
// }

// // AttendanceReportWindow.xaml.cs
// using System;
// using System.Collections.ObjectModel;
// using System.Linq;
// using System.Windows;
// using System.Windows.Controls;
// using BiometricStudentPickup.Models;
// using BiometricStudentPickup.Services;

// namespace BiometricStudentPickup
// {
//     public partial class AttendanceReportWindow : Window
//     {
//         private readonly AttendanceService _attendanceService;
//         private readonly StudentRegistry _studentRegistry;
//         private ObservableCollection<AttendanceRecord> _attendanceRecords;

//         public class AttendanceRecord
//         {
//             public string? StudentName { get; set; }
//             public string? ClassName { get; set; }
//             public string? Date { get; set; }
//             public string? TimeIn { get; set; }
//             public string? Status { get; set; }
//         }

//         public AttendanceReportWindow(AttendanceService attendanceService, StudentRegistry studentRegistry)
//         {
//             InitializeComponent();

//             _attendanceService = attendanceService;
//             _studentRegistry = studentRegistry;
//             _attendanceRecords = new ObservableCollection<AttendanceRecord>();

//             AttendanceDataGrid.ItemsSource = _attendanceRecords;

//             // Load today's attendance by default
//             LoadTodayAttendance();
//         }

//         private void LoadTodayAttendance()
//         {
//             try
//             {
//                 _attendanceRecords.Clear();
//                 var today = DateTime.Today;
//                 var attendance = _attendanceService.GetAttendanceByDate(today);

//                 foreach (var record in attendance)
//                 {
//                     // FIX: Use FirstOrDefault with LocalId instead of FindById
//                     var student = _studentRegistry.All.FirstOrDefault(s => s.LocalId == record.StudentId);
//                     if (student != null)
//                     {
//                         _attendanceRecords.Add(new AttendanceRecord
//                         {
//                             StudentName = student.FullName,
//                             ClassName = student.ClassName,
//                             Date = record.Date.ToString("yyyy-MM-dd"),
//                             TimeIn = record.TimeIn.ToString("HH:mm:ss"),
//                             Status = "Present"
//                         });
//                     }
//                 }

//                 // Add absent students
//                 var presentStudentIds = _attendanceService.GetTodayPresentStudentIds();
//                 foreach (var student in _studentRegistry.All)
//                 {
//                     if (!presentStudentIds.Contains(student.LocalId))
//                     {
//                         _attendanceRecords.Add(new AttendanceRecord
//                         {
//                             StudentName = student.FullName,
//                             ClassName = student.ClassName,
//                             Date = today.ToString("yyyy-MM-dd"),
//                             TimeIn = "N/A",
//                             Status = "Absent"
//                         });
//                     }
//                 }

//                 // Update summary
//                 UpdateSummary(today);
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show($"Error loading attendance: {ex.Message}", "Error", 
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//         }

//         private void UpdateSummary(DateTime date)
//         {
//             int totalStudents = _studentRegistry.All.Count;
//             int presentCount = _attendanceService.GetTodayAttendanceCount();
//             int absentCount = totalStudents - presentCount;

//             // FIX: TextBlock uses Text property, not Content
//             DateLabel.Text = $"Date: {date:yyyy-MM-dd}";
//             SummaryLabel.Text = $"Total: {totalStudents} | Present: {presentCount} | Absent: {absentCount}";
//         }

//         private void TodayButton_Click(object sender, RoutedEventArgs e)
//         {
//             LoadTodayAttendance();
//         }

//         private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
//         {
//             if (DatePicker.SelectedDate.HasValue)
//             {
//                 LoadAttendanceForDate(DatePicker.SelectedDate.Value);
//             }
//         }

//         private void LoadAttendanceForDate(DateTime date)
//         {
//             try
//             {
//                 _attendanceRecords.Clear();
//                 var attendance = _attendanceService.GetAttendanceByDate(date);

//                 foreach (var record in attendance)
//                 {
//                     // FIX: Use FirstOrDefault with LocalId instead of FindById
//                     var student = _studentRegistry.All.FirstOrDefault(s => s.LocalId == record.StudentId);
//                     if (student != null)
//                     {
//                         _attendanceRecords.Add(new AttendanceRecord
//                         {
//                             StudentName = student.FullName,
//                             ClassName = student.ClassName,
//                             Date = record.Date.ToString("yyyy-MM-dd"),
//                             TimeIn = record.TimeIn.ToString("HH:mm:ss"),
//                             Status = "Present"
//                         });
//                     }
//                 }

//                 UpdateSummary(date);
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show($"Error loading attendance: {ex.Message}", "Error", 
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//         }

//         private void ExportButton_Click(object sender, RoutedEventArgs e)
//         {
//             try
//             {
//                 var date = DatePicker.SelectedDate ?? DateTime.Today;
//                 var dateStr = date.ToString("yyyy-MM-dd");
//                 var fileName = $"Attendance_{dateStr}.csv";

//                 using var writer = new System.IO.StreamWriter(fileName);
//                 writer.WriteLine("Student Name,Class,Date,Time In,Status");

//                 foreach (var record in _attendanceRecords)
//                 {
//                     writer.WriteLine($"\"{record.StudentName}\",{record.ClassName},{record.Date},{record.TimeIn},{record.Status}");
//                 }

//                 writer.Close();
//                 MessageBox.Show($"Attendance report exported to: {fileName}", "Export Complete", 
//                     MessageBoxButton.OK, MessageBoxImage.Information);
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show($"Error exporting: {ex.Message}", "Export Error", 
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//         }

//         private void CloseButton_Click(object sender, RoutedEventArgs e)
//         {
//             this.Close();
//         }
//     }
// }

// // AttendanceReportWindow.xaml.cs
// using System;
// using System.Collections.ObjectModel;
// using System.Linq;
// using System.Windows;
// using System.Windows.Controls;
// using BiometricStudentPickup.Models;
// using BiometricStudentPickup.Services;

// namespace BiometricStudentPickup
// {
//     public partial class AttendanceReportWindow : Window
//     {
//         private readonly AttendanceService? _attendanceService;
//         private readonly StudentRegistry? _studentRegistry;
//         private ObservableCollection<AttendanceRecord>? _attendanceRecords;
//         private string _selectedClass = "All Classes"; // Track selected class filter

//         public class AttendanceRecord
//         {
//             public string StudentName { get; set; } = string.Empty;
//             public string ClassName { get; set; } = string.Empty;
//             public string Date { get; set; } = string.Empty;
//             public string TimeIn { get; set; } = string.Empty;
//             public string Status { get; set; } = string.Empty;
//         }

//         public AttendanceReportWindow(AttendanceService attendanceService, StudentRegistry studentRegistry)
//         {
//             Console.WriteLine($"Constructor called with attendanceService: {attendanceService != null}");
//             Console.WriteLine($"Constructor called with studentRegistry: {studentRegistry != null}");

//             // ADD NULL CHECK HERE
//             if (attendanceService == null)
//             {
//                 Console.WriteLine("ERROR: attendanceService is null in constructor!");
//                 throw new ArgumentNullException(nameof(attendanceService), "Attendance service cannot be null");
//             }

//             if (studentRegistry == null)
//             {
//                 Console.WriteLine("ERROR: studentRegistry is null in constructor!");
//                 throw new ArgumentNullException(nameof(studentRegistry), "Student registry cannot be null");
//             }

//             // Initialize the collection FIRST
//             _attendanceRecords = new ObservableCollection<AttendanceRecord>();
//             Console.WriteLine($"Created _attendanceRecords: {_attendanceRecords != null}");

//             InitializeComponent();
//             Console.WriteLine("InitializeComponent completed");

//             _attendanceService = attendanceService;
//             _studentRegistry = studentRegistry;

//             Console.WriteLine($"Set _attendanceService: {_attendanceService != null}");
//             Console.WriteLine($"Set _studentRegistry: {_studentRegistry != null}");

//             AttendanceDataGrid.ItemsSource = _attendanceRecords;
//             Console.WriteLine("Set ItemsSource");

//             // Populate class filter dropdown
//             PopulateClassFilter();
//             Console.WriteLine("PopulateClassFilter completed");

//             // Load today's attendance by default
//             LoadTodayAttendance();
//             Console.WriteLine("LoadTodayAttendance completed");
//         }

//         // private void PopulateClassFilter()
//         // {
//         //     // Clear existing items
//         //     ClassFilterComboBox.Items.Clear();

//         //     // Add "All Classes" option
//         //     ClassFilterComboBox.Items.Add("All Classes");

//         //     // Get unique class names from student registry
//         //     var uniqueClasses = _studentRegistry.All
//         //         .Select(s => s.ClassName)
//         //         .Where(c => !string.IsNullOrEmpty(c))
//         //         .Distinct()
//         //         .OrderBy(c => c)
//         //         .ToList();

//         //     foreach (var className in uniqueClasses)
//         //     {
//         //         ClassFilterComboBox.Items.Add(className ?? string.Empty);
//         //     }

//         //     // Set default selection
//         //     ClassFilterComboBox.SelectedIndex = 0;
//         // }
//         private void PopulateClassFilter()
//         {
//             try
//             {
//                 // Clear existing items
//                 ClassFilterComboBox.Items.Clear();

//                 // Add "All Classes" option
//                 ClassFilterComboBox.Items.Add("All Classes");

//                 // ADD NULL CHECK HERE
//                 if (_studentRegistry == null)
//                 {
//                     MessageBox.Show("Student registry is not initialized.", "Error",
//                         MessageBoxButton.OK, MessageBoxImage.Error);
//                     return;
//                 }

//                 if (_studentRegistry.All == null)
//                 {
//                     MessageBox.Show("Student list is empty or not loaded.", "Information",
//                         MessageBoxButton.OK, MessageBoxImage.Information);
//                     return;
//                 }

//                 // Get unique class names from student registry
//                 var uniqueClasses = _studentRegistry.All
//                     .Select(s => s?.ClassName) // ADD null check for student
//                     .Where(c => !string.IsNullOrEmpty(c))
//                     .Distinct()
//                     .OrderBy(c => c)
//                     .ToList();

//                 foreach (var className in uniqueClasses)
//                 {
//                     ClassFilterComboBox.Items.Add(className ?? string.Empty);
//                 }

//                 // Set default selection
//                 ClassFilterComboBox.SelectedIndex = 0;
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show($"Error populating class filter: {ex.Message}", "Error",
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//         }

//         private void ClassFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//         {
//             if (ClassFilterComboBox.SelectedItem != null)
//             {
//                 _selectedClass = ClassFilterComboBox.SelectedItem.ToString() ?? "All Classes";

//                 // Reload attendance data with filter applied
//                 if (DatePicker.SelectedDate.HasValue)
//                 {
//                     LoadAttendanceForDate(DatePicker.SelectedDate.Value);
//                 }
//                 else
//                 {
//                     LoadTodayAttendance();
//                 }
//             }
//         }

//         private void LoadTodayAttendance()
//         {
//             LoadAttendanceForDate(DateTime.Today);
//         }

//         // private void LoadAttendanceForDate(DateTime date)
//         // {
//         //     try
//         //     {
//         //         _attendanceRecords.Clear();

//         //         // Get attendance for the selected date
//         //         var attendance = _attendanceService.GetAttendanceByDate(date);

//         //         // ADD THIS CHECK
//         //         if (attendance == null)
//         //         {
//         //             MessageBox.Show("No attendance data available for the selected date.",
//         //                 "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
//         //             return;
//         //         }

//         //         // Get all student IDs that were present
//         //         var presentStudentIds = attendance.Select(a => a.StudentId).ToList();

//         //         // Process all students
//         //         foreach (var student in _studentRegistry.All)
//         //         {
//         //             // Apply class filter
//         //             if (_selectedClass != "All Classes" && student.ClassName != _selectedClass)
//         //                 continue;

//         //             // Check if student was present
//         //             if (presentStudentIds.Contains(student.LocalId))
//         //             {
//         //                 // Student was present - get their attendance record
//         //                 var attendanceRecord = attendance.FirstOrDefault(a => a.StudentId == student.LocalId);
//         //                 if (attendanceRecord != null)
//         //                 {
//         //                     _attendanceRecords.Add(new AttendanceRecord
//         //                     {
//         //                         StudentName = student.FullName ?? string.Empty,
//         //                         ClassName = student.ClassName ?? string.Empty,
//         //                         Date = attendanceRecord.Date.ToString("yyyy-MM-dd"),
//         //                         TimeIn = attendanceRecord.TimeIn.ToString("HH:mm:ss"),
//         //                         Status = "Present"
//         //                     });
//         //                 }
//         //             }
//         //             else
//         //             {
//         //                 // Student was absent
//         //                 _attendanceRecords.Add(new AttendanceRecord
//         //                 {
//         //                     StudentName = student.FullName ?? string.Empty,
//         //                     ClassName = student.ClassName ?? string.Empty,
//         //                     Date = date.ToString("yyyy-MM-dd"),
//         //                     TimeIn = "N/A",
//         //                     Status = "Absent"
//         //                 });
//         //             }
//         //         }

//         //         // Update summary
//         //         UpdateSummary(date);
//         //     }
//         //     catch (Exception ex)
//         //     {
//         //         MessageBox.Show($"Error loading attendance: {ex.Message}", "Error",
//         //             MessageBoxButton.OK, MessageBoxImage.Error);
//         //     }
//         // }
//         private void LoadAttendanceForDate(DateTime date)
//         {
//             try
//             {
//                 // ADD THIS CHECK AT THE VERY BEGINNING
//                 if (_attendanceRecords == null)
//                 {
//                     _attendanceRecords = new ObservableCollection<AttendanceRecord>();
//                     AttendanceDataGrid.ItemsSource = _attendanceRecords; // Reassign if needed
//                 }

//                 _attendanceRecords.Clear();

//                 // Get attendance for the selected date
//                 var attendance = _attendanceService.GetAttendanceByDate(date);

//                 // ADD THIS CHECK
//                 if (attendance == null)
//                 {
//                     MessageBox.Show("No attendance data available for the selected date.",
//                         "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
//                     return;
//                 }

//                 // Get all student IDs that were present
//                 var presentStudentIds = attendance.Select(a => a.StudentId).ToList();

//                 // ADD CHECK FOR _studentRegistry.All
//                 if (_studentRegistry.All == null)
//                 {
//                     MessageBox.Show("Student registry data is not available.", "Error",
//                         MessageBoxButton.OK, MessageBoxImage.Error);
//                     return;
//                 }

//                 // Process all students
//                 foreach (var student in _studentRegistry.All)
//                 {
//                     // Apply class filter
//                     if (_selectedClass != "All Classes" && student.ClassName != _selectedClass)
//                         continue;

//                     // Check if student was present
//                     if (presentStudentIds.Contains(student.LocalId))
//                     {
//                         // Student was present - get their attendance record
//                         var attendanceRecord = attendance.FirstOrDefault(a => a.StudentId == student.LocalId);
//                         if (attendanceRecord != null)
//                         {
//                             // ADD NULL CHECKS FOR attendanceRecord properties
//                             _attendanceRecords.Add(new AttendanceRecord
//                             {
//                                 StudentName = student.FullName ?? string.Empty,
//                                 ClassName = student.ClassName ?? string.Empty,
//                                 Date = attendanceRecord.Date.ToString("yyyy-MM-dd"),
//                                 TimeIn = attendanceRecord.TimeIn.ToString("HH:mm:ss"),
//                                 Status = "Present"
//                             });
//                         }
//                     }
//                     else
//                     {
//                         // Student was absent
//                         _attendanceRecords.Add(new AttendanceRecord
//                         {
//                             StudentName = student.FullName ?? string.Empty,
//                             ClassName = student.ClassName ?? string.Empty,
//                             Date = date.ToString("yyyy-MM-dd"),
//                             TimeIn = "N/A",
//                             Status = "Absent"
//                         });
//                     }
//                 }

//                 // Update summary
//                 UpdateSummary(date);
//             }
//             catch (NullReferenceException nex)
//             {
//                 // ADD SPECIAL HANDLING FOR NULL REFERENCE
//                 MessageBox.Show($"Null reference error: {nex.Message}\n\nStack trace:\n{nex.StackTrace}",
//                     "Null Reference Error", MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show($"Error loading attendance: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Error",
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//         }

//         private void UpdateSummary(DateTime date)
//         {
//             try
//             {
//                 // Calculate counts
//                 int totalStudents = _selectedClass == "All Classes"
//                     ? _studentRegistry.All.Count
//                     : _studentRegistry.All.Count(s => s.ClassName == _selectedClass);

//                 int presentCount = _attendanceRecords.Count(r => r.Status == "Present");
//                 int absentCount = totalStudents - presentCount;
//                 int lateCount = 0; // You can add late logic if needed

//                 // Update date label
//                 string dateText;
//                 if (date.Date == DateTime.Today.Date)
//                 {
//                     dateText = $"Today, {date:MMMM d, yyyy}";
//                 }
//                 else if (date.Date == DateTime.Today.AddDays(-1).Date)
//                 {
//                     dateText = $"Yesterday, {date:MMMM d, yyyy}";
//                 }
//                 else
//                 {
//                     dateText = $"{date:dddd, MMMM d, yyyy}";
//                 }

//                 DateLabel.Text = dateText;

//                 // Update summary label
//                 SummaryLabel.Text = $"Total: {totalStudents} | Present: {presentCount} | Late: {lateCount} | Absent: {absentCount}";
//             }
//             catch (Exception ex)
//             {
//                 SummaryLabel.Text = $"Error updating summary: {ex.Message}";
//             }
//         }

//         private void TodayButton_Click(object sender, RoutedEventArgs e)
//         {
//             DatePicker.SelectedDate = DateTime.Today;
//             LoadTodayAttendance();
//         }

//         private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
//         {
//             if (DatePicker.SelectedDate.HasValue)
//             {
//                 LoadAttendanceForDate(DatePicker.SelectedDate.Value);
//             }
//         }

//         private void ExportButton_Click(object sender, RoutedEventArgs e)
//         {
//             try
//             {
//                 var date = DatePicker.SelectedDate ?? DateTime.Today;
//                 var dateStr = date.ToString("yyyy-MM-dd");
//                 var className = _selectedClass == "All Classes" ? "All" : _selectedClass.Replace(" ", "_");
//                 var fileName = $"Attendance_{dateStr}_{className}.csv";

//                 using var writer = new System.IO.StreamWriter(fileName);
//                 writer.WriteLine("Student Name,Class,Date,Time In,Status");

//                 foreach (var record in _attendanceRecords)
//                 {
//                     writer.WriteLine($"\"{record.StudentName ?? ""}\",{record.ClassName ?? ""},{record.Date ?? ""},{record.TimeIn ?? ""},{record.Status ?? ""}");
//                 }

//                 writer.Close();
//                 MessageBox.Show($"Attendance report exported to: {fileName}", "Export Complete",
//                     MessageBoxButton.OK, MessageBoxImage.Information);
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show($"Error exporting: {ex.Message}", "Export Error",
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//         }

//         private void CloseButton_Click(object sender, RoutedEventArgs e)
//         {
//             this.Close();
//         }
//     }
// }

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BiometricStudentPickup.Models;
using BiometricStudentPickup.Services;

namespace BiometricStudentPickup
{
    public partial class AttendanceReportWindow : Window
    {
        private readonly AttendanceService _attendanceService = null!;
        private readonly StudentRegistry _studentRegistry = null!;
        private ObservableCollection<AttendanceRecord> _attendanceRecords = null!;
        private string _selectedClass = "All Classes"; // Track selected class filter

        public class AttendanceRecord
        {
            public string StudentName { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
            public string TimeIn { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        public AttendanceReportWindow(AttendanceService attendanceService, StudentRegistry studentRegistry)
        {
            // Validate parameters
            if (attendanceService == null)
                throw new ArgumentNullException(nameof(attendanceService));
            if (studentRegistry == null)
                throw new ArgumentNullException(nameof(studentRegistry));

            // Initialize the collection
            _attendanceRecords = new ObservableCollection<AttendanceRecord>();

            // Now assign the validated parameters
            _attendanceService = attendanceService;
            _studentRegistry = studentRegistry;

            Console.WriteLine($"Constructor called with attendanceService: {_attendanceService != null}");
            Console.WriteLine($"Constructor called with studentRegistry: {_studentRegistry != null}");
            Console.WriteLine($"Created _attendanceRecords: {_attendanceRecords != null}");

            InitializeComponent();
            Console.WriteLine("InitializeComponent completed");

            Console.WriteLine($"Set _attendanceService: {_attendanceService != null}");
            Console.WriteLine($"Set _studentRegistry: {_studentRegistry != null}");

            AttendanceDataGrid.ItemsSource = _attendanceRecords;
            Console.WriteLine("Set ItemsSource");

            // Populate class filter dropdown
            PopulateClassFilter();
            Console.WriteLine("PopulateClassFilter completed");

            // Load today's attendance by default
            LoadTodayAttendance();
            Console.WriteLine("LoadTodayAttendance completed");
        }

        private void PopulateClassFilter()
        {
            try
            {
                // Clear existing items
                ClassFilterComboBox.Items.Clear();

                // Add "All Classes" option
                ClassFilterComboBox.Items.Add("All Classes");

                // Check for null (shouldn't happen but defensive)
                if (_studentRegistry.All == null)
                {
                    MessageBox.Show("Student list is empty or not loaded.", "Information",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Get unique class names from student registry
                var uniqueClasses = _studentRegistry.All
                    .Select(s => s?.ClassName)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                foreach (var className in uniqueClasses)
                {
                    ClassFilterComboBox.Items.Add(className ?? string.Empty);
                }

                // Set default selection
                ClassFilterComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error populating class filter: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClassFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClassFilterComboBox.SelectedItem != null)
            {
                _selectedClass = ClassFilterComboBox.SelectedItem.ToString() ?? "All Classes";

                // Reload attendance data with filter applied
                if (DatePicker.SelectedDate.HasValue)
                {
                    LoadAttendanceForDate(DatePicker.SelectedDate.Value);
                }
                else
                {
                    LoadTodayAttendance();
                }
            }
        }

        private void LoadTodayAttendance()
        {
            LoadAttendanceForDate(DateTime.Today);
        }

        private void LoadAttendanceForDate(DateTime date)
        {
            try
            {
                // Get attendance for the selected date
                var attendance = _attendanceService.GetAttendanceByDate(date);

                if (attendance == null)
                {
                    MessageBox.Show("No attendance data available for the selected date.",
                        "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _attendanceRecords.Clear();

                // Get all student IDs that were present
                var presentStudentIds = attendance.Select(a => a.StudentId).ToList();

                // Check for null student registry (defensive)
                if (_studentRegistry.All == null)
                {
                    MessageBox.Show("Student registry data is not available.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Process all students
                foreach (var student in _studentRegistry.All)
                {
                    // Apply class filter
                    if (_selectedClass != "All Classes" && student.ClassName != _selectedClass)
                        continue;

                    // Check if student was present
                    if (presentStudentIds.Contains(student.LocalId))
                    {
                        // Student was present - get their attendance record
                        var attendanceRecord = attendance.FirstOrDefault(a => a.StudentId == student.LocalId);
                        if (attendanceRecord != null)
                        {
                            _attendanceRecords.Add(new AttendanceRecord
                            {
                                StudentName = student.FullName ?? string.Empty,
                                ClassName = student.ClassName ?? string.Empty,
                                Date = attendanceRecord.Date.ToString("yyyy-MM-dd"),
                                TimeIn = attendanceRecord.TimeIn.ToString("HH:mm:ss"),
                                Status = "Present"
                            });
                        }
                    }
                    else
                    {
                        // Student was absent
                        _attendanceRecords.Add(new AttendanceRecord
                        {
                            StudentName = student.FullName ?? string.Empty,
                            ClassName = student.ClassName ?? string.Empty,
                            Date = date.ToString("yyyy-MM-dd"),
                            TimeIn = "N/A",
                            Status = "Absent"
                        });
                    }
                }

                // Update summary
                UpdateSummary(date);
            }
            catch (NullReferenceException nex)
            {
                MessageBox.Show($"Null reference error: {nex.Message}\n\nStack trace:\n{nex.StackTrace}",
                    "Null Reference Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading attendance: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummary(DateTime date)
        {
            try
            {
                // Defensive null checks (though fields should never be null)
                if (_studentRegistry.All == null)
                {
                    SummaryLabel.Text = "Student data not available";
                    return;
                }

                if (_attendanceRecords == null)
                {
                    SummaryLabel.Text = "Attendance records not available";
                    return;
                }

                // Calculate counts
                int totalStudents = _selectedClass == "All Classes"
                    ? _studentRegistry.All.Count()
                    : _studentRegistry.All.Count(s => s.ClassName == _selectedClass);

                int presentCount = _attendanceRecords.Count(r => r.Status == "Present");
                int absentCount = totalStudents - presentCount;
                int lateCount = 0;

                // Update date label
                string dateText;
                if (date.Date == DateTime.Today.Date)
                {
                    dateText = $"Today, {date:MMMM d, yyyy}";
                }
                else if (date.Date == DateTime.Today.AddDays(-1).Date)
                {
                    dateText = $"Yesterday, {date:MMMM d, yyyy}";
                }
                else
                {
                    dateText = $"{date:dddd, MMMM d, yyyy}";
                }

                DateLabel.Text = dateText;

                // Update summary label
                SummaryLabel.Text = $"Total: {totalStudents} | Present: {presentCount} | Late: {lateCount} | Absent: {absentCount}";
            }
            catch (Exception ex)
            {
                SummaryLabel.Text = $"Error updating summary: {ex.Message}";
            }
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            DatePicker.SelectedDate = DateTime.Today;
            LoadTodayAttendance();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePicker.SelectedDate.HasValue)
            {
                LoadAttendanceForDate(DatePicker.SelectedDate.Value);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var date = DatePicker.SelectedDate ?? DateTime.Today;
                var dateStr = date.ToString("yyyy-MM-dd");
                var className = _selectedClass == "All Classes" ? "All" : _selectedClass.Replace(" ", "_");
                var defaultFileName = $"Attendance_{dateStr}_{className}.csv";

                // Create SaveFileDialog
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = defaultFileName,
                    DefaultExt = ".csv",
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads"
                };

                // Show dialog
                bool? result = saveFileDialog.ShowDialog();

                if (result == true)
                {
                    using var writer = new System.IO.StreamWriter(saveFileDialog.FileName);
                    writer.WriteLine("Student Name,Class,Date,Time In,Status");

                    foreach (var record in _attendanceRecords)
                    {
                        writer.WriteLine($"\"{record.StudentName ?? ""}\",{record.ClassName ?? ""},{record.Date ?? ""},{record.TimeIn ?? ""},{record.Status ?? ""}");
                    }

                    writer.Close();

                    MessageBox.Show($"Attendance report exported to:\n{saveFileDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}