using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PlanifKanban.Models;
using PlanifKanban.ViewModels;

namespace PlanifKanban.Views
{
    public partial class GanttWindow : Window
    {
        private KanbanViewModel _viewModel;
        public ObservableCollection<GanttTaskViewModel> GanttTasks { get; set; }
        private DateTime _minDate;
        private DateTime _maxDate;

        public GanttWindow(KanbanViewModel viewModel)
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                _viewModel = viewModel ?? new KanbanViewModel();
                GanttTasks = new ObservableCollection<GanttTaskViewModel>();

                TimeScaleComboBox.SelectedIndex = 1; // Par défaut "Jours"

                PrepareGanttTasks();
                ConfigureTimeScale();
                GanttTasksItemsControl.ItemsSource = GanttTasks;
            };
        }

        private void PrepareGanttTasks()
        {
            if (_viewModel == null ||
                _viewModel.TodoTasks == null ||
                _viewModel.InProgressTasks == null ||
                _viewModel.TestingTasks == null ||
                _viewModel.DoneTasks == null)
            {
                GanttTasks?.Clear();
                return;
            }

            // Fusionner les listes et sécuriser chaque tâche
            var allTasks = _viewModel.TodoTasks
                .Concat(_viewModel.InProgressTasks)
                .Concat(_viewModel.TestingTasks)
                .Concat(_viewModel.DoneTasks)
                .Where(t => t != null)
                .ToList();

            // Filtrer les tâches avec dates valides pour Gantt
            var activeTasks = allTasks
                .Where(t =>
                    (t.StartDate.HasValue || t.DueDate.HasValue) &&
                    (!t.CompletionDate.HasValue || t.CompletionDate.Value.Date > DateTime.Today))
                .ToList();

            if (!activeTasks.Any())
            {
                _minDate = DateTime.Today;
                _maxDate = DateTime.Today.AddDays(7);
                GanttTasks.Clear();
                TimeScaleItemsControl.ItemsSource = null;
                return;
            }

            _minDate = activeTasks
                .Select(t => t.StartDate ?? t.DueDate ?? DateTime.Today)
                .Min();

            _maxDate = activeTasks
                .Select(t =>
                {
                    var baseDate = t.StartDate ?? t.DueDate ?? DateTime.Today;
                    return baseDate.AddDays(t.PlannedTimeDays);
                })
                .Max();

            GanttTasks.Clear();
            foreach (var task in activeTasks)
            {
                var ganttTask = CreateGanttTask(task);
                if (ganttTask != null)
                    GanttTasks.Add(ganttTask);
            }
        }




        private GanttTaskViewModel CreateGanttTask(TaskModel task)
        {
            if (!task.StartDate.HasValue && !task.DueDate.HasValue)
                return null;

            DateTime startDate = task.StartDate ?? task.DueDate.Value;
            double pixelsPerUnit = GetPixelsPerUnit();
            double daysFromStart = (startDate - _minDate).TotalDays;
            double offset = daysFromStart * pixelsPerUnit;
            double duration = task.PlannedTimeDays * pixelsPerUnit;

            return new GanttTaskViewModel
            {
                Title = task.Title,
                Client = task.Client,
                Description = task.Description,
                StartDate = startDate,
                DueDate = task.DueDate,
                RequestedDate = task.RequestedDate,
                PlannedTimeDays = task.PlannedTimeDays,
                Duration = duration,
                Offset = new Thickness(offset, 0, 0, 0),
                OriginalTask = task
            };
        }

        private void ConfigureTimeScale()
        {
            if (GanttTasks == null || !GanttTasks.Any())
                return;

            var selectedScale = TimeScaleComboBox.SelectedItem as ComboBoxItem;
            if (selectedScale == null)
                return;

            var days = new List<string>();

            switch (selectedScale.Content?.ToString())
            {
                case "Heures":
                    for (var date = _minDate; date <= _maxDate; date = date.AddHours(1))
                        days.Add(date.ToString("HH:mm"));
                    break;

                case "Jours":
                    for (var date = _minDate.Date; date <= _maxDate.Date; date = date.AddDays(1))
                        days.Add(date.ToString("dd/MM"));
                    break;

                case "Semaines":
                    for (var date = _minDate.Date.AddDays(-(int)_minDate.DayOfWeek);
                         date <= _maxDate.Date;
                         date = date.AddDays(7))
                        days.Add(date.ToString("dd/MM"));
                    break;

                case "Mois":
                    for (var date = new DateTime(_minDate.Year, _minDate.Month, 1);
                         date <= _maxDate;
                         date = date.AddMonths(1))
                        days.Add(date.ToString("MM/yyyy"));
                    break;
            }

            TimeScaleItemsControl.ItemsSource = days;
        }

        private void TimeScaleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PrepareGanttTasks();
            ConfigureTimeScale();
        }

        private double GetPixelsPerUnit()
        {
            var selectedScale = TimeScaleComboBox.SelectedItem as ComboBoxItem;
            switch (selectedScale?.Content?.ToString())
            {
                case "Heures": return 2;
                case "Jours": return 50;
                case "Semaines": return 10;
                case "Mois": return 2;
                default: return 50;
            }
        }

        private void GanttTasksItemsControl_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var hitTestResult = VisualTreeHelper.HitTest(GanttTasksItemsControl, e.GetPosition(GanttTasksItemsControl));
            if (hitTestResult == null) return;

            var container = FindParent<Border>(hitTestResult.VisualHit);
            if (container == null) return;

            var task = container.DataContext as GanttTaskViewModel;
            if (task == null) return;

            var editWindow = new KanbanView(task.OriginalTask);
            if (editWindow.ShowDialog() == true && editWindow.CreatedTask != null)
            {
                UpdateTask(task, editWindow.CreatedTask);
            }
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            T parent = parentObject as T;
            return parent ?? FindParent<T>(parentObject);
        }

        private void UpdateTask(GanttTaskViewModel ganttTask, TaskModel updatedTask)
        {
            var comparer = new TodoTaskComparer();
            comparer.UpdateOriginalTask(_viewModel, new TaskWithSortDate(ganttTask.OriginalTask), updatedTask);
            PrepareGanttTasks();
            ConfigureTimeScale();
        }
    }

    public class GanttTaskViewModel
    {
        public string Title { get; set; }
        public string Client { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? RequestedDate { get; set; }
        public double PlannedTimeDays { get; set; }
        public double Duration { get; set; }
        public Thickness Offset { get; set; }
        public TaskModel OriginalTask { get; set; }

        public string StartDateDisplay => $"Début : {StartDate:dd/MM/yyyy}";
        public string DueDateDisplay => DueDate.HasValue ? $"Prévu : {DueDate:dd/MM/yyyy}" : "";
        public string RequestedDateDisplay => RequestedDate.HasValue ? $"Demandé : {RequestedDate:dd/MM/yyyy}" : "";
        public string PlannedTimeDisplay => $"Temps prévu : {PlannedTimeDays:0.#} jours";
    }
}
