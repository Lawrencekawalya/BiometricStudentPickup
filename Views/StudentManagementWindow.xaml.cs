// using System.Collections.ObjectModel;
// using System.Linq;
// using System.Windows;
// using System.Windows.Controls;
// using BiometricStudentPickup.Models;

// namespace BiometricStudentPickup.Views
// {
//     public partial class StudentManagementWindow : Window
//     {
//         private StudentRowViewModel? GetSelected(object sender)
//         {
//             return ((FrameworkElement)sender).DataContext as StudentRowViewModel;
//         }


//         public StudentManagementWindow()
//         {
//             InitializeComponent();
//             LoadStudents();
//         }

//         private void LoadStudents()
//         {
//             // This will later come from DatabaseService
//             _students = StudentRepository.GetAllStudentsWithGuardians();
//             _filtered = new ObservableCollection<StudentRowViewModel>(_students);

//             StudentsGrid.ItemsSource = _filtered;
//             StatusText.Text = $"{_filtered.Count} students loaded";
//         }

//         private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
//         {
//             var query = SearchBox.Text.ToLower();

//             _filtered.Clear();

//             foreach (var s in _students.Where(x =>
//                          x.StudentName.ToLower().Contains(query) ||
//                          x.ClassName.ToLower().Contains(query)))
//             {
//                 _filtered.Add(s);
//             }

//             StatusText.Text = $"{_filtered.Count} matching students";
//         }

//         private StudentRowViewModel GetSelected(object sender)
//         {
//             return ((FrameworkElement)sender).DataContext as StudentRowViewModel;
//         }

//         private void View_Click(object sender, RoutedEventArgs e)
//         {
//             var student = GetSelected(sender);
//             if (student == null) return;

//             new ViewStudentWindow(student.StudentId).ShowDialog();
//         }

//         private void Edit_Click(object sender, RoutedEventArgs e)
//         {
//             var student = GetSelected(sender);
//             if (student == null) return;

//             var dialog = new EditStudentWindow(student.StudentId);
//             if (dialog.ShowDialog() == true)
//             {
//                 LoadStudents();
//             }
//         }

//         private void Delete_Click(object sender, RoutedEventArgs e)
//         {
//             var student = GetSelected(sender);
//             if (student == null) return;

//             var confirm = MessageBox.Show(
//                 $"Delete {student.StudentName}?\n\nThis will remove pickup history and guardian links.",
//                 "Confirm Deletion",
//                 MessageBoxButton.YesNo,
//                 MessageBoxImage.Warning);

//             if (confirm != MessageBoxResult.Yes) return;

//             StudentRepository.DeleteStudent(student.StudentId);

//             LoadStudents();
//         }

//         private void AddStudent_Click(object sender, RoutedEventArgs e)
//         {
//             var dialog = new AddStudentWindow();
//             if (dialog.ShowDialog() == true)
//             {
//                 LoadStudents();
//             }
//         }
//     }
// }

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BiometricStudentPickup.Models;

namespace BiometricStudentPickup.Views
{
    public partial class StudentManagementWindow : Window
    {
        private ObservableCollection<StudentRowViewModel> _students = new();
        private ObservableCollection<StudentRowViewModel> _filtered = new();

        public StudentManagementWindow()
        {
            InitializeComponent();
            LoadStudents();
        }

        private void LoadStudents()
        {
            _students.Clear();

            // Test data
            _students.Add(new StudentRowViewModel
            {
                StudentId = 1,
                StudentName = "Test Student",
                ClassName = "P3",
                FingerprintId = 101,
                Guardians = "John Doe"
            });

            _students.Add(new StudentRowViewModel
            {
                StudentId = 2,
                StudentName = "Jane Smith",
                ClassName = "P4",
                FingerprintId = 102,
                Guardians = "Mary Smith"
            });

            _students.Add(new StudentRowViewModel
            {
                StudentId = 3,
                StudentName = "Bob Johnson",
                ClassName = "P5",
                FingerprintId = 103,
                Guardians = "Sarah Johnson, Mike Johnson"
            });

            _filtered = new ObservableCollection<StudentRowViewModel>(_students);

            StudentsGrid.ItemsSource = _filtered;
            StatusText.Text = $"✅ Ready - {_filtered.Count} students loaded";

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

            MessageBox.Show($"Editing student: {student.StudentName}\n" +
                           "Edit feature coming soon!",
                           "Edit Student",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);
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
        }

        // ===== TOOLBAR BUTTON HANDLERS =====
        private void AddStudent_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Student feature coming soon!",
                           "Info",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);
        }

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