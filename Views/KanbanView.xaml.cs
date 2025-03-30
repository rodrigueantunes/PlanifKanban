using System;
using System.Windows;
using System.Windows.Controls;
using PlanifKanban.Models;
using PlanifKanban.Utilities;

namespace PlanifKanban.Views
{
    public partial class KanbanView : Window
    {
        public TaskModel CreatedTask { get; private set; }
        private bool _updatingControls = false;

        public KanbanView()
        {
            InitializeComponent();

            // Ajouter les gestionnaires d'événements pour les DatePickers
            RequestedDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            DueDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            StartDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            CompletionDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;

            // Ajouter les gestionnaires pour les calculs de durée
            DueDatePicker.SelectedDateChanged += RecalculateDuration;
            StartDatePicker.SelectedDateChanged += RecalculateDuration;
        }

        public KanbanView(TaskModel existingTask) : this()
        {
            // Préremplit les champs
            TaskTitle.Text = existingTask.Title;
            ClientName.Text = existingTask.Client;
            TaskDescription.Text = existingTask.Description;

            // Éviter le déclenchement des événements pendant l'initialisation
            _updatingControls = true;

            StartDatePicker.SelectedDate = existingTask.StartDate;
            DueDatePicker.SelectedDate = existingTask.DueDate;
            RequestedDatePicker.SelectedDate = existingTask.RequestedDate;
            CompletionDatePicker.SelectedDate = existingTask.CompletionDate;

            PlannedTimeDays.Text = existingTask.PlannedTimeDays.ToString("0.##");
            PlannedTimeHours.Text = existingTask.PlannedTimeHours.ToString("0.##");
            ActualTimeDays.Text = existingTask.ActualTimeDays.ToString("0.##");
            ActualTimeHours.Text = existingTask.ActualTimeHours.ToString("0.##");

            _updatingControls = false;

            CreatedTask = existingTask; // Sera modifié à la fermeture
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

        private void RecalculateDuration(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingControls) return;

            UpdatePlannedTimeFromDates();
        }

        private void UpdatePlannedTimeFromDates()
        {
            if (StartDatePicker.SelectedDate.HasValue && DueDatePicker.SelectedDate.HasValue)
            {
                DateTime startDate = StartDatePicker.SelectedDate.Value;
                DateTime dueDate = DueDatePicker.SelectedDate.Value;

                // S'assurer que la date de fin est après la date de début
                if (dueDate < startDate)
                {
                    MessageBox.Show("La date prévue doit être postérieure à la date de début.",
                        "Erreur de date", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _updatingControls = true;
                    DueDatePicker.SelectedDate = startDate;
                    _updatingControls = false;
                    return;
                }

                // Calculer le nombre de jours ouvrables entre les deux dates
                int workDays = WorkdayCalculator.GetWorkdayCount(startDate, dueDate);

                // Ne pas déclencher l'événement de modification pour éviter les boucles infinies
                _updatingControls = true;
                PlannedTimeDays.Text = workDays.ToString("0.##");
                PlannedTimeHours.Text = (workDays * 8).ToString("0.##");
                _updatingControls = false;
            }
        }

        private void OnAddTaskClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TaskTitle.Text) || string.IsNullOrWhiteSpace(ClientName.Text))
            {
                MessageBox.Show("Le titre et le client sont obligatoires.");
                return;
            }

            // S'assurer que les dates sélectionnées sont des jours ouvrables
            if (StartDatePicker.SelectedDate.HasValue && !WorkdayCalculator.IsWorkday(StartDatePicker.SelectedDate.Value))
            {
                StartDatePicker.SelectedDate = WorkdayCalculator.AdjustToWorkday(StartDatePicker.SelectedDate.Value);
                MessageBox.Show("La date de début a été ajustée au prochain jour ouvrable.");
            }

            if (DueDatePicker.SelectedDate.HasValue && !WorkdayCalculator.IsWorkday(DueDatePicker.SelectedDate.Value))
            {
                DueDatePicker.SelectedDate = WorkdayCalculator.AdjustToWorkday(DueDatePicker.SelectedDate.Value);
                MessageBox.Show("La date prévue a été ajustée au prochain jour ouvrable.");
            }

