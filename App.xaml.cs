using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using BiometricStudentPickup.Services;

namespace BiometricStudentPickup
{
    public partial class App : Application
    {
        public App()
        {
            // Catch exceptions that happen before OnStartup
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show($"Unhandled exception: {ex?.Message}",
                    "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"CurrentDomain UnhandledException: {ex}");
            };
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            LoadMaterialDesignV5();
            try
            {
                Debug.WriteLine("=== APPLICATION STARTING ===");

                // 1. Create DatabaseService - constructor initializes database automatically
                var databaseService = new DatabaseService();
                Debug.WriteLine("Created DatabaseService (database initialized automatically)");

                // 2. Create audit log service
                var auditLogService = new AuditLogService(databaseService);
                Debug.WriteLine("Created AuditLogService");

                // 3. Create registries
                Debug.WriteLine("Creating registries...");
                var studentRegistry = new StudentRegistry(databaseService);
                Debug.WriteLine($"StudentRegistry created with {studentRegistry.All.Count} students");

                var guardianRegistry = new GuardianRegistry(databaseService);
                Debug.WriteLine($"GuardianRegistry created with {guardianRegistry.All.Count} guardians");

                var guardianStudentRegistry = new GuardianStudentRegistry(databaseService);
                Debug.WriteLine("GuardianStudentRegistry created");

                // 4. Create other services
                var attendanceService = new AttendanceService(databaseService, auditLogService);
                Debug.WriteLine("Created AttendanceService");

                var pickupLogService = new PickupLogService(databaseService, auditLogService);
                Debug.WriteLine("Created PickupLogService");

                // 5. Create and show main window
                Debug.WriteLine("Creating MainWindow...");
                var mainWindow = new MainWindow(
                    studentRegistry,
                    guardianRegistry,
                    guardianStudentRegistry,
                    auditLogService,
                    databaseService,
                    attendanceService,
                    pickupLogService
                );

                Debug.WriteLine("Showing MainWindow...");
                mainWindow.Show();
                Debug.WriteLine("=== APPLICATION STARTED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== STARTUP FAILED: {ex.Message} ===");
                Debug.WriteLine(ex.StackTrace);

                MessageBox.Show(
                    $"Application startup failed:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown();
            }
        }

        private void LoadMaterialDesignV5()
        {
            try
            {
                Debug.WriteLine("=== Loading Material Design 5.x ===");

                var resources = new ResourceDictionary();

                // Try loading resources for version 5.x
                string[] resourcePaths = {
            "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml",
            "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml",
            "pack://application:,,,/MaterialDesignColors;component/Themes/MaterialDesignColor.Blue.xaml",
            "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Fonts.xaml"
        };

                foreach (var path in resourcePaths)
                {
                    try
                    {
                        var dict = new ResourceDictionary { Source = new Uri(path, UriKind.Absolute) };
                        resources.MergedDictionaries.Add(dict);
                        Debug.WriteLine($"✓ Loaded: {path}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"✗ Failed: {path}");
                        Debug.WriteLine($"  Error: {ex.Message}");
                    }
                }

                this.Resources.MergedDictionaries.Add(resources);
                Debug.WriteLine("Material Design resources loaded");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load Material Design: {ex.Message}");
            }
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine($"=== DISPATCHER EXCEPTION: {e.Exception.Message} ===");
            Debug.WriteLine(e.Exception.StackTrace);

            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}",
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            e.Handled = true;
        }
    }
}