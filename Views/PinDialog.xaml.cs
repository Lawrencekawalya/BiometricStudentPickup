using System.Windows;

namespace BiometricStudentPickup.Views
{
    public partial class PinDialog : Window
    {
        public string EnteredPin => PinBox.Password;

        public PinDialog()
        {
            InitializeComponent();
            PinBox.Focus();
        }

        private void Unlock_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
