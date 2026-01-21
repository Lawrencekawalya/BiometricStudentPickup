// using System.Collections.ObjectModel;
// using BiometricStudentPickup.Models;

// namespace BiometricStudentPickup.Services
// {
//     public class QueueService
//     {
//         public ObservableCollection<PickupQueue> Queue { get; }
//             = new ObservableCollection<PickupQueue>();

//         // public void AddPickup(int studentId, string studentName, string className)
//         // {
//         //     Queue.Add(new PickupQueue
//         //     {
//         //         StudentId = studentId,
//         //         StudentName = studentName,
//         //         ClassName = className
//         //     });
//         // }
//         public void AddPickup(int studentId, string studentName, string className, int? guardianId = null)
//         {
//             var pickup = new PickupQueue
//             {
//                 StudentId = studentId,
//                 StudentName = studentName,
//                 ClassName = className,
//                 GuardianId = guardianId,
//                 RequestedAt = DateTime.Now,
//                 Status = "Waiting"
//             };

//             _queue.Enqueue(pickup);
            
//             // Log pickup request
//             if (_pickupLogService != null)
//             {
//                 _pickupLogService.LogPickupRequest(studentId, guardianId);
//             }
            
//             OnPropertyChanged(nameof(Queue));
//         }

//         public PickupQueue? GetNext()
//         {
//             if (Queue.Count == 0)
//                 return null;

//             return Queue[0];
//         }

//         public void CompletePickup(PickupQueue pickup)
//         {
//             pickup.Status = "Completed";
//             Queue.Remove(pickup);
//         }

//         // NEW
//         public void RequeuePickup(PickupQueue pickup)
//         {
//             pickup.RetryCount++;
//             pickup.CalledAt = null;
//             pickup.Status = "Pending (Retry)";

//             Queue.Remove(pickup);
//             Queue.Add(pickup);
//         }
//     }
// }


// using System;
// using System.Collections.ObjectModel;
// using BiometricStudentPickup.Models;

// namespace BiometricStudentPickup.Services
// {
//     public class QueueService
//     {
//         public ObservableCollection<PickupQueue> Queue { get; }
//             = new ObservableCollection<PickupQueue>();

//         // Option 1: Simple version without logging
//         public void AddPickup(int studentId, string studentName, string className, int? guardianId = null)
//         {
//             var pickup = new PickupQueue
//             {
//                 StudentId = studentId,
//                 StudentName = studentName,
//                 ClassName = className,
//                 GuardianId = guardianId,
//                 RequestedAt = DateTime.Now,
//                 Status = "Waiting"
//             };

//             // Add to the ObservableCollection, not a private queue
//             Queue.Add(pickup);
            
//             // Note: We removed the logging here because we need to handle it in MainWindow
//             // The logging will be done in MainWindow's ScanGuardian_Click method
//         }

//         public PickupQueue? GetNext()
//         {
//             if (Queue.Count == 0)
//                 return null;

//             return Queue[0];
//         }

//         public void CompletePickup(PickupQueue pickup)
//         {
//             pickup.Status = "Completed";
//             Queue.Remove(pickup);
//         }

//         public void RequeuePickup(PickupQueue pickup)
//         {
//             pickup.RetryCount++;
//             pickup.CalledAt = null;
//             pickup.Status = "Pending (Retry)";

//             Queue.Remove(pickup);
//             Queue.Add(pickup);
//         }
//     }
// }

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using BiometricStudentPickup.Models;

namespace BiometricStudentPickup.Services
{
    public class QueueService : INotifyPropertyChanged
    {
        private ObservableCollection<PickupQueue> _queue = new ObservableCollection<PickupQueue>();
        
        public ObservableCollection<PickupQueue> Queue
        {
            get => _queue;
            set
            {
                _queue = value;
                OnPropertyChanged(nameof(Queue));
            }
        }

        public void AddPickup(int studentId, string studentName, string className, int? guardianId = null)
        {
            var pickup = new PickupQueue
            {
                StudentId = studentId,
                StudentName = studentName,
                ClassName = className,
                GuardianId = guardianId,
                RequestedAt = DateTime.Now,
                Status = "Waiting"
            };

            Queue.Add(pickup);
            UpdateQueueNumbers();
            
            // Notify that Queue has changed
            OnPropertyChanged(nameof(Queue));
        }

        public PickupQueue? GetNext()
        {
            if (Queue.Count == 0)
                return null;

            return Queue[0];
        }

        public void CompletePickup(PickupQueue pickup)
        {
            if (Queue.Contains(pickup))
            {
                pickup.Status = "Completed";
                Queue.Remove(pickup);
                UpdateQueueNumbers();
                
                // Notify that Queue has changed
                OnPropertyChanged(nameof(Queue));
            }
        }

        public void RequeuePickup(PickupQueue pickup)
        {
            if (Queue.Contains(pickup))
            {
                pickup.RetryCount++;
                pickup.CalledAt = null;
                pickup.Status = "Pending (Retry)";

                Queue.Remove(pickup);
                Queue.Add(pickup);
                UpdateQueueNumbers();
                
                // Notify that Queue has changed
                OnPropertyChanged(nameof(Queue));
            }
        }
        
        private void UpdateQueueNumbers()
        {
            for (int i = 0; i < Queue.Count; i++)
            {
                Queue[i].QueueNumber = i + 1;
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}