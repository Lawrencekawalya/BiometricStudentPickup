using System.Text.RegularExpressions;
using System.Windows;

namespace BiometricStudentPickup.Views
{
    public partial class CreatePinDialog : Window
    {
        public string Pin { get; private set; } = "";

        public CreatePinDialog()
        {
            InitializeComponent();
            PinBox.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var pin = PinBox.Password;
            var confirm = ConfirmPinBox.Password;

            if (!IsValidPin(pin))
            {
                MessageBox.Show(
                    "PIN must be exactly 6 digits.",
                    "Invalid PIN",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            if (pin != confirm)
            {
                MessageBox.Show(
                    "PINs do not match.",
                    "Mismatch",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            Pin = pin;
            DialogResult = true;
        }

        private bool IsValidPin(string pin)
        {
            return Regex.IsMatch(pin, @"^\d{6}$");
        }
    }
}
