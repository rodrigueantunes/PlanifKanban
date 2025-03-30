using System;
using System.Windows;
using System.Windows.Controls;
using PlanifKanban.Models;
using PlanifKanban.Utilities;

namespace PlanifKanban.Views
{
    public partial class DateMissingPopup : Window
    {
        private bool _updatingControls = false;

        public DateTime? StartDate { get; private set; }
        public DateTime? DueDate { get; private set; }
        public DateTime? RequestedDate { get; private set; }
        public DateTime? CompletionDate { get; private set; }

        public DateMissingPopup(string column, TaskModel task = null)
        {
            InitializeComponent();

            // Ajouter les gestionnaires d'événements pour les DatePickers
            RequestedDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            DueDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            StartDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            CompletionDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;

            // Précharger les dates existantes si une tâche est fournie
            if (task != null)
            {
                _updatingControls = true;

                StartDatePicker.SelectedDate = task.StartDate;
                DueDatePicker.SelectedDate = task.DueDate;
                RequestedDatePicker.SelectedDate = task.RequestedDate;
                CompletionDatePicker.SelectedDate = task.CompletionDate;

                _updatingControls = false;
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

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingControls) return;

            DatePicker datePicker = sender as DatePicker;
            if (datePicker?.SelectedDate != null)
            {
                DateTime selectedDate = datePicker.SelectedDate.Value;
                if (!WorkdayCalculator.IsWorkday(selectedDate))
                {
                    _updatingControls = true;
                    // Si un weekend est sélectionné, déplacer au prochain jour ouvrable
                    datePicker.SelectedDate = WorkdayCalculator.AdjustToWorkday(selectedDate);
                    _updatingControls = false;

                    MessageBox.Show("Les weekends ne sont pas des jours ouvrables. La date a été ajustée au prochain jour ouvrable.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void OnValidateClick(object sender, RoutedEventArgs e)
        {
            StartDate = StartDatePicker.SelectedDate;
            DueDate = DueDatePicker.SelectedDate;
            RequestedDate = RequestedDatePicker.SelectedDate;
            CompletionDate = CompletionDatePicker.SelectedDate;

            // S'assurer que les dates sont des jours ouvrables
            if (StartDate.HasValue && !WorkdayCalculator.IsWorkday(StartDate.Value))
            {
                StartDate = WorkdayCalculator.AdjustToWorkday(StartDate.Value);
                MessageBox.Show("La date de début a été ajustée au prochain jour ouvrable.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (DueDate.HasValue && !WorkdayCalculator.IsWorkday(DueDate.Value))
            {
                DueDate = WorkdayCalculator.AdjustToWorkday(DueDate.Value);
                MessageBox.Show("La date prévue a été ajustée au prochain jour ouvrable.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (RequestedDate.HasValue && !WorkdayCalculator.IsWorkday(RequestedDate.Value))
            {
                RequestedDate = WorkdayCalculator.AdjustToWorkday(RequestedDate.Value);
                MessageBox.Show("La date demandée a été ajustée au prochain jour ouvrable.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (CompletionDate.HasValue && !WorkdayCalculator.IsWorkday(CompletionDate.Value))
            {
                CompletionDate = WorkdayCalculator.AdjustToWorkday(CompletionDate.Value);
                MessageBox.Show("La date de finalisation a été ajustée au prochain jour ouvrable.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Vérifier que les dates obligatoires sont renseignées
            if (StartDate.HasValue && DueDate.HasValue)
            {
                // Vérifier que la date de fin est après la date de début
                if (DueDate.Value < StartDate.Value)
                {
                    MessageBox.Show("La date prévue doit être postérieure à la date de début.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

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