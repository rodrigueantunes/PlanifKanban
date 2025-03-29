using System;
using System.Windows;
using System.Windows.Controls;
using PlanifKanban.Models;

namespace PlanifKanban.Views
{
    public partial class KanbanView : Window
    {
        public TaskModel CreatedTask { get; private set; }

        public KanbanView()
        {
            InitializeComponent();
        }

        public KanbanView(TaskModel existingTask) : this()
        {
            // Préremplit les champs
            TaskTitle.Text = existingTask.Title;
            ClientName.Text = existingTask.Client;
            TaskDescription.Text = existingTask.Description;
            StartDatePicker.SelectedDate = existingTask.StartDate;
            DueDatePicker.SelectedDate = existingTask.DueDate;
            RequestedDatePicker.SelectedDate = existingTask.RequestedDate;
            CompletionDatePicker.SelectedDate = existingTask.CompletionDate;
            PlannedTimeDays.Text = existingTask.PlannedTimeDays.ToString("0.##");
            PlannedTimeHours.Text = existingTask.PlannedTimeHours.ToString("0.##");
            ActualTimeDays.Text = existingTask.ActualTimeDays.ToString("0.##");
            ActualTimeHours.Text = existingTask.ActualTimeHours.ToString("0.##");

            CreatedTask = existingTask; // Sera modifié à la fermeture
        }

        private void OnAddTaskClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TaskTitle.Text) || string.IsNullOrWhiteSpace(ClientName.Text))
            {
                MessageBox.Show("Le titre et le client sont obligatoires.");
                return;
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
            if (double.TryParse(PlannedTimeDays.Text, out double days))
            {
                PlannedTimeHours.Text = (days * 8).ToString("0.##");
            }
        }

        private void OnPlannedTimeHoursChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(PlannedTimeHours.Text, out double hours))
            {
                PlannedTimeDays.Text = (hours / 8).ToString("0.##");
            }
        }

        private void OnActualTimeDaysChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(ActualTimeDays.Text, out double days))
            {
                ActualTimeHours.Text = (days * 8).ToString("0.##");
            }
        }

        private void OnActualTimeHoursChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(ActualTimeHours.Text, out double hours))
            {
                ActualTimeDays.Text = (hours / 8).ToString("0.##");
            }
        }
    }
}