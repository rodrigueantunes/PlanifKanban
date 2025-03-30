using System;
using System.Windows;

namespace PlanifKanban.Views
{
    public partial class DateSelectionDialog : Window
    {
        public DateTime SelectedDate { get; private set; }

        public DateSelectionDialog()
        {
            InitializeComponent();
            TargetDatePicker.SelectedDate = DateTime.Today;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (TargetDatePicker.SelectedDate.HasValue)
            {
                SelectedDate = TargetDatePicker.SelectedDate.Value;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner une date valide.", "Date manquante",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}