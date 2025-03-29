using System;
using System.Windows;
using PlanifKanban.Models;

namespace PlanifKanban.Views
{
    public partial class DateMissingPopup : Window
    {
        public DateTime? StartDate { get; private set; }
        public DateTime? DueDate { get; private set; }
        public DateTime? RequestedDate { get; private set; }
        public DateTime? CompletionDate { get; private set; }

        public DateMissingPopup(string column, TaskModel task = null)
        {
            InitializeComponent();

            // Précharger les dates existantes si une tâche est fournie
            if (task != null)
            {
                StartDatePicker.SelectedDate = task.StartDate;
                DueDatePicker.SelectedDate = task.DueDate;
                RequestedDatePicker.SelectedDate = task.RequestedDate;
                CompletionDatePicker.SelectedDate = task.CompletionDate;
            }

            // La date de finalisation n'est visible et obligatoire que pour la colonne "Terminée"
            if (column == "Terminée")
            {
                CompletionDateGroup.Visibility = Visibility.Visible;
            }
            else
            {
                CompletionDateGroup.Visibility = Visibility.Collapsed;
            }
        }

        private void OnValidateClick(object sender, RoutedEventArgs e)
        {
            StartDate = StartDatePicker.SelectedDate;
            DueDate = DueDatePicker.SelectedDate;
            RequestedDate = RequestedDatePicker.SelectedDate;
            CompletionDate = CompletionDatePicker.SelectedDate;

            // Vérifier que les dates obligatoires sont renseignées
            if (StartDate.HasValue && DueDate.HasValue)
            {
                // Si la date de finalisation est visible (colonne "Terminée"), elle doit être renseignée
                if (CompletionDateGroup.Visibility == Visibility.Visible && !CompletionDate.HasValue)
                {
                    MessageBox.Show("La date de finalisation est obligatoire pour terminer une tâche.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Les dates de début et prévue sont obligatoires.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}