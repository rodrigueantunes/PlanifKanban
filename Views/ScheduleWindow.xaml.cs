using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using PlanifKanban.Models;
using PlanifKanban.ViewModels;
using System.Windows.Threading;
using Microsoft.Win32;
using ClosedXML.Excel;
using System.IO;

namespace PlanifKanban.Views
{
    // Classe pour étendre TaskModel avec une propriété SortDate calculée
    public class TaskWithSortDate : TaskModel, INotifyPropertyChanged
    {
        private readonly TaskModel _originalTask;

        public new event PropertyChangedEventHandler PropertyChanged;

        public DateTime? SortDate
        {
            get
            {
                // Utiliser la date de début si elle existe, sinon la date prévue
                return StartDate ?? DueDate;
            }
        }

        // Propriété pour déterminer si la tâche est "à faire" (sans date prévue ni date de début)
        public bool IsTodoOnly
        {
            get
            {
                // Une tâche est considérée "à faire" uniquement si elle n'a ni date de début ni date prévue
                // La présence d'une date demandée ne change pas cette classification
                return !StartDate.HasValue && !DueDate.HasValue && !CompletionDate.HasValue;
            }
        }

        // Propriété pour déterminer si la tâche est "en cours ou en test" (avec date prévue ou date de début)
        public bool IsInProgressOrTesting
        {
            get
            {
                // Une tâche est "en cours ou en test" uniquement si elle a une date de début OU une date prévue
                // et qu'elle n'est pas encore terminée
                return (StartDate.HasValue || DueDate.HasValue) && !CompletionDate.HasValue;
            }
        }

        public bool HasRequestedDate
        {
            get
            {
                return RequestedDate.HasValue;
            }
        }

