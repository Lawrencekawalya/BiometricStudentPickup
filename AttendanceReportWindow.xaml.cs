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

// AttendanceReportWindow.xaml.cs
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
        private readonly AttendanceService _attendanceService;
        private readonly StudentRegistry _studentRegistry;
        private ObservableCollection<AttendanceRecord> _attendanceRecords;
        
        public class AttendanceRecord
        {
            public string? StudentName { get; set; }
            public string? ClassName { get; set; }
            public string? Date { get; set; }
            public string? TimeIn { get; set; }
            public string? Status { get; set; }
        }

        public AttendanceReportWindow(AttendanceService attendanceService, StudentRegistry studentRegistry)
        {
            InitializeComponent();
            
            _attendanceService = attendanceService;
            _studentRegistry = studentRegistry;
            _attendanceRecords = new ObservableCollection<AttendanceRecord>();
            
            AttendanceDataGrid.ItemsSource = _attendanceRecords;
            
            // Load today's attendance by default
            LoadTodayAttendance();
        }

        private void LoadTodayAttendance()
        {
            try
            {
                _attendanceRecords.Clear();
                var today = DateTime.Today;
                var attendance = _attendanceService.GetAttendanceByDate(today);
                
                foreach (var record in attendance)
                {
                    // FIX: Use FirstOrDefault with LocalId instead of FindById
                    var student = _studentRegistry.All.FirstOrDefault(s => s.LocalId == record.StudentId);
                    if (student != null)
                    {
                        _attendanceRecords.Add(new AttendanceRecord
                        {
                            StudentName = student.FullName,
                            ClassName = student.ClassName,
                            Date = record.Date.ToString("yyyy-MM-dd"),
                            TimeIn = record.TimeIn.ToString("HH:mm:ss"),
                            Status = "Present"
                        });
                    }
                }
                
                // Add absent students
                var presentStudentIds = _attendanceService.GetTodayPresentStudentIds();
                foreach (var student in _studentRegistry.All)
                {
                    if (!presentStudentIds.Contains(student.LocalId))
                    {
                        _attendanceRecords.Add(new AttendanceRecord
                        {
                            StudentName = student.FullName,
                            ClassName = student.ClassName,
                            Date = today.ToString("yyyy-MM-dd"),
                            TimeIn = "N/A",
                            Status = "Absent"
                        });
                    }
                }
                
                // Update summary
                UpdateSummary(today);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading attendance: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummary(DateTime date)
        {
            int totalStudents = _studentRegistry.All.Count;
            int presentCount = _attendanceService.GetTodayAttendanceCount();
            int absentCount = totalStudents - presentCount;
            
            // FIX: TextBlock uses Text property, not Content
            DateLabel.Text = $"Date: {date:yyyy-MM-dd}";
            SummaryLabel.Text = $"Total: {totalStudents} | Present: {presentCount} | Absent: {absentCount}";
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            LoadTodayAttendance();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePicker.SelectedDate.HasValue)
            {
                LoadAttendanceForDate(DatePicker.SelectedDate.Value);
            }
        }

        private void LoadAttendanceForDate(DateTime date)
        {
            try
            {
                _attendanceRecords.Clear();
                var attendance = _attendanceService.GetAttendanceByDate(date);
                
                foreach (var record in attendance)
                {
                    // FIX: Use FirstOrDefault with LocalId instead of FindById
                    var student = _studentRegistry.All.FirstOrDefault(s => s.LocalId == record.StudentId);
                    if (student != null)
                    {
                        _attendanceRecords.Add(new AttendanceRecord
                        {
                            StudentName = student.FullName,
                            ClassName = student.ClassName,
                            Date = record.Date.ToString("yyyy-MM-dd"),
                            TimeIn = record.TimeIn.ToString("HH:mm:ss"),
                            Status = "Present"
                        });
                    }
                }
                
                UpdateSummary(date);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading attendance: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var date = DatePicker.SelectedDate ?? DateTime.Today;
                var dateStr = date.ToString("yyyy-MM-dd");
                var fileName = $"Attendance_{dateStr}.csv";
                
                using var writer = new System.IO.StreamWriter(fileName);
                writer.WriteLine("Student Name,Class,Date,Time In,Status");
                
                foreach (var record in _attendanceRecords)
                {
                    writer.WriteLine($"\"{record.StudentName}\",{record.ClassName},{record.Date},{record.TimeIn},{record.Status}");
                }
                
                writer.Close();
                MessageBox.Show($"Attendance report exported to: {fileName}", "Export Complete", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
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