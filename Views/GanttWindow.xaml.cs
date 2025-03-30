using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PlanifKanban.Models;
using PlanifKanban.ViewModels;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PlanifKanban.Utilities;

namespace PlanifKanban.Views
{
    public partial class GanttWindow : Window
    {
        private KanbanViewModel _viewModel;
        public ObservableCollection<GanttTaskViewModel> GanttTasks { get; set; }
        private DateTime _minDate;
        private DateTime _maxDate;
        private bool _isScrollChanging = false;

        public GanttWindow(KanbanViewModel viewModel)
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                _viewModel = viewModel ?? new KanbanViewModel();
                GanttTasks = new ObservableCollection<GanttTaskViewModel>();

                TimeScaleComboBox.SelectedIndex = 0; // Par défaut "Jours"
                ShowDescriptionCheckBox.IsChecked = false; // Par défaut, ne pas afficher les descriptions

                // Définir la valeur Tag pour la largeur des colonnes de l'échelle de temps
                TimeScaleItemsControl.Tag = GetPixelsPerUnit();

                PrepareGanttTasks();
                ConfigureTimeScale();
                GanttTasksItemsControl.ItemsSource = GanttTasks;
                TaskInfoItemsControl.ItemsSource = GanttTasks;

                // Synchroniser les scrollbars
                TaskInfoScrollViewer.ScrollChanged += (sender, args) =>
                {
                    if (_isScrollChanging) return;
                    _isScrollChanging = true;

                    try
                    {
                        if (args.VerticalChange != 0)
                            GanttScrollViewer.ScrollToVerticalOffset(args.VerticalOffset);
                    }
                    finally
                    {
                        _isScrollChanging = false;
                    }
                };
            };
        }

        public class GanttTaskViewModel : INotifyPropertyChanged
        {
            private int _rowIndex;
            private string _description;
            private bool _isHighlighted;
            private bool _isInProgress;

            public string Title { get; set; }
            public string Client { get; set; }

            public double GetWorkdayDuration()
            {
                if (StartDate == DateTime.MinValue || !DueDate.HasValue)
                    return PlannedTimeDays;

                return WorkdayCalculator.GetWorkdayCount(StartDate, DueDate.Value);
            }

            public string Description
            {
                get => _description;
                set
                {
                    if (_description != value)
                    {
                        _description = value;
                        OnPropertyChanged(nameof(Description));
                    }
                }
            }

            public DateTime StartDate { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? RequestedDate { get; set; }
            public double PlannedTimeDays { get; set; }
            public double Duration { get; set; }
            public double StartOffset { get; set; }
            public TaskModel OriginalTask { get; set; }

            public bool IsHighlighted
            {
                get => _isHighlighted;
                set
                {
                    if (_isHighlighted != value)
                    {
                        _isHighlighted = value;
                        OnPropertyChanged(nameof(IsHighlighted));
                    }
                }
            }

            public bool IsInProgress
            {
                get => _isInProgress;
                set
                {
                    if (_isInProgress != value)
                    {
                        _isInProgress = value;
                        OnPropertyChanged(nameof(IsInProgress));
                    }
                }
            }

            public int RowIndex
            {
                get => _rowIndex;
                set
                {
                    if (_rowIndex != value)
                    {
                        _rowIndex = value;
                        OnPropertyChanged(nameof(RowIndex));
                    }
                }
            }

            public string StartDateDisplay => $"Début : {StartDate:dd/MM/yyyy}";
            public string DueDateDisplay => DueDate.HasValue ? $"Prévu : {DueDate:dd/MM/yyyy}" : "";
            public string RequestedDateDisplay => RequestedDate.HasValue ? $"Demandé : {RequestedDate:dd/MM/yyyy}" : "";
            public string PlannedTimeDisplay => $"Temps prévu : {PlannedTimeDays:0.#} jours";

            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
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
                    (!t.CompletionDate.HasValue || t.CompletionDate.Value.Date >= DateTime.Today.AddDays(-7)))
                .ToList();

            if (!activeTasks.Any())
            {
                _minDate = DateTime.Today;
                _maxDate = DateTime.Today.AddDays(7);
                GanttTasks.Clear();
                TimeScaleItemsControl.ItemsSource = null;
                return;
            }

            // Calculer les dates minimales et maximales en tenant compte à la fois des dates de début et d'échéance
            _minDate = activeTasks
                .Select(t =>
                {
                    if (t.StartDate.HasValue) return t.StartDate.Value;
                    if (t.DueDate.HasValue) return t.DueDate.Value.AddDays(-Math.Max(t.PlannedTimeDays, 1.0));
                    return DateTime.Today;
                })
                .Min();

            _maxDate = activeTasks
                .Select(t =>
                {
                    if (t.DueDate.HasValue) return t.DueDate.Value;
                    if (t.StartDate.HasValue) return t.StartDate.Value.AddDays(Math.Max(t.PlannedTimeDays, 1.0));
                    return DateTime.Today.AddDays(7);
                })
                .Max();

            // Ajouter une marge au début et à la fin de la période
            _minDate = _minDate.AddDays(-2);
            _maxDate = _maxDate.AddDays(2);

            // Créer les tâches Gantt
            var ganttTaskList = new List<GanttTaskViewModel>();
            foreach (var task in activeTasks)
            {
                var ganttTask = CreateGanttTask(task);
                if (ganttTask != null)
                    ganttTaskList.Add(ganttTask);
            }

            // Organiser les tâches en évitant les chevauchements
            var organizedTasks = OrganizeTasksInParallel(ganttTaskList);

            // Mettre à jour la collection observable
            GanttTasks.Clear();
            foreach (var task in organizedTasks)
            {
                GanttTasks.Add(task);
            }

            // Mettre à jour les hauteurs des tâches
            UpdateTaskHeights();
        }

        private void UpdateTaskHeights()
        {
            // Notifier que les propriétés ont changé pour forcer la mise à jour de l'UI
            foreach (GanttTaskViewModel task in GanttTasks)
            {
                task.OnPropertyChanged("Description");
                task.OnPropertyChanged("Title");
                task.OnPropertyChanged("Client");
            }
        }

        private List<GanttTaskViewModel> OrganizeTasksInParallel(List<GanttTaskViewModel> tasks)
        {
            // Liste des tâches organisées à retourner
            var result = new List<GanttTaskViewModel>();

            // Si aucune tâche, retourner une liste vide
            if (tasks == null || !tasks.Any())
                return result;

            // Cloner la liste d'entrée pour éviter de la modifier
            var tasksToOrganize = new List<GanttTaskViewModel>(tasks);

            // 1. Trier d'abord par date de début, puis par durée (les plus longues d'abord)
            tasksToOrganize = tasksToOrganize
                .OrderBy(t => t.StartDate)
                .ThenByDescending(t => t.PlannedTimeDays)
                .ToList();

            // 2. Initialiser la structure des lignes (tracks)
            var tracks = new List<List<GanttTaskViewModel>>();

            // 3. Pour chaque tâche à placer
            foreach (var task in tasksToOrganize)
            {
                // Chercher une ligne (track) où cette tâche peut être placée sans chevauchement
                bool placed = false;

                // Parcourir toutes les lignes existantes
                for (int trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
                {
                    var track = tracks[trackIndex];

                    // Vérifier si cette tâche peut être placée dans cette ligne
                    bool canPlaceInTrack = true;

                    foreach (var existingTask in track)
                    {
                        // Nouvelle vérification plus stricte qui prend en compte les dates d'échéance
                        bool overlap = CheckOverlap(task, existingTask);

                        if (overlap)
                        {
                            canPlaceInTrack = false;
                            break;
                        }
                    }

                    // Si on peut placer la tâche dans cette ligne
                    if (canPlaceInTrack)
                    {
                        track.Add(task);
                        placed = true;
                        break;
                    }
                }

                // Si la tâche n'a pas été placée, créer une nouvelle ligne
                if (!placed)
                {
                    var newTrack = new List<GanttTaskViewModel> { task };
                    tracks.Add(newTrack);
                }
            }

            // 4. Vérification finale pour s'assurer qu'aucun chevauchement ne reste
            var finalTracks = new List<List<GanttTaskViewModel>>();

            foreach (var track in tracks)
            {
                // Organiser les tâches par date de début
                var sortedTrack = track.OrderBy(t => t.StartDate).ToList();
                var validTrack = new List<GanttTaskViewModel>();

                foreach (var task in sortedTrack)
                {
                    bool canAdd = true;

                    foreach (var placedTask in validTrack)
                    {
                        // Utiliser la méthode améliorée de vérification de chevauchement
                        if (CheckOverlap(task, placedTask))
                        {
                            canAdd = false;

                            // Créer une nouvelle ligne pour cette tâche qui ne peut pas être placée
                            var newTrack = new List<GanttTaskViewModel> { task };
                            finalTracks.Add(newTrack);
                            break;
                        }
                    }

                    if (canAdd)
                    {
                        validTrack.Add(task);
                    }
                }

                if (validTrack.Any())
                {
                    finalTracks.Add(validTrack);
                }
            }

            // 5. Aplatir les lignes dans une seule liste
            int rowIndex = 0;
            foreach (var track in finalTracks)
            {
                foreach (var task in track.OrderBy(t => t.StartDate))
                {
                    task.RowIndex = rowIndex;
                    result.Add(task);
                }
                rowIndex++;
            }

            return result;
        }

        // Nouvelle méthode améliorée pour vérifier les chevauchements entre deux tâches
        private bool CheckOverlap(GanttTaskViewModel task1, GanttTaskViewModel task2)
        {
            // Calcul des dates de fin pour les deux tâches
            DateTime task1End;
            DateTime task2End;

            // Calculer la fin de la tâche 1
            if (task1.DueDate.HasValue)
                task1End = task1.DueDate.Value;
            else
                task1End = WorkdayCalculator.AddWorkdays(task1.StartDate, (int)Math.Ceiling(task1.PlannedTimeDays));

            // Calculer la fin de la tâche 2
            if (task2.DueDate.HasValue)
                task2End = task2.DueDate.Value;
            else
                task2End = WorkdayCalculator.AddWorkdays(task2.StartDate, (int)Math.Ceiling(task2.PlannedTimeDays));

            // Vérification standard de chevauchement de périodes
            bool standardOverlap = (task1.StartDate < task2End && task2.StartDate < task1End);

            // Vérification supplémentaire pour les tâches avec dates d'échéance
            bool dueDateOverlap = false;

            // Si les deux tâches ont des dates d'échéance, vérifier si elles sont pour le même jour
            if (task1.DueDate.HasValue && task2.DueDate.HasValue)
            {
                // Si les dates d'échéance sont le même jour ou à +/- 1 jour, considérer comme chevauchement
                TimeSpan dueDateDifference = (task1.DueDate.Value - task2.DueDate.Value).Duration();
                if (dueDateDifference.TotalDays <= 1)
                {
                    dueDateOverlap = true;
                }
            }

            // Si une tâche a une date d'échéance et l'autre une date de début proche de cette échéance
            if (task1.DueDate.HasValue &&
                (task2.StartDate.Date == task1.DueDate.Value.Date ||
                 (task2.StartDate - task1.DueDate.Value).TotalDays <= 1))
            {
                dueDateOverlap = true;
            }

            if (task2.DueDate.HasValue &&
                (task1.StartDate.Date == task2.DueDate.Value.Date ||
                 (task1.StartDate - task2.DueDate.Value).TotalDays <= 1))
            {
                dueDateOverlap = true;
            }

            // Considérer un chevauchement si au moins une des conditions est vraie
            return standardOverlap || dueDateOverlap;
        }

        private GanttTaskViewModel CreateGanttTask(TaskModel task)
        {
            DateTime startDate;

            // Déterminer la date de début la plus appropriée
            if (task.StartDate.HasValue)
            {
                // Si la tâche a une date de début, l'utiliser
                startDate = task.StartDate.Value;
            }
            else if (task.DueDate.HasValue)
            {
                // Si pas de date de début mais une date d'échéance, calculer la date de début 
                // en soustrayant la durée planifiée de la date d'échéance
                double plannedDays = Math.Max(task.PlannedTimeDays, 1.0);

                // Calculer la date de début en jours ouvrables
                startDate = WorkdayCalculator.AddWorkdays(task.DueDate.Value, -((int)Math.Ceiling(plannedDays)));
            }
            else
            {
                // Si aucune tâche n'est spécifiée, cette tâche ne devrait pas être dans le Gantt
                return null;
            }

            // S'assurer que la date de début est précise au jour près (sans heures/minutes/secondes)
            startDate = startDate.Date;

            // S'assurer que la date de début est un jour ouvrable
            if (!WorkdayCalculator.IsWorkday(startDate))
            {
                startDate = WorkdayCalculator.AdjustToWorkday(startDate);
            }

            // Calculer les paramètres de positionnement
            double pixelsPerUnit = GetPixelsPerUnit();

            // Avec le mode jours, calculer le nombre de jours ouvrables entre _minDate et startDate
            int businessDaysFromStart = 0;
            var selectedScale = TimeScaleComboBox.SelectedItem as ComboBoxItem;
            if (selectedScale?.Content?.ToString() == "Jours")
            {
                // Compter uniquement les jours ouvrables entre _minDate et startDate
                businessDaysFromStart = WorkdayCalculator.GetWorkdayCount(_minDate, startDate);
            }
            else
            {
                // Pour les autres échelles, utiliser le calcul original
                businessDaysFromStart = (int)(startDate - _minDate).TotalDays;
            }

            double offset = Math.Max(businessDaysFromStart * pixelsPerUnit, 0); // Éviter les valeurs négatives

            // Assurer une durée minimale pour la visibilité
            double durationDays = Math.Max(task.PlannedTimeDays, 1.0);

            // Si nous sommes en mode jours, utiliser seulement les jours ouvrables pour la durée
            double duration;
            if (selectedScale?.Content?.ToString() == "Jours")
            {
                // La durée visuellement représente uniquement les jours ouvrables
                duration = durationDays * pixelsPerUnit;
            }
            else
            {
                // Pour les autres échelles, convertir les jours ouvrables en jours calendaires
                int calendarDays = WorkdayCalculator.WorkdaysToCalendarDays(startDate, (int)Math.Ceiling(durationDays));
                duration = calendarDays * pixelsPerUnit;
            }

            // Déterminer le type de tâche (en cours ou date prévue)
            bool isInProgress = _viewModel.InProgressTasks.Contains(task);

            // Créer l'objet GanttTaskViewModel
            return new GanttTaskViewModel
            {
                Title = task.Title ?? "",
                Client = task.Client ?? "",
                Description = task.Description ?? "",
                StartDate = startDate,
                DueDate = task.DueDate.HasValue ? task.DueDate.Value.Date : (DateTime?)null, // Normaliser la date d'échéance
                RequestedDate = task.RequestedDate,
                PlannedTimeDays = durationDays,
                Duration = duration,
                StartOffset = offset,
                OriginalTask = task,
                IsInProgress = isInProgress  // Nouvelle propriété pour indiquer si la tâche est en cours
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
                    // Ne montrer que les jours ouvrables (lundi-vendredi)
                    for (var date = _minDate.Date; date <= _maxDate.Date; date = date.AddDays(1))
                    {
                        // N'ajouter que les jours ouvrables
                        if (WorkdayCalculator.IsWorkday(date))
                        {
                            days.Add(date.ToString("dd/MM"));
                        }
                    }
                    break;

                case "Semaines":
                    // Utiliser le format de semaine ISO (numéro de semaine dans l'année)
                    for (var date = _minDate.Date.AddDays(-(int)_minDate.DayOfWeek + 1); // Commencer lundi
                         date <= _maxDate.Date;
                         date = date.AddDays(7))
                    {
                        // Obtenir le numéro de semaine selon ISO 8601
                        System.Globalization.Calendar cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
                        int weekNum = cal.GetWeekOfYear(
                            date,
                            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                            DayOfWeek.Monday);

                        days.Add($"S{weekNum}");
                    }
                    break;

                case "Mois":
                    for (var date = new DateTime(_minDate.Year, _minDate.Month, 1);
                         date <= _maxDate;
                         date = date.AddMonths(1))
                        days.Add(date.ToString("MM/yyyy"));
                    break;
            }

            // Ajuster la largeur du canvas de Gantt en fonction du nombre de jours
            double pixelsPerUnit = GetPixelsPerUnit();
            double totalWidth = days.Count * pixelsPerUnit;

            // Définir une largeur minimale pour éviter les problèmes d'affichage
            totalWidth = Math.Max(totalWidth, 1000);

            // Ajuster la largeur du canvas pour s'adapter exactement au nombre d'unités de temps
            GanttCanvas.Width = totalWidth;

            // Mettre à jour la valeur Tag pour la largeur des colonnes de l'échelle de temps
            TimeScaleItemsControl.Tag = pixelsPerUnit;

            TimeScaleItemsControl.ItemsSource = days;
        }

        private void TimeScaleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PrepareGanttTasks();
            ConfigureTimeScale();
        }

        private void ShowDescriptionCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Ajuster la hauteur des barres de Gantt en fonction de l'affichage de la description
            UpdateTaskHeights();
        }

        private double GetPixelsPerUnit()
        {
            var selectedScale = TimeScaleComboBox.SelectedItem as ComboBoxItem;
            switch (selectedScale?.Content?.ToString())
            {
                case "Heures": return 2;
                case "Jours": return 50; // Un jour ouvrable = 50 pixels
                case "Semaines": return 150;
                case "Mois": return 200;
                default: return 50;
            }
        }

        private void GanttTasksItemsControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var hitTestResult = VisualTreeHelper.HitTest(GanttTasksItemsControl, e.GetPosition(GanttTasksItemsControl));
            if (hitTestResult == null) return;

            var container = FindParent<Border>(hitTestResult.VisualHit);
            if (container == null) return;

            var task = container.DataContext as GanttTaskViewModel;
            if (task == null) return;

            OpenTaskEditWindow(task);
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
            // Mettre à jour le modèle original via le ViewModel
            var comparer = new TodoTaskComparer();
            comparer.UpdateOriginalTask(_viewModel, new TaskWithSortDate(ganttTask.OriginalTask), updatedTask);

            // Mettre à jour uniquement la tâche modifiée
            UpdateSingleTask(ganttTask);
        }

        private void UpdateSingleTask(GanttTaskViewModel ganttTask)
        {
            // 1. Mémoriser l'index de la tâche dans la collection
            int taskIndex = GanttTasks.IndexOf(ganttTask);
            if (taskIndex < 0) return; // La tâche n'existe plus dans la collection

            // 2. Recréer la tâche Gantt à partir du modèle mis à jour
            var updatedGanttTask = CreateGanttTask(ganttTask.OriginalTask);
            if (updatedGanttTask == null) return;

            // 3. Préserver la propriété IsHighlighted
            updatedGanttTask.IsHighlighted = ganttTask.IsHighlighted;

            // 4. Si les dates ou la durée ont changé, nous devons réorganiser toutes les tâches
            if (ganttTask.StartDate != updatedGanttTask.StartDate ||
                ganttTask.PlannedTimeDays != updatedGanttTask.PlannedTimeDays)
            {
                // Régénérer toutes les tâches car l'organisation a potentiellement changé
                PrepareGanttTasks();
                ConfigureTimeScale();
                return;
            }

            // 5. Si seulement d'autres propriétés ont changé (titre, client, description, etc.)
            // Nous pouvons mettre à jour uniquement cette tâche sans réorganiser
            GanttTasks[taskIndex] = updatedGanttTask;

            // 6. Notifier le changement pour mettre à jour l'UI
            updatedGanttTask.OnPropertyChanged("Title");
            updatedGanttTask.OnPropertyChanged("Client");
            updatedGanttTask.OnPropertyChanged("Description");
        }

        // Synchroniser le défilement horizontal entre les scrollviewers
        private void GanttScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isScrollChanging) return;
            _isScrollChanging = true;

            try
            {
                if (e.VerticalChange != 0)
                {
                    TaskInfoScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
                }

                if (e.HorizontalChange != 0)
                {
                    TimelineScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
                }
            }
            finally
            {
                _isScrollChanging = false;
            }
        }

        // Gestion des événements pour les barres de tâches Gantt
        private void GanttTask_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is GanttTaskViewModel task)
            {
                task.IsHighlighted = true;
            }
        }

        private void GanttTask_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is GanttTaskViewModel task)
            {
                task.IsHighlighted = false;
            }
        }

        // Gestion des événements pour la colonne des informations de tâche
        private void TaskInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is GanttTaskViewModel task)
            {
                task.IsHighlighted = true;
            }
        }

        private void TaskInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is GanttTaskViewModel task)
            {
                task.IsHighlighted = false;
            }
        }

        private void TaskInfo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Vérifier s'il s'agit d'un double clic
            if (e.ClickCount != 2) return;

            if (sender is Border border && border.DataContext is GanttTaskViewModel task)
            {
                OpenTaskEditWindow(task);
            }
        }

        private void OpenTaskEditWindow(GanttTaskViewModel task)
        {
            var editWindow = new KanbanView(task.OriginalTask);
            if (editWindow.ShowDialog() == true && editWindow.CreatedTask != null)
            {
                // Vérifier si les dates ont changé
                bool dateChanged = HasDateChanged(task.OriginalTask, editWindow.CreatedTask);

                // Mettre à jour la tâche
                UpdateTask(task, editWindow.CreatedTask);

                // Si une date a changé, faire défiler jusqu'à la nouvelle position
                if (dateChanged && editWindow.CreatedTask.StartDate.HasValue)
                {
                    ScrollToDate(editWindow.CreatedTask.StartDate.Value);
                }
            }
        }

        private bool HasDateChanged(TaskModel original, TaskModel updated)
        {
            // Vérifier si les dates pertinentes pour le Gantt ont changé
            return !DateTimeEquals(original.StartDate, updated.StartDate) ||
                   !DateTimeEquals(original.DueDate, updated.DueDate) ||
                   original.PlannedTimeDays != updated.PlannedTimeDays;
        }

        private bool DateTimeEquals(DateTime? date1, DateTime? date2)
        {
            if (date1 == null && date2 == null) return true;
            if (date1 == null || date2 == null) return false;

            return date1.Value.Date == date2.Value.Date;
        }

        public void ScrollToDate(DateTime date)
        {
            // Vérifier que la date est dans la plage visible
            if (date < _minDate || date > _maxDate)
                return;

            var selectedScale = TimeScaleComboBox.SelectedItem as ComboBoxItem;
            if (selectedScale == null)
                return;

            double pixelsPerUnit = GetPixelsPerUnit();
            double offset = 0;

            switch (selectedScale.Content?.ToString())
            {
                case "Jours":
                    // Calculer le nombre de jours ouvrables entre _minDate et la date cible
                    int businessDays = WorkdayCalculator.GetWorkdayCount(_minDate, date);
                    offset = businessDays * pixelsPerUnit;
                    break;

                case "Semaines":
                    // Calculer le nombre de semaines entre _minDate et la date cible
                    TimeSpan span = date - _minDate;
                    double weeks = span.TotalDays / 7;
                    offset = weeks * pixelsPerUnit;
                    break;

                case "Mois":
                    // Calculer le nombre de mois entre _minDate et la date cible
                    int months = ((date.Year - _minDate.Year) * 12) + date.Month - _minDate.Month;
                    offset = months * pixelsPerUnit;
                    break;

                default:
                    TimeSpan diff = date - _minDate;
                    offset = diff.TotalDays * pixelsPerUnit;
                    break;
            }

            // Ajuster le défilement pour centrer la date dans la vue
            double viewportWidth = GanttScrollViewer.ViewportWidth;
            offset = Math.Max(0, offset - (viewportWidth / 2));

            // Appliquer le défilement
            GanttScrollViewer.ScrollToHorizontalOffset(offset);
        }

        // Ajout de la méthode manquante ExportGanttButton_Click
        private void ExportGanttButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Configurer la boîte de dialogue de sauvegarde de fichier
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Fichiers PDF (*.pdf)|*.pdf",
                    Title = "Enregistrer le diagramme de Gantt",
                    FileName = "Planification_Gantt_Operationnelle.pdf"
                };

                // Afficher la boîte de dialogue et vérifier si l'utilisateur a cliqué sur OK
                if (saveFileDialog.ShowDialog() == true)
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    // Créer un nouveau document PDF
                    PdfDocument document = new PdfDocument();
                    document.Info.Title = "Planification Gantt Opérationnelle";
                    document.Info.Author = "PlanifKanban";
                    document.Info.Subject = "Diagramme de Gantt des tâches";

                    // Ajouter une page au format paysage
                    PdfPage page = document.AddPage();
                    page.Orientation = PdfSharp.PageOrientation.Landscape;
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    // Définir les polices sans style
                    XFont titleFont = new XFont("Arial", 20);
                    XFont normalFont = new XFont("Arial", 10);
                    XFont smallFont = new XFont("Arial", 8);  // Police plus petite pour les dates/textes compressés
                    XFont taskTitleFont = new XFont("Arial", 11);
                    XFont footerFont = new XFont("Arial", 8);

                    // Couleurs
                    XColor titleColor = XColor.FromArgb(44, 62, 80); // SecondaryColor
                    XColor primaryColor = XColor.FromArgb(59, 124, 212); // PrimaryColor
                    XColor warningColor = XColor.FromArgb(243, 156, 18); // WarningColor
                    XColor gridColor = XColor.FromArgb(200, 200, 200); // Couleur grise pour les grilles
                    XColor alternateRowColor = XColor.FromArgb(240, 240, 240); // Couleur pour les lignes alternées

                    // Définir les marges et dimensions
                    double margin = 50;
                    double currentY = margin;
                    double infoColumnWidth = 250; // Réduire légèrement pour donner plus d'espace au Gantt
                    double ganttStartX = infoColumnWidth + margin; // Position X où commence le diagramme Gantt

                    // Dessiner le titre
                    gfx.DrawString("Planification Gantt Opérationnelle", titleFont, new XSolidBrush(titleColor),
                                   new XRect(margin, currentY, page.Width - 2 * margin, 30),
                                   XStringFormats.TopCenter);
                    currentY += 40;

                    // Ajouter la date d'export
                    gfx.DrawString($"Exporté le {DateTime.Now:dd/MM/yyyy à HH:mm}", normalFont, XBrushes.DarkGray,
                                   new XRect(margin, currentY, page.Width - 2 * margin, 20),
                                   XStringFormats.TopRight);
                    currentY += 30;

                    // Déterminer les paramètres d'échelle de temps
                    var selectedScale = TimeScaleComboBox.SelectedItem as ComboBoxItem;
                    string timeScaleType = selectedScale?.Content?.ToString() ?? "Jours";
                    gfx.DrawString($"Échelle : {timeScaleType}", normalFont, XBrushes.DarkGray,
                                  new XRect(margin, currentY, 200, 20),
                                  XStringFormats.TopLeft);
                    currentY += 20;

                    // Dessiner les en-têtes des colonnes
                    double headerHeight = 30;
                    gfx.DrawRectangle(new XSolidBrush(primaryColor), margin, currentY, infoColumnWidth, headerHeight);
                    gfx.DrawString("Client / Tâche", normalFont, XBrushes.White,
                                  new XRect(margin + 5, currentY, infoColumnWidth - 10, headerHeight),
                                  XStringFormats.CenterLeft);

                    // Calculer l'échelle de temps et adapter en fonction de la tâche la plus longue
                    double timeScaleWidth = page.Width - ganttStartX - margin;
                    gfx.DrawRectangle(new XSolidBrush(primaryColor), ganttStartX, currentY, timeScaleWidth, headerHeight);

                    // Obtenir les échelles de temps depuis l'UI
                    var timeScaleItems = TimeScaleItemsControl.ItemsSource as IEnumerable<string>;

                    // Calculer un facteur d'échelle pour adapter la vue Gantt à la tâche la plus longue
                    double scaleFactor = 1.0;
                    double maxTaskWidth = 0;

                    // Trouver la tâche la plus longue et la plus éloignée
                    if (GanttTasks != null && GanttTasks.Any())
                    {
                        double maxEndOffset = GanttTasks.Max(t => t.StartOffset + t.Duration);

                        // Si la largeur maximale dépasse l'espace disponible, calculer un facteur d'échelle
                        if (maxEndOffset > timeScaleWidth * 0.9) // Utiliser 90% de l'espace disponible
                        {
                            scaleFactor = (timeScaleWidth * 0.9) / maxEndOffset;
                        }

                        maxTaskWidth = GanttTasks.Max(t => t.Duration) * scaleFactor;
                    }

                    // Afficher l'échelle de temps en tenant compte du facteur d'échelle
                    if (timeScaleItems != null && timeScaleItems.Any())
                    {
                        int count = timeScaleItems.Count();

                        // Ajuster l'espacement des unités de temps en fonction du nombre d'unités
                        double unitWidth = timeScaleWidth / count;

                        // Choisir la police en fonction de l'espace disponible
                        XFont timeScaleFont = (count > 15) ? smallFont : normalFont;

                        int index = 0;
                        foreach (var unit in timeScaleItems)
                        {
                            double unitX = ganttStartX + index * unitWidth;

                            // Dessiner la ligne de séparation verticale
                            gfx.DrawLine(new XPen(XColors.White, 0.5), unitX, currentY, unitX, currentY + headerHeight);

                            // Dessiner le texte d'échelle, compressé si nécessaire
                            string displayUnit = unit;
                            if (count > 30 && unit.Length > 4)
                            {
                                // Abréger le texte si beaucoup d'unités
                                displayUnit = unit.Substring(0, 4);
                            }

                            gfx.DrawString(displayUnit, timeScaleFont, XBrushes.White,
                                          new XRect(unitX, currentY, unitWidth, headerHeight),
                                          XStringFormats.Center);
                            index++;
                        }
                    }

                    currentY += headerHeight;

                    // Dessiner chaque tâche avec adaptation d'échelle
                    double taskHeight = 40;
                    bool alternateRow = false;

                    foreach (var task in GanttTasks)
                    {
                        // Dessiner la zone d'info tâche (côté gauche)
                        XBrush rowBrush = alternateRow ? new XSolidBrush(alternateRowColor) : XBrushes.White;
                        gfx.DrawRectangle(rowBrush, margin, currentY, infoColumnWidth, taskHeight);
                        gfx.DrawRectangle(new XPen(gridColor, 0.5), margin, currentY, infoColumnWidth, taskHeight);

                        // Client et titre de la tâche
                        gfx.DrawString(task.Client, taskTitleFont, XBrushes.Black,
                                       new XRect(margin + 5, currentY + 5, infoColumnWidth - 10, 15),
                                       XStringFormats.TopLeft);

                        // Déterminer si le titre est long et adapter la police si nécessaire
                        XFont taskDisplayFont = (task.Title?.Length > 30) ? smallFont : normalFont;

                        gfx.DrawString(task.Title, taskDisplayFont, XBrushes.Black,
                                        new XRect(margin + 5, currentY + 25, infoColumnWidth - 10, taskHeight - 25),
                                        XStringFormats.TopLeft);

                        // Dessiner la zone du diagramme de Gantt (côté droit)
                        gfx.DrawRectangle(rowBrush, ganttStartX, currentY, timeScaleWidth, taskHeight);
                        gfx.DrawRectangle(new XPen(gridColor, 0.5), ganttStartX, currentY, timeScaleWidth, taskHeight);

                        // Dessiner la barre de Gantt avec adaptation d'échelle
                        double barWidth = task.Duration * scaleFactor;
                        double barX = ganttStartX + (task.StartOffset * scaleFactor);

                        // Couleur de la barre selon le statut
                        XBrush barBrush = task.IsInProgress ?
                                         new XSolidBrush(warningColor) :
                                         new XSolidBrush(primaryColor);

                        // S'assurer que la barre a une largeur minimale visible
                        barWidth = Math.Max(barWidth, 5);

                        // Dessiner la barre de Gantt
                        gfx.DrawRoundedRectangle(barBrush,
                                                barX, currentY + 5,
                                                barWidth, taskHeight - 10,
                                                5, 5);

                        // Ajouter des informations de date et durée
                        XFont dateFont = (barWidth < 80) ? smallFont : normalFont;

                        // Ajouter du texte dans la barre si assez large
                        if (barWidth > 30)
                        {
                            string durationText = task.PlannedTimeDays.ToString("0.#") + " j";

                            // Si la barre est assez large, ajouter la date de début
                            if (barWidth > 80)
                            {
                                durationText = $"{task.StartDate:dd/MM} - {durationText}";
                            }

                            gfx.DrawString(durationText,
                                          dateFont, XBrushes.White,
                                          new XRect(barX + 5, currentY + 5, barWidth - 10, taskHeight - 10),
                                          XStringFormats.CenterLeft);
                        }
                        else if (task.PlannedTimeDays >= 2)
                        {
                            // Si la barre est trop étroite mais la tâche dure plusieurs jours,
                            // afficher la durée à côté de la barre
                            gfx.DrawString(task.PlannedTimeDays.ToString("0.#") + "j",
                                          smallFont, XBrushes.Black,
                                          new XRect(barX + barWidth + 3, currentY + (taskHeight / 2) - 5, 20, 10),
                                          XStringFormats.CenterLeft);
                        }

                        // Passer à la tâche suivante
                        currentY += taskHeight;
                        alternateRow = !alternateRow;

                        // Si on dépasse la page, créer une nouvelle page
                        if (currentY > page.Height - margin - 50)
                        {
                            // Ajouter une note de bas de page
                            gfx.DrawString("Suite sur la page suivante...", footerFont, XBrushes.Gray,
                                          new XRect(margin, page.Height - 20, page.Width - 2 * margin, 20),
                                          XStringFormats.Center);

                            // Nouvelle page
                            page = document.AddPage();
                            page.Orientation = PdfSharp.PageOrientation.Landscape;
                            gfx = XGraphics.FromPdfPage(page);
                            currentY = margin;

                            // Répéter les en-têtes
                            gfx.DrawString("Planification Gantt Opérationnelle (suite)", titleFont, new XSolidBrush(titleColor),
                                          new XRect(margin, currentY, page.Width - 2 * margin, 30),
                                          XStringFormats.TopCenter);
                            currentY += 40;

                            // Répéter les en-têtes de colonnes
                            gfx.DrawRectangle(new XSolidBrush(primaryColor), margin, currentY, infoColumnWidth, headerHeight);
                            gfx.DrawString("Client / Tâche", normalFont, XBrushes.White,
                                          new XRect(margin + 5, currentY, infoColumnWidth - 10, headerHeight),
                                          XStringFormats.CenterLeft);

                            gfx.DrawRectangle(new XSolidBrush(primaryColor), ganttStartX, currentY, timeScaleWidth, headerHeight);

                            // Redessiner l'échelle de temps sur la nouvelle page
                            if (timeScaleItems != null && timeScaleItems.Any())
                            {
                                int count = timeScaleItems.Count();
                                double unitWidth = timeScaleWidth / count;
                                XFont timeScaleFont = (count > 15) ? smallFont : normalFont;

                                int index = 0;
                                foreach (var unit in timeScaleItems)
                                {
                                    double unitX = ganttStartX + index * unitWidth;
                                    gfx.DrawLine(new XPen(XColors.White, 0.5), unitX, currentY, unitX, currentY + headerHeight);

                                    string displayUnit = unit;
                                    if (count > 30 && unit.Length > 4)
                                    {
                                        displayUnit = unit.Substring(0, 4);
                                    }

                                    gfx.DrawString(displayUnit, timeScaleFont, XBrushes.White,
                                                  new XRect(unitX, currentY, unitWidth, headerHeight),
                                                  XStringFormats.Center);
                                    index++;
                                }
                            }

                            currentY += headerHeight;
                        }
                    }

                    // Ajouter une légende
                    currentY += 20;
                    double legendRectSize = 15;
                    double legendX = margin;

                    // Légende pour les tâches en cours (orange)
                    gfx.DrawRoundedRectangle(new XSolidBrush(warningColor), legendX, currentY, legendRectSize, legendRectSize, 3, 3);
                    gfx.DrawString("Tâche en cours", normalFont, XBrushes.Black,
                                  legendX + legendRectSize + 5, currentY + legendRectSize - 2);

                    // Légende pour les tâches planifiées (bleu)
                    legendX += 150;
                    gfx.DrawRoundedRectangle(new XSolidBrush(primaryColor), legendX, currentY, legendRectSize, legendRectSize, 3, 3);
                    gfx.DrawString("Tâche planifiée", normalFont, XBrushes.Black,
                                  legendX + legendRectSize + 5, currentY + legendRectSize - 2);

                    // Ajouter une information sur l'échelle
                    if (scaleFactor < 0.9)
                    {
                        legendX += 150;
                        gfx.DrawString($"Échelle : {scaleFactor:0.##}x", normalFont, XBrushes.DarkGray,
                                      legendX + 5, currentY + legendRectSize - 2);
                    }

                    // Ajouter une note de bas de page
                    gfx.DrawString("Document généré automatiquement par PlanifKanban", footerFont, XBrushes.Gray,
                                  new XRect(margin, page.Height - 20, page.Width - 2 * margin, 20),
                                  XStringFormats.TopCenter);

                    // Enregistrer le document
                    document.Save(saveFileDialog.FileName);

                    // Message de succès
                    MessageBoxResult result = MessageBox.Show(
                        "Le PDF a été enregistré avec succès. Voulez-vous l'ouvrir maintenant ?",
                        "Exportation réussie",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'exportation du PDF : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }
}