        public TaskWithSortDate(TaskModel task)
        {
            _originalTask = task;
            // Copier toutes les propriétés
            Title = task.Title;
            Client = task.Client;
            Description = task.Description;
            StartDate = task.StartDate;
            DueDate = task.DueDate;
            RequestedDate = task.RequestedDate;
            CompletionDate = task.CompletionDate;
            PlannedTimeDays = task.PlannedTimeDays;
            PlannedTimeHours = task.PlannedTimeHours;
            ActualTimeDays = task.ActualTimeDays;
            ActualTimeHours = task.ActualTimeHours;
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class ScheduleWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private KanbanViewModel _viewModel;
        private ICollectionView _activeTasksView;
        private ICollectionView _todoTasksView;
        private ICollectionView _completedTasksView;

        // Directions de tri pour les colonnes de la liste active
        private string _activeClientSortDirection = "";
        private string _activeTitleSortDirection = "";
        private string _activeDescriptionSortDirection = "";
        private string _activeRequestedDateSortDirection = "";
        private string _activeDueDateSortDirection = "";
        private string _activeStartDateSortDirection = "";
        private string _activeSortDateSortDirection = "Ascending"; // Par défaut trié ascendant sur SortDate
        private string _activePlannedTimeDaysSortDirection = "";

        // Directions de tri pour les colonnes de la liste des tâches à faire
        private string _todoClientSortDirection = "";
        private string _todoTitleSortDirection = "";
        private string _todoDescriptionSortDirection = "";
        private string _todoRequestedDateSortDirection = "";
        private string _todoDueDateSortDirection = "";

        // Directions de tri pour les colonnes de la liste terminée
        private string _completedClientSortDirection = "";
        private string _completedTitleSortDirection = "";
        private string _completedDescriptionSortDirection = "";
        private string _completedRequestedDateSortDirection = "";
        private string _completedDueDateSortDirection = "";
        private string _completedStartDateSortDirection = "";
        private string _completedSortDateSortDirection = "";
        private string _completedPlannedTimeDaysSortDirection = "";
        private string _completedActualTimeDaysSortDirection = "";
        private string _completedCompletionDateSortDirection = "Ascending"; // Par défaut trié ascendant sur CompletionDate

        // Propriétés pour les directions de tri de la liste active
        public string ActiveClientSortDirection
        {
            get { return _activeClientSortDirection; }
            set { _activeClientSortDirection = value; NotifyPropertyChanged(nameof(ActiveClientSortDirection)); }
        }

        public string ActiveTitleSortDirection
        {
            get { return _activeTitleSortDirection; }
            set { _activeTitleSortDirection = value; NotifyPropertyChanged(nameof(ActiveTitleSortDirection)); }
        }

        public string ActiveDescriptionSortDirection
        {
            get { return _activeDescriptionSortDirection; }
            set { _activeDescriptionSortDirection = value; NotifyPropertyChanged(nameof(ActiveDescriptionSortDirection)); }
        }

        public string ActiveRequestedDateSortDirection
        {
            get { return _activeRequestedDateSortDirection; }
            set { _activeRequestedDateSortDirection = value; NotifyPropertyChanged(nameof(ActiveRequestedDateSortDirection)); }
        }

        public string ActiveDueDateSortDirection
        {
            get { return _activeDueDateSortDirection; }
            set { _activeDueDateSortDirection = value; NotifyPropertyChanged(nameof(ActiveDueDateSortDirection)); }
        }

        public string ActiveStartDateSortDirection
        {
            get { return _activeStartDateSortDirection; }
            set { _activeStartDateSortDirection = value; NotifyPropertyChanged(nameof(ActiveStartDateSortDirection)); }
        }

        public string ActiveSortDateSortDirection
        {
            get { return _activeSortDateSortDirection; }
            set { _activeSortDateSortDirection = value; NotifyPropertyChanged(nameof(ActiveSortDateSortDirection)); }
        }

        public string ActivePlannedTimeDaysSortDirection
        {
            get { return _activePlannedTimeDaysSortDirection; }
            set { _activePlannedTimeDaysSortDirection = value; NotifyPropertyChanged(nameof(ActivePlannedTimeDaysSortDirection)); }
        }

        // Propriétés pour les tâches à faire
        public string TodoClientSortDirection
        {
            get { return _todoClientSortDirection; }
            set { _todoClientSortDirection = value; NotifyPropertyChanged(nameof(TodoClientSortDirection)); }
        }

        public string TodoTitleSortDirection
        {
            get { return _todoTitleSortDirection; }
            set { _todoTitleSortDirection = value; NotifyPropertyChanged(nameof(TodoTitleSortDirection)); }
        }

        public string TodoDescriptionSortDirection
        {
            get { return _todoDescriptionSortDirection; }
            set { _todoDescriptionSortDirection = value; NotifyPropertyChanged(nameof(TodoDescriptionSortDirection)); }
        }

        public string TodoRequestedDateSortDirection
        {
            get { return _todoRequestedDateSortDirection; }
            set { _todoRequestedDateSortDirection = value; NotifyPropertyChanged(nameof(TodoRequestedDateSortDirection)); }
        }

        public string TodoDueDateSortDirection
        {
            get { return _todoDueDateSortDirection; }
            set { _todoDueDateSortDirection = value; NotifyPropertyChanged(nameof(TodoDueDateSortDirection)); }
        }

        // Propriétés pour les tâches terminées
        public string CompletedClientSortDirection
        {
            get { return _completedClientSortDirection; }
            set { _completedClientSortDirection = value; NotifyPropertyChanged(nameof(CompletedClientSortDirection)); }
        }

        public string CompletedTitleSortDirection
        {
            get { return _completedTitleSortDirection; }
            set { _completedTitleSortDirection = value; NotifyPropertyChanged(nameof(CompletedTitleSortDirection)); }
        }

        public string CompletedDescriptionSortDirection
        {
            get { return _completedDescriptionSortDirection; }
            set { _completedDescriptionSortDirection = value; NotifyPropertyChanged(nameof(CompletedDescriptionSortDirection)); }
        }

        public string CompletedRequestedDateSortDirection
        {
            get { return _completedRequestedDateSortDirection; }
            set { _completedRequestedDateSortDirection = value; NotifyPropertyChanged(nameof(CompletedRequestedDateSortDirection)); }
        }

        public string CompletedDueDateSortDirection
        {
            get { return _completedDueDateSortDirection; }
            set { _completedDueDateSortDirection = value; NotifyPropertyChanged(nameof(CompletedDueDateSortDirection)); }
        }

        public string CompletedStartDateSortDirection
        {
            get { return _completedStartDateSortDirection; }
            set { _completedStartDateSortDirection = value; NotifyPropertyChanged(nameof(CompletedStartDateSortDirection)); }
        }

        public string CompletedSortDateSortDirection
        {
            get { return _completedSortDateSortDirection; }
            set { _completedSortDateSortDirection = value; NotifyPropertyChanged(nameof(CompletedSortDateSortDirection)); }
        }

        public string CompletedPlannedTimeDaysSortDirection
        {
            get { return _completedPlannedTimeDaysSortDirection; }
            set { _completedPlannedTimeDaysSortDirection = value; NotifyPropertyChanged(nameof(CompletedPlannedTimeDaysSortDirection)); }
        }

        public string CompletedActualTimeDaysSortDirection
        {
            get { return _completedActualTimeDaysSortDirection; }
            set { _completedActualTimeDaysSortDirection = value; NotifyPropertyChanged(nameof(CompletedActualTimeDaysSortDirection)); }
        }

        public string CompletedCompletionDateSortDirection
        {
            get { return _completedCompletionDateSortDirection; }
            set { _completedCompletionDateSortDirection = value; NotifyPropertyChanged(nameof(CompletedCompletionDateSortDirection)); }
        }

        public ScheduleWindow(KanbanViewModel viewModel)
        {
            InitializeComponent();
            DataContext = this;
            _viewModel = viewModel;

            // Préparer les collections de tâches
            PrepareTasksCollections();

            // Appliquer le tri par défaut
            ApplyDefaultSorting();
        }

        private void PrepareTasksCollections()
        {
            try
            {
                // Désactiver temporairement les liens de données
                if (ActiveTasksGrid != null)
                    ActiveTasksGrid.ItemsSource = null;
                if (TodoTasksGrid != null)
                    TodoTasksGrid.ItemsSource = null;
                if (CompletedTasksGrid != null)
                    CompletedTasksGrid.ItemsSource = null;

                var allTasks = _viewModel.TodoTasks.Concat(_viewModel.InProgressTasks)
                    .Concat(_viewModel.TestingTasks).Concat(_viewModel.DoneTasks)
                    .Select(t => new TaskWithSortDate(t))
                    .ToList();

                // Collection pour les tâches en cours ou en test (avec date)
                var activeTasks = new ObservableCollection<TaskWithSortDate>(
                    allTasks.Where(t => t.IsInProgressOrTesting));

                _activeTasksView = CollectionViewSource.GetDefaultView(activeTasks);
                if (ActiveTasksGrid != null)
                    ActiveTasksGrid.ItemsSource = _activeTasksView;

                // Collection pour les tâches à faire (sans date)
                var todoTasks = new ObservableCollection<TaskWithSortDate>(
                    allTasks.Where(t => t.IsTodoOnly));

                _todoTasksView = CollectionViewSource.GetDefaultView(todoTasks);
                if (TodoTasksGrid != null)
                    TodoTasksGrid.ItemsSource = _todoTasksView;

                // Collection pour les tâches terminées
                var completedTasks = new ObservableCollection<TaskWithSortDate>(
                    allTasks.Where(t => t.CompletionDate.HasValue));

                _completedTasksView = CollectionViewSource.GetDefaultView(completedTasks);
                if (CompletedTasksGrid != null)
                    CompletedTasksGrid.ItemsSource = _completedTasksView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la préparation des collections : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyDefaultSorting()
        {
            // Tri par défaut pour les tâches actives (priorité à la date de début si elle existe, sinon date prévue)
            _activeTasksView.SortDescriptions.Clear();
            _activeTasksView.SortDescriptions.Add(new SortDescription("SortDate", ListSortDirection.Ascending));

            // Tri par défaut pour les tâches à faire (d'abord celles avec date demandée, puis par date au plus tôt)
            _todoTasksView.SortDescriptions.Clear();
            _todoTasksView.SortDescriptions.Add(new SortDescription("HasRequestedDate", ListSortDirection.Descending));
            _todoTasksView.SortDescriptions.Add(new SortDescription("RequestedDate", ListSortDirection.Ascending));
            _todoTasksView.SortDescriptions.Add(new SortDescription("Client", ListSortDirection.Ascending));

            // Tri par défaut pour les tâches terminées (par date de finalisation)
            _completedTasksView.SortDescriptions.Clear();
            _completedTasksView.SortDescriptions.Add(new SortDescription("CompletionDate", ListSortDirection.Ascending));
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            string columnName = button.CommandParameter.ToString();

            ICollectionView view;
            string propertyName;

            if (columnName.EndsWith("-Completed"))
            {
                view = _completedTasksView;
                propertyName = columnName.Substring(0, columnName.Length - 10);
                ResetSortDirections(true);
            }
            else if (columnName.EndsWith("-Todo"))
            {
                view = _todoTasksView;
                propertyName = columnName.Substring(0, columnName.Length - 5);
                ResetSortDirections(false, true);
            }
            else
            {
                view = _activeTasksView;
                propertyName = columnName;
                ResetSortDirections(false, false);
            }

            // Obtenir la direction de tri actuelle et la basculer
            string currentSortDirection = GetSortDirection(columnName);
            string newSortDirection = (currentSortDirection == "Ascending") ? "Descending" : "Ascending";
            SetSortDirection(columnName, newSortDirection);

            // Appliquer le tri
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(
                propertyName,
                newSortDirection == "Ascending" ? ListSortDirection.Ascending : ListSortDirection.Descending));
        }

        private string GetSortDirection(string columnName)
        {
            if (columnName.EndsWith("-Completed"))
            {
                string property = columnName.Substring(0, columnName.Length - 10);
                switch (property)
                {
                    case "Client": return CompletedClientSortDirection;
                    case "Title": return CompletedTitleSortDirection;
                    case "Description": return CompletedDescriptionSortDirection;
                    case "RequestedDate": return CompletedRequestedDateSortDirection;
                    case "DueDate": return CompletedDueDateSortDirection;
                    case "StartDate": return CompletedStartDateSortDirection;
                    case "SortDate": return CompletedSortDateSortDirection;
                    case "PlannedTimeDays": return CompletedPlannedTimeDaysSortDirection;
                    case "ActualTimeDays": return CompletedActualTimeDaysSortDirection;
                    case "CompletionDate": return CompletedCompletionDateSortDirection;
                    default: return "";
                }
            }
            else if (columnName.EndsWith("-Todo"))
            {
                string property = columnName.Substring(0, columnName.Length - 5);
                switch (property)
                {
                    case "Client": return TodoClientSortDirection;
                    case "Title": return TodoTitleSortDirection;
                    case "Description": return TodoDescriptionSortDirection;
                    case "RequestedDate": return TodoRequestedDateSortDirection;
                    case "DueDate": return TodoDueDateSortDirection;
                    default: return "";
                }
            }
            else
            {
                switch (columnName)
                {
                    case "Client": return ActiveClientSortDirection;
                    case "Title": return ActiveTitleSortDirection;
                    case "Description": return ActiveDescriptionSortDirection;
                    case "RequestedDate": return ActiveRequestedDateSortDirection;
                    case "DueDate": return ActiveDueDateSortDirection;
                    case "StartDate": return ActiveStartDateSortDirection;
                    case "SortDate": return ActiveSortDateSortDirection;
                    case "PlannedTimeDays": return ActivePlannedTimeDaysSortDirection;
                    default: return "";
                }
            }
        }

        private void SetSortDirection(string columnName, string direction)
        {
            if (columnName.EndsWith("-Completed"))
            {
                string property = columnName.Substring(0, columnName.Length - 10);
                switch (property)
                {
                    case "Client": CompletedClientSortDirection = direction; break;
                    case "Title": CompletedTitleSortDirection = direction; break;
                    case "Description": CompletedDescriptionSortDirection = direction; break;
                    case "RequestedDate": CompletedRequestedDateSortDirection = direction; break;
                    case "DueDate": CompletedDueDateSortDirection = direction; break;
                    case "StartDate": CompletedStartDateSortDirection = direction; break;
                    case "SortDate": CompletedSortDateSortDirection = direction; break;
                    case "PlannedTimeDays": CompletedPlannedTimeDaysSortDirection = direction; break;
                    case "ActualTimeDays": CompletedActualTimeDaysSortDirection = direction; break;
                    case "CompletionDate": CompletedCompletionDateSortDirection = direction; break;
                }
            }
            else if (columnName.EndsWith("-Todo"))
            {
                string property = columnName.Substring(0, columnName.Length - 5);
                switch (property)
                {
                    case "Client": TodoClientSortDirection = direction; break;
                    case "Title": TodoTitleSortDirection = direction; break;
                    case "Description": TodoDescriptionSortDirection = direction; break;
                    case "RequestedDate": TodoRequestedDateSortDirection = direction; break;
                    case "DueDate": TodoDueDateSortDirection = direction; break;
                }
            }
            else
            {
                switch (columnName)
                {
                    case "Client": ActiveClientSortDirection = direction; break;
                    case "Title": ActiveTitleSortDirection = direction; break;
                    case "Description": ActiveDescriptionSortDirection = direction; break;
                    case "RequestedDate": ActiveRequestedDateSortDirection = direction; break;
                    case "DueDate": ActiveDueDateSortDirection = direction; break;
                    case "StartDate": ActiveStartDateSortDirection = direction; break;
                    case "SortDate": ActiveSortDateSortDirection = direction; break;
                    case "PlannedTimeDays": ActivePlannedTimeDaysSortDirection = direction; break;
                }
            }
        }

        private void ResetSortDirections(bool isCompletedList, bool isTodoList = false)
        {
            if (isCompletedList)
            {
                CompletedClientSortDirection = "";
                CompletedTitleSortDirection = "";
                CompletedDescriptionSortDirection = "";
                CompletedRequestedDateSortDirection = "";
                CompletedDueDateSortDirection = "";
                CompletedStartDateSortDirection = "";
                CompletedSortDateSortDirection = "";
                CompletedPlannedTimeDaysSortDirection = "";
                CompletedActualTimeDaysSortDirection = "";
                CompletedCompletionDateSortDirection = "";
            }
            else if (isTodoList)
            {
                TodoClientSortDirection = "";
                TodoTitleSortDirection = "";
                TodoDescriptionSortDirection = "";
                TodoRequestedDateSortDirection = "";
                TodoDueDateSortDirection = "";
            }
            else
            {
                ActiveClientSortDirection = "";
                ActiveTitleSortDirection = "";
                ActiveDescriptionSortDirection = "";
                ActiveRequestedDateSortDirection = "";
                ActiveDueDateSortDirection = "";
                ActiveStartDateSortDirection = "";
                ActiveSortDateSortDirection = "";
                ActivePlannedTimeDaysSortDirection = "";
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ShowCompletedTasksCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // L'affichage/masquage est géré directement par le binding dans le XAML
        }

        private void ShowTodoTasksCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // L'affichage/masquage est géré directement par le binding dans le XAML
        }

        private void ActiveTasksGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ActiveTasksGrid.SelectedItem is TaskWithSortDate task)
            {
                OpenEditWindow(task);
            }
        }

