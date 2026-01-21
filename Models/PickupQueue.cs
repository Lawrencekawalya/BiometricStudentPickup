// using System;

// namespace BiometricStudentPickup.Models
// {
//     public class PickupQueue
//     {
//         public int StudentId { get; set; }
//         public Guid Id { get; set; } = Guid.NewGuid();

//         public string StudentName { get; set; } = "";
//         public string ClassName { get; set; } = "";

//         public DateTime RequestedAt { get; set; } = DateTime.Now;

//         // NEW: Add GuardianId property
//         public int? GuardianId { get; set; }

//         // NEW
//         public DateTime? CalledAt { get; set; }
//         public int RetryCount { get; set; } = 0;

//         public string Status { get; set; } = "Pending";
//     }
// }


using System;
using System.ComponentModel;

namespace BiometricStudentPickup.Models
{
    public class PickupQueue : INotifyPropertyChanged
    {
        public int StudentId { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();

        public string StudentName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public int? GuardianId { get; set; }
        public DateTime? CalledAt { get; set; }
        public int RetryCount { get; set; } = 0;

        // Added QueueNumber property
        private int _queueNumber;
        public int QueueNumber
        {
            get => _queueNumber;
            set
            {
                if (_queueNumber != value)
                {
                    _queueNumber = value;
                    OnPropertyChanged(nameof(QueueNumber));
                }
            }
        }

        // Added AddedAt property
        private DateTime _addedAt = DateTime.Now;
        public DateTime AddedAt
        {
            get => _addedAt;
            set
            {
                if (_addedAt != value)
                {
                    _addedAt = value;
                    OnPropertyChanged(nameof(AddedAt));
                }
            }
        }

        private string _status = "Pending";
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}