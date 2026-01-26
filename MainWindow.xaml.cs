using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

using BiometricStudentPickup.Services;
using BiometricStudentPickup.Models;
using BiometricStudentPickup.Views;

namespace BiometricStudentPickup
{
    public partial class MainWindow : Window
    {
        private readonly PickupLogService _pickupLogService;
        private readonly AttendanceService _attendanceService;
        private readonly DatabaseService _databaseService;
        private readonly AdminSecurityService _adminSecurity;
        private readonly AuditLogService _auditLogService;
        private readonly StudentRegistry _studentRegistry;
        private readonly GuardianRegistry _guardianRegistry;
        private readonly GuardianStudentRegistry _guardianStudentRegistry;

        private readonly QueueService _queueService = new();
        private readonly VoiceService _voiceService = new();
        public QueueService QueueService => _queueService;

        private FingerprintService? _fingerprintService;
        private DispatcherTimer? _adminSessionTimer;
        private PickupQueue? _currentPickup;
        private DispatcherTimer? _scanTimer;
        private bool _isScanning = false;


        private const int CALL_TIMEOUT_SECONDS = 15;

        public MainWindow(
            StudentRegistry studentRegistry,
            GuardianRegistry guardianRegistry,
            GuardianStudentRegistry guardianStudentRegistry,
            AuditLogService auditLogService,
            DatabaseService databaseService,
            AttendanceService attendanceService,
            PickupLogService pickupLogService)
        {
            Debug.WriteLine("=== MainWindow START ===");

            InitializeComponent();

            _studentRegistry = studentRegistry;
            _guardianRegistry = guardianRegistry;
            _guardianStudentRegistry = guardianStudentRegistry;
            _auditLogService = auditLogService;
            _databaseService = databaseService;
            _attendanceService = attendanceService;
            _pickupLogService = pickupLogService;
            DataContext = this;
            _adminSecurity = new AdminSecurityService(auditLogService);

            PickupQueueList.ItemsSource = _queueService.Queue;


            // StartCallingLoop();
            StartContinuousScanning();

            Loaded += MainWindow_Loaded;

            Debug.WriteLine("=== MainWindow READY ===");
        }

        ///////////////////////////////////////////////////////////////////////////
        // private void StartCallingLoop()
        // {
        //     _callTimer = new DispatcherTimer
        //     {
        //         Interval = TimeSpan.FromSeconds(10)
        //     };

        //     _callTimer.Tick += (s, e) =>
        //     {
        //         if (_currentPickup == null)
        //         {
        //             _currentPickup = _queueService.GetNext();
        //             if (_currentPickup == null) return;

        //             _currentPickup.CalledAt = DateTime.Now;
        //             _currentPickup.Status = "Calling";

        //             NowCallingName.Text = _currentPickup.StudentName;
        //             NowCallingClass.Text = _currentPickup.ClassName;

        //             _voiceService.Speak(
        //                 $"Pickup request for {_currentPickup.StudentName} from {_currentPickup.ClassName}"
        //             );
        //             return;
        //         }

        //         if (_currentPickup.CalledAt.HasValue &&
        //             (DateTime.Now - _currentPickup.CalledAt.Value).TotalSeconds >= CALL_TIMEOUT_SECONDS)
        //         {
        //             _voiceService.Speak($"{_currentPickup.StudentName} did not respond");

        //             _queueService.RequeuePickup(_currentPickup);
        //             _currentPickup = null;

        //             NowCallingName.Text = "Waiting for next student...";
        //             NowCallingClass.Text = "";
        //         }
        //     };

        //     _callTimer.Start();
        // }
        ///////////////////////////////////////////////////////////////////////////
        private void StartContinuousScanning()
        {
            _scanTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Scan every second
            };

            _scanTimer.Tick += async (s, e) => await ContinuousScanAsync();
            _scanTimer.Start();
        }