        private void TodoTasksGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TodoTasksGrid.SelectedItem is TaskWithSortDate task)
            {
                OpenEditWindow(task);
            }
        }

        private void CompletedTasksGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CompletedTasksGrid.SelectedItem is TaskWithSortDate task)
            {
                OpenEditWindow(task);
            }
        }

        private void OpenEditWindow(TaskWithSortDate task)
        {
            var editWindow = new KanbanView(task);
            if (editWindow.ShowDialog() == true && editWindow.CreatedTask != null)
            {
                // Mettre à jour la tâche originale
                UpdateOriginalTask(task, editWindow.CreatedTask);

                // Plutôt que de rafraîchir les vues, recréer complètement les collections
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Recréer les collections pour éviter les problèmes de transaction
                    PrepareTasksCollections();

                    // Réappliquer les tris
                    ApplyDefaultSorting();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void RearrangeTasks(TaskWithSortDate task)
        {
            // Vérifier si la tâche doit être déplacée d'une liste à une autre
            var allTasks = new ObservableCollection<TaskWithSortDate>(
                _activeTasksView.Cast<TaskWithSortDate>().Concat(
                _todoTasksView.Cast<TaskWithSortDate>()).Concat(
                _completedTasksView.Cast<TaskWithSortDate>()));

            // Recréer les collections
            PrepareTasksCollections();

            // Appliquer à nouveau le tri
            ApplyDefaultSorting();
        }

        private void UpdateOriginalTask(TaskWithSortDate taskWrapper, TaskModel updatedTask)
        {
            // Utiliser le comparateur pour mettre à jour la tâche
            var comparer = new TodoTaskComparer();
            comparer.UpdateOriginalTask(_viewModel, taskWrapper, updatedTask);

            // Mettre à jour les propriétés du wrapper
            taskWrapper.Title = updatedTask.Title;
            taskWrapper.Client = updatedTask.Client;
            taskWrapper.Description = updatedTask.Description;
            taskWrapper.StartDate = updatedTask.StartDate;
            taskWrapper.DueDate = updatedTask.DueDate;
            taskWrapper.RequestedDate = updatedTask.RequestedDate;
            taskWrapper.CompletionDate = updatedTask.CompletionDate;
            taskWrapper.PlannedTimeDays = updatedTask.PlannedTimeDays;
            taskWrapper.PlannedTimeHours = updatedTask.PlannedTimeHours;
            taskWrapper.ActualTimeDays = updatedTask.ActualTimeDays;
            taskWrapper.ActualTimeHours = updatedTask.ActualTimeHours;
        }

        // Convertisseur pour afficher la description seulement si la case à cocher est cochée
        public class NullToEmptyStringConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (values.Length < 2 || values[0] == null || values[1] == null)
                    return string.Empty;

                string description = values[0].ToString();
                bool showDescription = (bool)values[1];

                if (!showDescription)
                    return string.Empty;

                return description ?? string.Empty;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }


        }
        private void OnShowGanttClick(object sender, RoutedEventArgs e)
        {
            var ganttWindow = new GanttWindow(_viewModel);
            ganttWindow.Owner = this;
            ganttWindow.ShowDialog();
        }

        private void OnExportExcelClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Créer une boîte de dialogue pour choisir l'emplacement du fichier
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "Enregistrer le fichier Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        // Créer les trois feuilles de calcul
                        var worksheetTodo = workbook.Worksheets.Add("Tâches à faire");
                        var worksheetActive = workbook.Worksheets.Add("Tâches prévu-encours-test");
                        var worksheetCompleted = workbook.Worksheets.Add("Tâches terminées");

                        // Couleurs correspondant aux styles de l'interface
                        var primaryColor = "#3B7CD4";
                        var secondaryColor = "#2C3E50";
                        var warningColor = "#F39C12";
                        var accentColor = "#2ECC71";

                        // Exporter les tâches à faire
                        ExportDataGridToWorksheet(TodoTasksGrid, worksheetTodo, primaryColor);

                        // Exporter les tâches actives (prévu/encours ou en test)
                        ExportDataGridToWorksheet(ActiveTasksGrid, worksheetActive, warningColor);

                        // Exporter les tâches terminées
                        ExportDataGridToWorksheet(CompletedTasksGrid, worksheetCompleted, accentColor);

                        // Sauvegarder le fichier Excel
                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    MessageBox.Show("Exportation réussie !", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'exportation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportDataGridToWorksheet(DataGrid dataGrid, IXLWorksheet worksheet, string headerColor)
        {
            // Si le DataGrid n'est pas visible ou ne contient pas de données, ne rien faire
            if (dataGrid.Visibility != Visibility.Visible || dataGrid.Items.Count == 0)
                return;

            int rowIndex = 1;
            int colIndex = 1;

            // Ajouter les en-têtes des colonnes
            foreach (var column in dataGrid.Columns)
            {
                if (column is DataGridTextColumn textColumn)
                {
                    var header = "";
                    if (textColumn.Header is string headerString)
                    {
                        header = headerString;
                    }
                    else if (textColumn.HeaderTemplate != null)
                    {
                        // Extraire le contenu du bouton dans le HeaderTemplate
                        var headerContent = textColumn.HeaderTemplate.LoadContent();
                        if (headerContent is Button button)
                        {
                            header = button.Content.ToString();
                        }
                    }

                    worksheet.Cell(rowIndex, colIndex).Value = header;

                    // Formater l'en-tête
                    var headerCell = worksheet.Cell(rowIndex, colIndex);
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Font.FontColor = XLColor.White;
                    headerCell.Style.Fill.BackgroundColor = XLColor.FromHtml(headerColor);
                    headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    colIndex++;
                }
            }

            // Ajouter les données des lignes
            foreach (var item in dataGrid.Items)
            {
                if (item is TaskWithSortDate task)
                {
                    colIndex = 1;
                    rowIndex++;

                    // Ajouter les valeurs des colonnes pour chaque ligne
                    foreach (var column in dataGrid.Columns)
                    {
                        if (column is DataGridTextColumn textColumn)
                        {
                            if (textColumn.Binding is Binding binding)
                            {
                                var propertyPath = binding.Path.Path;
                                var propertyInfo = task.GetType().GetProperty(propertyPath);

                                if (propertyInfo != null)
                                {
                                    var value = propertyInfo.GetValue(task);

                                    // Formater les dates
                                    if (value is DateTime dateValue)
                                    {
                                        worksheet.Cell(rowIndex, colIndex).Value = dateValue;
                                        worksheet.Cell(rowIndex, colIndex).Style.DateFormat.Format = "dd/MM/yyyy";
                                    }
                                    else if (value is DateTime?)
                                    {
                                        DateTime? nullableDateValue = value as DateTime?;
                                        if (nullableDateValue.HasValue)
                                        {
                                            worksheet.Cell(rowIndex, colIndex).Value = nullableDateValue.Value;
                                            worksheet.Cell(rowIndex, colIndex).Style.DateFormat.Format = "dd/MM/yyyy";
                                        }
                                        else
                                        {
                                            worksheet.Cell(rowIndex, colIndex).Value = "";
                                        }
                                    }
                                    else
                                    {
                                        worksheet.Cell(rowIndex, colIndex).Value = value?.ToString() ?? "";
                                    }
                                }
                            }

                            colIndex++;
                        }
                    }
                }
            }

            // Ajuster la largeur des colonnes
            worksheet.Columns().AdjustToContents();

            // Appliquer des bordures aux cellules
            var range = worksheet.Range(1, 1, rowIndex, colIndex - 1);
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }
    }
}