            if (RequestedDatePicker.SelectedDate.HasValue && !WorkdayCalculator.IsWorkday(RequestedDatePicker.SelectedDate.Value))
            {
                RequestedDatePicker.SelectedDate = WorkdayCalculator.AdjustToWorkday(RequestedDatePicker.SelectedDate.Value);
                MessageBox.Show("La date demandée a été ajustée au prochain jour ouvrable.");
            }

            if (CompletionDatePicker.SelectedDate.HasValue && !WorkdayCalculator.IsWorkday(CompletionDatePicker.SelectedDate.Value))
            {
                CompletionDatePicker.SelectedDate = WorkdayCalculator.AdjustToWorkday(CompletionDatePicker.SelectedDate.Value);
                MessageBox.Show("La date de finalisation a été ajustée au prochain jour ouvrable.");
            }

            CreatedTask = new TaskModel
            {
                Title = TaskTitle.Text,
                Client = ClientName.Text,
                Description = TaskDescription.Text,
                StartDate = StartDatePicker.SelectedDate,
                DueDate = DueDatePicker.SelectedDate,
                RequestedDate = RequestedDatePicker.SelectedDate,
                CompletionDate = CompletionDatePicker.SelectedDate,
                PlannedTimeDays = double.TryParse(PlannedTimeDays.Text, out double plannedDays) ? plannedDays : 0,
                PlannedTimeHours = double.TryParse(PlannedTimeHours.Text, out double plannedHours) ? plannedHours : 0,
                ActualTimeDays = double.TryParse(ActualTimeDays.Text, out double actualDays) ? actualDays : 0,
                ActualTimeHours = double.TryParse(ActualTimeHours.Text, out double actualHours) ? actualHours : 0
            };

            DialogResult = true;
        }

        private void OnPlannedTimeDaysChanged(object sender, TextChangedEventArgs e)
        {
            if (_updatingControls) return;

            if (double.TryParse(PlannedTimeDays.Text, out double workDays))
            {
                // Convertir les jours en heures (8h par jour)
                _updatingControls = true;
                PlannedTimeHours.Text = (workDays * 8).ToString("0.##");
                _updatingControls = false;


                if (StartDatePicker.SelectedDate.HasValue)
                {
                    int wholeDays = (int)Math.Ceiling(workDays);
                    DateTime startDate = StartDatePicker.SelectedDate.Value;
                    DateTime dueDate = WorkdayCalculator.AddWorkdays(startDate, wholeDays);

                    _updatingControls = true;
                    DueDatePicker.SelectedDate = dueDate;
                    _updatingControls = false;
                }
                
            }
        }

        private void OnPlannedTimeHoursChanged(object sender, TextChangedEventArgs e)
        {
            if (_updatingControls) return;

            if (double.TryParse(PlannedTimeHours.Text, out double hours))
            {
                double days = hours / 8;

                _updatingControls = true;
                PlannedTimeDays.Text = days.ToString("0.##");
                _updatingControls = false;

                // Calculer automatiquement la date d'échéance
                if (StartDatePicker.SelectedDate.HasValue)
                {
                    int wholeDays = (int)Math.Ceiling(days);
                    DateTime startDate = StartDatePicker.SelectedDate.Value;
                    DateTime dueDate = WorkdayCalculator.AddWorkdays(startDate, wholeDays);

                    _updatingControls = true;
                    DueDatePicker.SelectedDate = dueDate;
                    _updatingControls = false;
                }
            }
        }

        private void OnActualTimeDaysChanged(object sender, TextChangedEventArgs e)
        {
            if (_updatingControls) return;

            if (double.TryParse(ActualTimeDays.Text, out double days))
            {
                _updatingControls = true;
                ActualTimeHours.Text = (days * 8).ToString("0.##");
                _updatingControls = false;
            }
        }

        private void OnActualTimeHoursChanged(object sender, TextChangedEventArgs e)
        {
            if (_updatingControls) return;

            if (double.TryParse(ActualTimeHours.Text, out double hours))
            {
                _updatingControls = true;
                ActualTimeDays.Text = (hours / 8).ToString("0.##");
                _updatingControls = false;
            }
        }
    }
}