        private async Task ContinuousScanAsync()
        {
            // Prevent overlapping scans
            if (_isScanning || _fingerprintService == null)
                return;

            _isScanning = true;

            try
            {
                int? fingerprintId = await _fingerprintService.VerifyAsync();

                if (fingerprintId == null)
                {
                    // No fingerprint detected, just return
                    return;
                }

                // Process the detected fingerprint
                ProcessScannedFingerprint(fingerprintId.Value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Scan error: {ex.Message}");
            }
            finally
            {
                _isScanning = false;
            }
        }

        // private async Task ProcessScannedFingerprint(int fingerprintId)
        private void ProcessScannedFingerprint(int fingerprintId)
        {
            Debug.WriteLine($"=== Fingerprint detected: {fingerprintId} ===");

            // 1. Check if it's a Guardian
            var guardian = _guardianRegistry.FindByFingerprint(fingerprintId);
            if (guardian != null)
            {
                ProcessGuardianScan(guardian);
                return;
            }

            // 2. Check if it's a Student
            var student = _studentRegistry.FindByFingerprint(fingerprintId);
            if (student != null)
            {
                ProcessStudentScan(student);
                return;
            }

            // 3. Unknown fingerprint
            try
            {
                _voiceService.Speak("Fingerprint not recognized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Voice announcement failed: {ex.Message}");
            }
            Debug.WriteLine("Fingerprint not found in registry");
        }
        ///////////////////////////////////////////////////////////////////////////
        // private async Task ProcessGuardianScan(Guardian guardian)
        private void ProcessGuardianScan(Guardian guardian)
        {
            Debug.WriteLine($"Guardian scanned: {guardian.FullName}");
            _voiceService.Speak($"Welcome {guardian.FullName}");

            // Log guardian scan using audit log instead
            _auditLogService.Log(
                "GUARDIAN_SCANNED",
                $"Guardian scanned: {guardian.FullName} (ID: {guardian.LocalId})",
                guardianId: guardian.LocalId
            );

            // Get all students associated with this guardian
            var studentIds = _guardianStudentRegistry.GetStudentsForGuardian(guardian.LocalId);

            int addedCount = 0;
            foreach (var student in _studentRegistry.All.Where(s => studentIds.Contains(s.LocalId)))
            {
                // Check if student is already in queue
                if (_queueService.Queue.Any(q => q.StudentId == student.LocalId))
                {
                    Debug.WriteLine($"Student {student.FullName} is already in queue");
                    continue;
                }

                // Log pickup request
                _pickupLogService.LogPickupRequest(student.LocalId, guardian.LocalId);

                // Add to queue with guardian ID
                _queueService.AddPickup(student.LocalId, student.FullName, student.ClassName, guardian.LocalId);
                addedCount++;

                Debug.WriteLine($"Added {student.FullName} to pickup queue");
            }

            // Announce result
            if (addedCount > 0)
            {
                _voiceService.Speak($"Added {addedCount} student{(addedCount > 1 ? "s" : "")} to pickup queue");

                // If no student is currently being called, call the first one immediately
                if (_currentPickup == null)
                {
                    CallNextStudentImmediately();
                }
            }
            else
            {
                _voiceService.Speak("No students to pickup or all are already in queue");
            }
        }
        // private async Task ProcessStudentScan(Student student)
        private void ProcessStudentScan(Student student)
        {
            Debug.WriteLine($"Student scanned: {student.FullName}");

            // ✅ ATTENDANCE CHECK: Record attendance (only if not already marked today)
            bool attendanceRecorded = _attendanceService.RecordAttendance(student.LocalId, student.FullName);

            if (attendanceRecorded)
            {
                _voiceService.Speak($"Welcome {student.FullName}. Attendance recorded.");
            }
            else
            {
                _voiceService.Speak($"Welcome back {student.FullName}.");
            }

            // ✅ Check if student is in queue (Step 3: Remove waiting requirement)
            var pickupInQueue = _queueService.Queue.FirstOrDefault(q => q.StudentId == student.LocalId);

            if (pickupInQueue != null)
            {
                // Complete pickup immediately - no need to wait for "called" status
                Debug.WriteLine($"Student {student.FullName} is in queue, confirming pickup...");

                // Log successful pickup completion
                _pickupLogService.LogPickupCompletion(student.LocalId, pickupInQueue.GuardianId);

                // Complete the pickup
                _queueService.CompletePickup(pickupInQueue);
                _voiceService.Speak($"Pickup confirmed for {student.FullName}");

                // If this was the currently called student, clear it
                if (_currentPickup?.StudentId == student.LocalId)
                {
                    _currentPickup = null;
                    NowCallingName.Text = "Waiting for next student...";
                    NowCallingClass.Text = "";
                }

                // Call next student immediately
                CallNextStudentImmediately();
            }
            else
            {
                Debug.WriteLine($"Student {student.FullName} is not in pickup queue");

                // Optional: Ask if guardian is here for pickup
                // _voiceService.Speak($"{student.FullName}, please ask your guardian to scan");
            }
        }
        // private async Task CallNextStudentImmediately()
        // {
        //     if (_currentPickup != null)
        //     {
        //         // A student is already being called, don't interrupt
        //         return;
        //     }

        //     _currentPickup = _queueService.GetNext();
        //     if (_currentPickup == null)
        //     {
        //         // No students in queue
        //         NowCallingName.Text = "No students in queue";
        //         NowCallingClass.Text = "";
        //         return;
        //     }

        //     _currentPickup.CalledAt = DateTime.Now;
        //     _currentPickup.Status = "Calling";

        //     NowCallingName.Text = _currentPickup.StudentName;
        //     NowCallingClass.Text = _currentPickup.ClassName;

        //     // Announce the call
        //     _voiceService.Speak(
        //         $"Pickup request for {_currentPickup.StudentName} from {_currentPickup.ClassName}"
        //     );

        //     // Start timeout timer for this specific student
        //     StartStudentTimeoutTimer(_currentPickup);
        // }
        /////////////////////////////////////////////////////////////////////////////////////
        private void CallNextStudentImmediately()
        {
            if (_currentPickup != null)
            {
                // A student is already being called, don't interrupt
                return;
            }

            _currentPickup = _queueService.GetNext();
            if (_currentPickup == null)
            {
                // No students in queue
                NowCallingName.Text = "No students in queue";
                NowCallingClass.Text = "";
                return;
            }

            _currentPickup.CalledAt = DateTime.Now;
            _currentPickup.Status = "Calling";

            NowCallingName.Text = _currentPickup.StudentName;
            NowCallingClass.Text = _currentPickup.ClassName;

            // Announce the call
            _voiceService.Speak(
                $"Pickup request for {_currentPickup.StudentName} from {_currentPickup.ClassName}"
            );

            // Start timeout timer for this specific student
            StartStudentTimeoutTimer(_currentPickup);
        }
        /////////////////////////////////////////////////////////////////////////////////////
        private void StartStudentTimeoutTimer(PickupQueue pickup)
        {
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(CALL_TIMEOUT_SECONDS)
            };

            timer.Tick += (s, e) =>
            {
                timer.Stop();

                if (_currentPickup?.StudentId == pickup.StudentId && pickup.Status == "Calling")
                {
                    // Timeout - student didn't confirm
                    _voiceService.Speak($"{pickup.StudentName} did not respond");

                    // Log timeout
                    _pickupLogService.LogPickupTimeout(pickup.StudentId);

                    // Requeue for later
                    _queueService.RequeuePickup(pickup);

                    if (_currentPickup?.StudentId == pickup.StudentId)
                    {
                        _currentPickup = null;
                    }

                    // Call next student
                    // Dispatcher.BeginInvoke(new Action(async () =>
                    // {
                    //     CallNextStudentImmediately();
                    // }));
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        CallNextStudentImmediately();
                    }));
                }
            };

            timer.Start();
        }
        ///////////////////////////////////////////////////////////////////////////
        // private void StartCallingLoop()
        // {
        //     _callTimer = new DispatcherTimer
        //     {
        //         Interval = TimeSpan.FromSeconds(10)
        //     };

        //     _callTimer.Tick += (s, e) =>
        //     {
        //         if (_currentPickup == null)
        //         {
        //             _currentPickup = _queueService.GetNext();
        //             if (_currentPickup == null) return;

        //             _currentPickup.CalledAt = DateTime.Now;
        //             _currentPickup.Status = "Calling";

        //             NowCallingName.Text = _currentPickup.StudentName;
        //             NowCallingClass.Text = _currentPickup.ClassName;

        //             _voiceService.Speak(
        //                 $"Pickup request for {_currentPickup.StudentName} from {_currentPickup.ClassName}"
        //             );
        //             return;
        //         }

        //         if (_currentPickup.CalledAt.HasValue &&
        //             (DateTime.Now - _currentPickup.CalledAt.Value).TotalSeconds >= CALL_TIMEOUT_SECONDS)
        //         {
        //             _voiceService.Speak($"{_currentPickup.StudentName} did not respond");

        //             // Log the timeout event
        //             _pickupLogService.LogPickupTimeout(_currentPickup.StudentId);

        //             _queueService.RequeuePickup(_currentPickup);
        //             _currentPickup = null;

        //             NowCallingName.Text = "Waiting for next student...";
        //             NowCallingClass.Text = "";
        //         }
        //     };

        //     _callTimer.Start();
        // }
        ///////////////////////////////////////////////////////////////////////////

        // private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        // {
        //     try
        //     {
        //         // Run hardware init off the UI thread
        //         _fingerprintService = await Task.Run(() =>
        //         {
        //             return new FingerprintService();
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         MessageBox.Show(
        //             ex.Message,
        //             "Fingerprint initialization failed",
        //             MessageBoxButton.OK,
        //             MessageBoxImage.Warning
        //         );
        //     }

        //     StartAdminSessionMonitor();
        //     UpdateEnrollmentLockUI();
        // }

        // private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        // {
        //     try
        //     {
        //         // 1️⃣ Initialize fingerprint service first
        //         _fingerprintService = await Task.Run(() =>
        //         {
        //             return new FingerprintService();
        //         });

        //         // 2️⃣ Clear SDK in-memory DB
        //         _fingerprintService.ClearDb();

        //         // 3️⃣ Reload all templates into device memory
        //         await Task.Run(() =>
        //         {
        //             foreach (var student in _studentRegistry.All)
        //             {
        //                 _fingerprintService.UploadTemplate(
        //                     student.FingerprintId,
        //                     student.FingerprintTemplate
        //                 );
        //             }

        //             foreach (var guardian in _guardianRegistry.All)
        //             {
        //                 _fingerprintService.UploadTemplate(
        //                     guardian.FingerprintId,
        //                     guardian.FingerprintTemplate
        //                 );
        //             }
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         MessageBox.Show(
        //             ex.Message,
        //             "Fingerprint initialization failed",
        //             MessageBoxButton.OK,
        //             MessageBoxImage.Warning
        //         );
        //     }

        //     StartAdminSessionMonitor();
        //     UpdateEnrollmentLockUI();
        // }

        // private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        // {
        //     try
        //     {
        //         // 1️⃣ Initialize fingerprint service FIRST
        //         _fingerprintService = await Task.Run(() =>
        //         {
        //             return new FingerprintService();
        //         });

        //         // 2️⃣ Clear device DB to avoid duplicates
        //         _fingerprintService.ClearDeviceDatabase();

        //         // 3️⃣ Reload templates from SQLite
        //         await Task.Run(() =>
        //         {
        //             foreach (var student in _studentRegistry.All)
        //             {
        //                 _fingerprintService.UploadTemplate(
        //                     student.FingerprintId,
        //                     student.FingerprintTemplate
        //                 );
        //             }

        //             foreach (var guardian in _guardianRegistry.All)
        //             {
        //                 _fingerprintService.UploadTemplate(
        //                     guardian.FingerprintId,
        //                     guardian.FingerprintTemplate
        //                 );
        //             }
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         MessageBox.Show(
        //             ex.Message,
        //             "Fingerprint initialization failed",
        //             MessageBoxButton.OK,
        //             MessageBoxImage.Warning
        //         );
        //     }

        //     StartAdminSessionMonitor();
        //     UpdateEnrollmentLockUI();
        // }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _fingerprintService = await Task.Run(() =>
                {
                    return new FingerprintService();
                });

                // 1. Clear device DB
                _fingerprintService.ClearDeviceDatabase();

                // 2. Reload ALL biometrics in strict ID order
                await Task.Run(() =>
                {
                    var allBiometrics = _studentRegistry.All
                        .Select(s => new { s.FingerprintId, s.FingerprintTemplate })
                        .Concat(
                            _guardianRegistry.All.Select(g => new { g.FingerprintId, g.FingerprintTemplate })
                        )
                        .OrderBy(x => x.FingerprintId)
                        .ToList();

                    foreach (var bio in allBiometrics)
                    {
                        _fingerprintService.UploadTemplate(
                            bio.FingerprintId,
                            bio.FingerprintTemplate
                        );
                    }
                });
                // 3. Cleanup old audit logs (run once on startup)
                _auditLogService.CleanupOldLogs(daysToKeep: 180); // Keep 6 months of logs
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Fingerprint initialization failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            StartAdminSessionMonitor();
            UpdateEnrollmentLockUI();
        }
        ///////////////////////////////////////////////////////////////////
        // private async Task RefreshRegistriesAndDeviceDB()
        // {
        //     try
        //     {
        //         // Refresh ALL registries from database
        //         _studentRegistry.Refresh();
        //         _guardianRegistry.Refresh();
        //         _guardianStudentRegistry.Refresh(); // Add this!

        //         // Clear and reload device DB
        //         if (_fingerprintService != null)
        //         {
        //             _fingerprintService.ClearDeviceDatabase();

        //             var allBiometrics = _studentRegistry.All
        //                 .Select(s => new { s.FingerprintId, s.FingerprintTemplate })
        //                 .Concat(
        //                     _guardianRegistry.All.Select(g => new { g.FingerprintId, g.FingerprintTemplate })
        //                 )
        //                 .OrderBy(x => x.FingerprintId)
        //                 .ToList();

        //             foreach (var bio in allBiometrics)
        //             {
        //                 _fingerprintService.UploadTemplate(
        //                     bio.FingerprintId,
        //                     bio.FingerprintTemplate
        //                 );
        //             }

        //             // Add debug output
        //             Debug.WriteLine($"Refreshed device DB with {allBiometrics.Count} templates");
        //             Debug.WriteLine($"Students: {_studentRegistry.All.Count}, Guardians: {_guardianRegistry.All.Count}");

        //             MessageBox.Show($"Device database refreshed with {allBiometrics.Count} templates.", 
        //                 "Refresh Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.WriteLine($"Refresh failed: {ex.Message}");
        //         MessageBox.Show(
        //             $"Failed to refresh device database:\n{ex.Message}",
        //             "Refresh Error",
        //             MessageBoxButton.OK,
        //             MessageBoxImage.Warning
        //         );
        //     }
        // }
        /////////////////////////////////////////////////////////////////////////////
        // private async Task RefreshRegistriesAndDeviceDB()
        // {
        //     try
        //     {
        //         // Refresh registries from database
        //         _studentRegistry.Refresh();
        //         _guardianRegistry.Refresh();

        //         // Clear and reload device DB asynchronously
        //         if (_fingerprintService != null)
        //         {
        //             await Task.Run(() =>
        //             {
        //                 _fingerprintService.ClearDeviceDatabase();

        //                 var allBiometrics = _studentRegistry.All
        //                     .Select(s => new { s.FingerprintId, s.FingerprintTemplate })
        //                     .Concat(
        //                         _guardianRegistry.All.Select(g => new { g.FingerprintId, g.FingerprintTemplate })
        //                     )
        //                     .OrderBy(x => x.FingerprintId)
        //                     .ToList();

        //                 foreach (var bio in allBiometrics)
        //                 {
        //                     _fingerprintService.UploadTemplate(
        //                         bio.FingerprintId,
        //                         bio.FingerprintTemplate
        //                     );
        //                 }
        //             });

        //             // MessageBox.Show("Device database refreshed with latest enrollments.", 
        //             //     "Refresh Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        //             MessageBox.Show($"Device database refreshed with {allBiometrics.Count} templates.", 
        //                 "Refresh Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         MessageBox.Show(
        //             $"Failed to refresh device database:\n{ex.Message}",
        //             "Refresh Error",
        //             MessageBoxButton.OK,
        //             MessageBoxImage.Warning
        //         );
        //     }
        // }
        /////////////////////////////////////////////////////////////////////////////
        private async Task RefreshRegistriesAndDeviceDB()
        {
            try
            {
                // Refresh registries from database
                _studentRegistry.Refresh();
                _guardianRegistry.Refresh();

                // Get counts before async operation
                int studentCount = _studentRegistry.All.Count;
                int guardianCount = _guardianRegistry.All.Count;
                int totalTemplates = studentCount + guardianCount;

                // Clear and reload device DB asynchronously
                if (_fingerprintService != null)
                {
                    int uploadedCount = 0;

                    await Task.Run(() =>
                    {
                        _fingerprintService.ClearDeviceDatabase();

                        var allBiometrics = _studentRegistry.All
                            .Select(s => new
                            {
                                s.FingerprintId,
                                s.FingerprintTemplate,
                                Type = "Student",
                                Name = s.FullName
                            })
                            .Concat(
                                _guardianRegistry.All.Select(g => new
                                {
                                    g.FingerprintId,
                                    g.FingerprintTemplate,
                                    Type = "Guardian",
                                    Name = g.FullName
                                })
                            )
                            .OrderBy(x => x.FingerprintId)
                            .ToList();

                        uploadedCount = allBiometrics.Count;

                        foreach (var bio in allBiometrics)
                        {
                            _fingerprintService.UploadTemplate(
                                bio.FingerprintId,
                                bio.FingerprintTemplate
                            );
                        }
                    });

                    if (uploadedCount == totalTemplates)
                    {
                        MessageBox.Show($"Device database refreshed successfully!\n\n" +
                                    $"Total templates: {uploadedCount}\n" +
                                    $"Students: {studentCount}\n" +
                                    $"Guardians: {guardianCount}",
                            "Refresh Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Warning: Expected {totalTemplates} templates but uploaded {uploadedCount}.\n\n" +
                                    $"Students in registry: {studentCount}\n" +
                                    $"Guardians in registry: {guardianCount}",
                            "Refresh Issue",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Fingerprint service is not available.",
                        "Service Not Ready",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to refresh device database:\n\n{ex.Message}",
                    "Refresh Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
        /////////////////////////////////////////////////////////////////////////////
        // private async void ScanGuardian_Click(object sender, RoutedEventArgs e)
        // {
        //     _voiceService.Speak("Scanning guardian fingerprint");

        //     if (_fingerprintService == null)
        //     {
        //         _voiceService.Speak("Fingerprint device not ready");
        //         return;
        //     }

        //     int? fingerprintId = await _fingerprintService.VerifyAsync();
        //     if (fingerprintId == null)
        //     {
        //         _voiceService.Speak("Guardian not recognized");
        //         return;
        //     }

        //     var guardian = _guardianRegistry.FindByFingerprint(fingerprintId.Value);
        //     if (guardian == null)
        //     {
        //         _voiceService.Speak("Guardian not recognized");
        //         return;
        //     }

        //     var studentIds = _guardianStudentRegistry.GetStudentsForGuardian(guardian.LocalId);

        //     foreach (var student in _studentRegistry.All.Where(s => studentIds.Contains(s.LocalId)))
        //     {
        //         if (_queueService.Queue.Any(q => q.StudentId == student.LocalId))
        //             continue;

        //         _queueService.AddPickup(student.LocalId, student.FullName, student.ClassName);
        //     }

        //     _voiceService.Speak("Pickup request registered");
        // }
        // private async void ScanGuardian_Click(object sender, RoutedEventArgs e)
        // {
        //     _voiceService.Speak("Scanning guardian fingerprint");

        //     if (_fingerprintService == null)
        //     {
        //         _voiceService.Speak("Fingerprint device not ready");
        //         return;
        //     }

        //     int? fingerprintId = await _fingerprintService.VerifyAsync();
        //     if (fingerprintId == null)
        //     {
        //         _voiceService.Speak("Guardian not recognized");
        //         return;
        //     }

        //     var guardian = _guardianRegistry.FindByFingerprint(fingerprintId.Value);
        //     if (guardian == null)
        //     {
        //         _voiceService.Speak("Guardian not recognized");
        //         return;
        //     }

        //     // Log the guardian scan event
        //     _pickupLogService.LogGuardianScan(
        //         guardianId: guardian.LocalId,
        //         guardianName: guardian.FullName
        //     );
        //     _pickupLogService.LogEvent(new PickupLogEntry(
        //         eventType: "GuardianScan",
        //         guardianId: guardian.LocalId,
        //         guardianName: guardian.FullName,
        //         studentId: student.LocalId,
        //         studentName: student.FullName,
        //         className: student.ClassName
        //     ));

        //     var studentIds = _guardianStudentRegistry.GetStudentsForGuardian(guardian.LocalId);

        //     foreach (var student in _studentRegistry.All.Where(s => studentIds.Contains(s.LocalId)))
        //     {
        //         if (_queueService.Queue.Any(q => q.StudentId == student.LocalId))
        //             continue;

        //         _queueService.AddPickup(student.LocalId, student.FullName, student.ClassName);
        //     }

        //     _voiceService.Speak("Pickup request registered");
        // }
        // private async void ScanGuardian_Click(object sender, RoutedEventArgs e)
        // {
        //     _voiceService.Speak("Scanning guardian fingerprint");

        //     if (_fingerprintService == null)
        //     {
        //         _voiceService.Speak("Fingerprint device not ready");
        //         return;
        //     }

        //     int? fingerprintId = await _fingerprintService.VerifyAsync();
        //     if (fingerprintId == null)
        //     {
        //         _voiceService.Speak("Guardian not recognized");
        //         return;
        //     }

        //     var guardian = _guardianRegistry.FindByFingerprint(fingerprintId.Value);
        //     if (guardian == null)
        //     {
        //         _voiceService.Speak("Guardian not recognized");
        //         return;
        //     }

        //     var studentIds = _guardianStudentRegistry.GetStudentsForGuardian(guardian.LocalId);

        //     foreach (var student in _studentRegistry.All.Where(s => studentIds.Contains(s.LocalId)))
        //     {
        //         if (_queueService.Queue.Any(q => q.StudentId == student.LocalId))
        //             continue;

        //         // Log the pickup request
        //         _pickupLogService.LogPickupRequest(student.LocalId, guardian.LocalId);
        //         // Add to queue with guardian ID
        //         _queueService.AddPickup(student.LocalId, student.FullName, student.ClassName, guardian.LocalId);
        //     }

        //     _voiceService.Speak("Pickup request registered");
        // }

        // private async void ScanStudent_Click(object sender, RoutedEventArgs e)
        // {
        //     _voiceService.Speak("Scanning student fingerprint");

        //     // int? fingerprintId = await _fingerprintService!.VerifyAsync();
        //     if (_fingerprintService == null)
        //     {
        //         _voiceService.Speak("Fingerprint device not ready");
        //         return;
        //     }

        //     int? fingerprintId = await _fingerprintService.VerifyAsync();
        //     var student = _studentRegistry.FindByFingerprint(fingerprintId.Value);
        //     // if (fingerprintId == null) return;
        //     if (student == null)
        //     {
        //         _voiceService.Speak("Student not recognized");
        //         return;
        //     }

        //     if (_currentPickup == null || _currentPickup.StudentId != fingerprintId.Value)
        //     {
        //         _voiceService.Speak("This student is not being called");
        //         return;
        //     }

        //     _queueService.CompletePickup(_currentPickup);
        //     _voiceService.Speak($"Pickup confirmed for {_currentPickup.StudentName}");

        //     _currentPickup = null;
        //     NowCallingName.Text = "Waiting for next student...";
        //     NowCallingClass.Text = "";
        // }
        // private async void ScanStudent_Click(object sender, RoutedEventArgs e)
        // {
        //     _voiceService.Speak("Scanning student fingerprint");

        //     if (_fingerprintService == null)
        //     {
        //         _voiceService.Speak("Fingerprint device not ready");
        //         return;
        //     }

        //     int? fingerprintId = await _fingerprintService.VerifyAsync();
        //     if (fingerprintId == null)
        //     {
        //         _voiceService.Speak("Student not recognized");
        //         return;
        //     }

        //     var student = _studentRegistry.FindByFingerprint(fingerprintId.Value);
        //     if (student == null)
        //     {
        //         _voiceService.Speak("Student not recognized");
        //         return;
        //     }

        //     // FIX: Compare student.LocalId with _currentPickup.StudentId
        //     // OR compare fingerprintId with something else depending on what PickupQueue stores
        //     if (_currentPickup == null || _currentPickup.StudentId != student.LocalId)
        //     {
        //         _voiceService.Speak("This student is not being called");
        //         return;
        //     }

        //     _queueService.CompletePickup(_currentPickup);
        //     _voiceService.Speak($"Pickup confirmed for {_currentPickup.StudentName}");

        //     _currentPickup = null;
        //     NowCallingName.Text = "Waiting for next student...";
        //     NowCallingClass.Text = "";
        // }

        // private async void ScanStudent_Click(object sender, RoutedEventArgs e)
        // {
        //     Debug.WriteLine("=== ScanStudent_Click START ===");

        //     _voiceService.Speak("Scanning student fingerprint");

        //     if (_fingerprintService == null)
        //     {
        //         _voiceService.Speak("Fingerprint device not ready");
        //         Debug.WriteLine("FingerprintService is null");
        //         return;
        //     }

        //     int? fingerprintId = await _fingerprintService.VerifyAsync();
        //     Debug.WriteLine($"Device returned FingerprintId: {fingerprintId}");

        //     if (fingerprintId == null)
        //     {
        //         _voiceService.Speak("Student not recognized");
        //         Debug.WriteLine("No fingerprint matched in device database");
        //         return;
        //     }

        //     var student = _studentRegistry.FindByFingerprint(fingerprintId.Value);
        //     Debug.WriteLine($"Student found: {student?.FullName ?? "NULL"} (LocalId: {student?.LocalId}, FingerprintId: {student?.FingerprintId})");

        //     if (student == null)
        //     {
        //         _voiceService.Speak("Student not recognized");
        //         Debug.WriteLine("Student not found in StudentRegistry");

        //         // Debug: List all students in registry
        //         Debug.WriteLine("All students in registry:");
        //         foreach (var s in _studentRegistry.All)
        //         {
        //             Debug.WriteLine($"  - {s.FullName} (LocalId: {s.LocalId}, FingerprintId: {s.FingerprintId})");
        //         }
        //         return;
        //     }

        //     Debug.WriteLine($"Current pickup: {_currentPickup?.StudentName} (StudentId: {_currentPickup?.StudentId})");
        //     Debug.WriteLine($"Comparing: student.LocalId={student.LocalId} vs _currentPickup.StudentId={_currentPickup?.StudentId}");

        //     if (_currentPickup == null || _currentPickup.StudentId != student.LocalId)
        //     {
        //         _voiceService.Speak("This student is not being called");

        //         // Debug: List all pickups in queue
        //         Debug.WriteLine("All pickups in queue:");
        //         foreach (var pickup in _queueService.Queue)
        //         {
        //             Debug.WriteLine($"  - {pickup.StudentName} (StudentId: {pickup.StudentId})");
        //         }
        //         return;
        //     }

        //     _queueService.CompletePickup(_currentPickup);
        //     _voiceService.Speak($"Pickup confirmed for {_currentPickup.StudentName}");
        //     Debug.WriteLine($"Pickup confirmed for {_currentPickup.StudentName}");

        //     _currentPickup = null;
        //     NowCallingName.Text = "Waiting for next student...";
        //     NowCallingClass.Text = "";

        //     Debug.WriteLine("=== ScanStudent_Click END ===");
        // }
        ///////////////////////////////////////////////////////////////////////////
        // private async void ScanStudent_Click(object sender, RoutedEventArgs e)
        // {
        //     Debug.WriteLine("=== ScanStudent_Click START ===");

        //     _voiceService.Speak("Scanning student fingerprint");

        //     if (_fingerprintService == null)
        //     {
        //         _voiceService.Speak("Fingerprint device not ready");
        //         Debug.WriteLine("FingerprintService is null");
        //         return;
        //     }

        //     int? fingerprintId = await _fingerprintService.VerifyAsync();
        //     Debug.WriteLine($"Device returned FingerprintId: {fingerprintId}");

        //     if (fingerprintId == null)
        //     {
        //         _voiceService.Speak("Student not recognized");
        //         Debug.WriteLine("No fingerprint matched in device database");
        //         return;
        //     }

        //     var student = _studentRegistry.FindByFingerprint(fingerprintId.Value);
        //     Debug.WriteLine($"Student found: {student?.FullName ?? "NULL"} (LocalId: {student?.LocalId}, FingerprintId: {student?.FingerprintId})");

        //     if (student == null)
        //     {
        //         _voiceService.Speak("Student not recognized");
        //         Debug.WriteLine("Student not found in StudentRegistry");
        //         return;
        //     }

        //     // ✅ ATTENDANCE CHECK: Record attendance (only if not already marked today)
        //     bool attendanceRecorded = _attendanceService.RecordAttendance(student.LocalId, student.FullName);

        //     if (attendanceRecorded)
        //     {
        //         _voiceService.Speak($"Welcome {student.FullName}. Attendance recorded.");
        //     }
        //     else
        //     {
        //         _voiceService.Speak($"Welcome back {student.FullName}.");
        //     }

        //     Debug.WriteLine($"Current pickup: {_currentPickup?.StudentName} (StudentId: {_currentPickup?.StudentId})");
        //     Debug.WriteLine($"Comparing: student.LocalId={student.LocalId} vs _currentPickup.StudentId={_currentPickup?.StudentId}");

        //     if (_currentPickup == null || _currentPickup.StudentId != student.LocalId)
        //     {
        //         _voiceService.Speak("This student is not being called");
        //         return;
        //     }

        //     _queueService.CompletePickup(_currentPickup);
        //     _voiceService.Speak($"Pickup confirmed for {_currentPickup.StudentName}");
        //     Debug.WriteLine($"Pickup confirmed for {_currentPickup.StudentName}");

        //     _currentPickup = null;
        //     NowCallingName.Text = "Waiting for next student...";
        //     NowCallingClass.Text = "";

        //     Debug.WriteLine("=== ScanStudent_Click END ===");
        // }
        ////////////////////////////////////////////////////////////////////////////
        // private async void ScanStudent_Click(object sender, RoutedEventArgs e)
        // {
        //     Debug.WriteLine("=== ScanStudent_Click START ===");

        //     _voiceService.Speak("Scanning student fingerprint");

        //     if (_fingerprintService == null)
        //     {
        //         _voiceService.Speak("Fingerprint device not ready");
        //         Debug.WriteLine("FingerprintService is null");
        //         return;
        //     }

        //     int? fingerprintId = await _fingerprintService.VerifyAsync();
        //     Debug.WriteLine($"Device returned FingerprintId: {fingerprintId}");

        //     if (fingerprintId == null)
        //     {
        //         _voiceService.Speak("Student not recognized");
        //         Debug.WriteLine("No fingerprint matched in device database");
        //         return;
        //     }

        //     var student = _studentRegistry.FindByFingerprint(fingerprintId.Value);
        //     Debug.WriteLine($"Student found: {student?.FullName ?? "NULL"} (LocalId: {student?.LocalId}, FingerprintId: {student?.FingerprintId})");

        //     if (student == null)
        //     {
        //         _voiceService.Speak("Student not recognized");
        //         Debug.WriteLine("Student not found in StudentRegistry");
        //         return;
        //     }

        //     // ✅ ATTENDANCE CHECK: Record attendance (only if not already marked today)
        //     bool attendanceRecorded = _attendanceService.RecordAttendance(student.LocalId, student.FullName);

        //     if (attendanceRecorded)
        //     {
        //         _voiceService.Speak($"Welcome {student.FullName}. Attendance recorded.");
        //     }
        //     else
        //     {
        //         _voiceService.Speak($"Welcome back {student.FullName}.");
        //     }

        //     Debug.WriteLine($"Current pickup: {_currentPickup?.StudentName} (StudentId: {_currentPickup?.StudentId})");
        //     Debug.WriteLine($"Comparing: student.LocalId={student.LocalId} vs _currentPickup.StudentId={_currentPickup?.StudentId}");

        //     if (_currentPickup == null || _currentPickup.StudentId != student.LocalId)
        //     {
        //         _voiceService.Speak("This student is not being called");
        //         return;
        //     }

        //     // Log successful pickup completion
        //     _pickupLogService.LogPickupCompletion(student.LocalId, _currentPickup.GuardianId);

        //     _queueService.CompletePickup(_currentPickup!);
        //     _voiceService.Speak($"Pickup confirmed for {_currentPickup.StudentName}");
        //     Debug.WriteLine($"Pickup confirmed for {_currentPickup.StudentName}");

        //     _currentPickup = null;
        //     NowCallingName.Text = "Waiting for next student...";
        //     NowCallingClass.Text = "";

        //     Debug.WriteLine("=== ScanStudent_Click END ===");
        // }
        ////////////////////////////////////////////////////////////////////////////

        private void StartAdminSessionMonitor()
        {
            _adminSessionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _adminSessionTimer.Tick += (s, e) =>
            {
                UpdateEnrollmentLockUI();
            };

            _adminSessionTimer.Start();
        }


        private void UpdateEnrollmentLockUI()
        {
            if (_adminSecurity.IsInCooldown())
            {
                OpenEnrollmentButton.Content =
                    $"⏳ TRY AGAIN IN {_adminSecurity.CooldownSecondsRemaining()}s";
                OpenEnrollmentButton.ToolTip = "Too many failed PIN attempts";
                return;
            }

            if (_adminSecurity.IsAdminSessionActive())
            {
                OpenEnrollmentButton.Content = "🔓 ENROLLMENT UNLOCKED";
                OpenEnrollmentButton.ToolTip = "Admin session active";
            }
            else
            {
                OpenEnrollmentButton.Content = "🔒 OPEN ENROLLMENT WINDOW";
                OpenEnrollmentButton.ToolTip = "Admin PIN required";
            }
        }


        // private void OpenEnrollment_Click(object sender, RoutedEventArgs e)
        // {
        //     if (_fingerprintService == null)
        //     {
        //         MessageBox.Show(
        //             "Fingerprint device not ready yet.",
        //             "Please wait",
        //             MessageBoxButton.OK,
        //             MessageBoxImage.Information
        //         );
        //         return;
        //     }

        //     var enrollmentWindow = new EnrollmentWindow(
        //         _studentRegistry,
        //         _guardianRegistry,
        //         _guardianStudentRegistry,
        //         _fingerprintService
        //     )
        //     {
        //         Owner = this
        //     };

        //     // enrollmentWindow.Show();
        //     enrollmentWindow.ShowDialog();
        // }

        private void OpenEnrollment_Click(object sender, RoutedEventArgs e)
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
            if (!_adminSecurity.HasPin())
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

                // if (!_adminSecurity.VerifyPin(pinDialog.EnteredPin))
                // {
                //     MessageBox.Show(
                //         "Incorrect PIN",
                //         "Access denied",
                //         MessageBoxButton.OK,
                //         MessageBoxImage.Warning
                //     );
                //     return;
                // }
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

            // OPEN ENROLLMENT
            var enrollmentWindow = new EnrollmentWindow(
                _studentRegistry,
                _guardianRegistry,
                _guardianStudentRegistry,
                _fingerprintService,
                _databaseService,
                _auditLogService
            )
            {
                Owner = this
            };

            // END ADMIN SESSION WHEN ENROLLMENT CLOSES
            // enrollmentWindow.Closed += (s, args) =>
            // {
            //     _adminSecurity.ClearSession();
            //     UpdateEnrollmentLockUI();
            // };
            // REFRESH DATA WHEN ENROLLMENT WINDOW CLOSES
            enrollmentWindow.Closed += async (s, args) =>
            {
                _adminSecurity.ClearSession();
                UpdateEnrollmentLockUI();

                // IMPORTANT: Refresh registries and device database
                await RefreshRegistriesAndDeviceDB();
            };

            enrollmentWindow.ShowDialog();
        }

        // private void ViewAttendanceButton_Click(object sender, RoutedEventArgs e)
        // {
        //     if (!_adminSecurity.IsAdminSessionActive())
        //     {
        //         var pinDialog = new PinDialog { Owner = this };
        //         if (pinDialog.ShowDialog() != true) return;

        //         if (!_adminSecurity.VerifyPin(pinDialog.EnteredPin, out var error))
        //         {
        //             MessageBox.Show(error, "Access denied", MessageBoxButton.OK, MessageBoxImage.Warning);
        //             return;
        //         }
        //     }

        //     var attendanceWindow = new AttendanceReportWindow(_attendanceService, _studentRegistry)
        //     {
        //         Owner = this
        //     };

        //     attendanceWindow.ShowDialog();
        // }
        private void ViewAttendanceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_adminSecurity.IsAdminSessionActive())
            {
                var pinDialog = new PinDialog { Owner = this };
                if (pinDialog.ShowDialog() != true) return;

                if (!_adminSecurity.VerifyPin(pinDialog.EnteredPin, out var error))
                {
                    MessageBox.Show(error, "Access denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var attendanceWindow = new AttendanceReportWindow(_attendanceService, _studentRegistry)
            {
                Owner = this
            };

            // Clear admin session when attendance window closes
            attendanceWindow.Closed += (s, args) =>
            {
                _adminSecurity.ClearSession();
                UpdateEnrollmentLockUI();
            };

            attendanceWindow.ShowDialog();
        }

        private void OpenStudentManagementButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Check if admin session is already active
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


            // 2. Open Student Management window
            // Check if fingerprint service is available
            if (_fingerprintService == null)
            {
                MessageBox.Show(
                    "Fingerprint device is not ready. Please ensure the device is connected and try again.",
                    "Device Not Ready",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            var window = new StudentManagementWindow(
                _studentRegistry,
                _guardianRegistry,
                _guardianStudentRegistry,
                _fingerprintService,     // Now we know it's not null
                _databaseService,
                _auditLogService,
                _adminSecurity
            )
            {
                Owner = this
            };

            // 3. Clear admin session when window closes (same pattern as attendance)
            window.Closed += (s, args) =>
            {
                _adminSecurity.ClearSession();
                UpdateEnrollmentLockUI();
            };

            window.ShowDialog();
        }




        // private void ViewPickupReportButton_Click(object sender, RoutedEventArgs e)
        // {
        //     // Check for admin authentication
        //     if (!_adminSecurity.IsAdminSessionActive())
        //     {
        //         var pinDialog = new PinDialog { Owner = this };
        //         if (pinDialog.ShowDialog() != true) return;

        //         if (!_adminSecurity.VerifyPin(pinDialog.EnteredPin, out var error))
        //         {
        //             MessageBox.Show(error, "Access denied", 
        //                 MessageBoxButton.OK, MessageBoxImage.Warning);
        //             return;
        //         }
        //     }

        //     // Open the pickup report window
        //     var pickupReportWindow = new PickupReportWindow(
        //         _pickupLogService,
        //         _studentRegistry,
        //         _guardianRegistry)
        //     {
        //         Owner = this
        //     };

        //     pickupReportWindow.ShowDialog();
        // }
        // private void ViewPickupReportButton_Click(object sender, RoutedEventArgs e)
        // {
        //     try
        //     {
        //         Debug.WriteLine("=== ViewPickupReportButton_Click START ===");

        //         // Check for admin authentication
        //         if (!_adminSecurity.IsAdminSessionActive())
        //         {
        //             Debug.WriteLine("Admin session not active, showing PIN dialog");
        //             var pinDialog = new PinDialog { Owner = this };
        //             if (pinDialog.ShowDialog() != true)
        //             {
        //                 Debug.WriteLine("PIN dialog cancelled");
        //                 return;
        //             }

        //             if (!_adminSecurity.VerifyPin(pinDialog.EnteredPin, out var error))
        //             {
        //                 Debug.WriteLine($"PIN verification failed: {error}");
        //                 MessageBox.Show(error, "Access denied", 
        //                     MessageBoxButton.OK, MessageBoxImage.Warning);
        //                 return;
        //             }
        //             Debug.WriteLine("PIN verified successfully");
        //         }

        //         Debug.WriteLine("Opening PickupReportWindow");

        //         // Open the pickup report window
        //         var pickupReportWindow = new PickupReportWindow(
        //             _pickupLogService,
        //             _studentRegistry,
        //             _guardianRegistry)
        //         {
        //             Owner = this
        //         };

        //         Debug.WriteLine("PickupReportWindow created, showing dialog");
        //         pickupReportWindow.ShowDialog();
        //         Debug.WriteLine("PickupReportWindow closed");
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.WriteLine($"CRASH in ViewPickupReportButton_Click: {ex.Message}");
        //         Debug.WriteLine($"Stack trace: {ex.StackTrace}");

        //         MessageBox.Show($"Failed to open pickup report:\n\n{ex.Message}", 
        //             "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //     }
        //     finally
        //     {
        //         Debug.WriteLine("=== ViewPickupReportButton_Click END ===");
        //     }
        // }
        private void ViewPickupReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_adminSecurity.IsAdminSessionActive())
            {
                var pinDialog = new PinDialog { Owner = this };
                if (pinDialog.ShowDialog() != true) return;

                if (!_adminSecurity.VerifyPin(pinDialog.EnteredPin, out var error))
                {
                    MessageBox.Show(error, "Access denied",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var pickupReportWindow = new PickupReportWindow(
                _pickupLogService,
                _studentRegistry,
                _guardianRegistry)
            {
                Owner = this
            };

            // Clear admin session when pickup report window closes
            pickupReportWindow.Closed += (s, args) =>
            {
                _adminSecurity.ClearSession();
                UpdateEnrollmentLockUI();
            };

            pickupReportWindow.ShowDialog();
        }

        /// <summary>
        /// Clean up resources when MainWindow closes
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // Stop timers
            _adminSessionTimer?.Stop();
            _scanTimer?.Stop();

            // Dispose fingerprint service
            if (_fingerprintService != null)
            {
                _fingerprintService.Dispose();
                _fingerprintService = null;
            }

            base.OnClosed(e);
        }



    }
}
