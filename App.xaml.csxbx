// // using System.Windows;
// // using BiometricStudentPickup.Services;

// // namespace BiometricStudentPickup
// // {
// //     public partial class App : Application
// //     {
// //         protected override void OnStartup(StartupEventArgs e)
// //         {
// //             base.OnStartup(e);

// //             // Single shared registries (central in-memory state)
// //             var studentRegistry = new StudentRegistry();
// //             var guardianRegistry = new GuardianRegistry();
// //             var guardianStudentRegistry = new GuardianStudentRegistry();

// //             var mainWindow = new MainWindow(
// //                 studentRegistry,
// //                 guardianRegistry,
// //                 guardianStudentRegistry
// //             );

// //             mainWindow.Show();
// //         }
// //     }
// // }

// // using System.Windows;
// // using BiometricStudentPickup.Services;

// // namespace BiometricStudentPickup
// // {
// //     public partial class App : Application
// //     {
// //         protected override void OnStartup(StartupEventArgs e)
// //         {
// //             base.OnStartup(e);

// //             // SINGLE database instance (source of truth)
// //             var databaseService = new DatabaseService();
// //             var auditLogService = new AuditLogService(databaseService);

// //             // Registries backed by SQLite
// //             var studentRegistry = new StudentRegistry(databaseService);
// //             var guardianRegistry = new GuardianRegistry(databaseService);
// //             var guardianStudentRegistry = new GuardianStudentRegistry(databaseService);
// //             // var databaseService = new DatabaseService();

// //             var mainWindow = new MainWindow(
// //                 studentRegistry,
// //                 guardianRegistry,
// //                 guardianStudentRegistry,
// //                 auditLogService
// //             );

// //             mainWindow.Show();
// //         }
// //     }
// // }

// using System;
// using System.Windows;
// using BiometricStudentPickup.Services;

// namespace BiometricStudentPickup
// {
//     public partial class App : Application
//     {
//         protected override void OnStartup(StartupEventArgs e)
//         {
//             try
//             {
//                 base.OnStartup(e);

//                 var databaseService = new DatabaseService();
//                 var auditLogService = new AuditLogService(databaseService);

//                 var studentRegistry = new StudentRegistry(databaseService);
//                 var guardianRegistry = new GuardianRegistry(databaseService);
//                 var guardianStudentRegistry = new GuardianStudentRegistry(databaseService);
//                 // Create attendance service
//                 var attendanceService = new AttendanceService(databaseService, auditLogService);
//                 var pickupLogService = new PickupLogService(connectionString);

//                 var mainWindow = new MainWindow(
//                     studentRegistry,
//                     guardianRegistry,
//                     guardianStudentRegistry,
//                     auditLogService,
//                     databaseService,
//                     attendanceService,
//                     pickupLogService
//                 );

//                 mainWindow.Show();
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show(
//                     ex.ToString(),
//                     "Application startup failed",
//                     MessageBoxButton.OK,
//                     MessageBoxImage.Error
//                 );

//                 Shutdown();
//             }
//         }
//     }
// }

using System;
using System.Windows;
using BiometricStudentPickup.Services;

namespace BiometricStudentPickup
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                var databaseService = new DatabaseService();
                var auditLogService = new AuditLogService(databaseService);

                var studentRegistry = new StudentRegistry(databaseService);
                var guardianRegistry = new GuardianRegistry(databaseService);
                var guardianStudentRegistry = new GuardianStudentRegistry(databaseService);
                
                // Create attendance service
                var attendanceService = new AttendanceService(databaseService, auditLogService);
                
                // FIXED: Create PickupLogService with correct parameters
                var pickupLogService = new PickupLogService(databaseService, auditLogService);

                var mainWindow = new MainWindow(
                    studentRegistry,
                    guardianRegistry,
                    guardianStudentRegistry,
                    auditLogService,
                    databaseService,
                    attendanceService,
                    pickupLogService
                );

                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Application startup failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown();
            }
        }
    }
}