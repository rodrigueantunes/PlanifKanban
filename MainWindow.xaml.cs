using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PlanifKanban.Models;
using PlanifKanban.ViewModels;
using PlanifKanban.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Text;
using ExcelDataReader;
using System.ComponentModel;
using System.Windows.Data;


namespace PlanifKanban
{
    public partial class MainWindow : Window
    {
        public KanbanViewModel ViewModel { get; set; }
        private Point _startPoint;
        private ListViewItem _draggedItem;
        private TaskModel _draggedTask;



        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new KanbanViewModel();
            DataContext = ViewModel;

            TodoList.Tag = TodoList.Background?.ToString() ?? "null";
            InProgressList.Tag = InProgressList.Background?.ToString() ?? "null";
            TestingList.Tag = TestingList.Background?.ToString() ?? "null";
            DoneList.Tag = DoneList.Background?.ToString() ?? "null";
        }

        private void OnAddTaskClick(object sender, RoutedEventArgs e)
        {
            var kanbanView = new KanbanView();
            if (kanbanView.ShowDialog() == true && kanbanView.CreatedTask != null)
            {
                ViewModel.AddTask(kanbanView.CreatedTask);
            }
        }

        private void OnTaskDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is TaskModel task)
            {
                var editWindow = new KanbanView(task);
                if (editWindow.ShowDialog() == true && editWindow.CreatedTask != null)
                {
                    UpdateTask(task, editWindow.CreatedTask);
                }
            }
        }

        private void ModifyTaskClick(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuItem)?.DataContext is TaskModel task)
            {
                var editWindow = new KanbanView(task);
                if (editWindow.ShowDialog() == true && editWindow.CreatedTask != null)
                {
                    UpdateTask(task, editWindow.CreatedTask);
                }
            }
        }

        private void DeleteTaskClick(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuItem)?.DataContext is TaskModel task)
            {
                ViewModel.TodoTasks.Remove(task);
                ViewModel.InProgressTasks.Remove(task);
                ViewModel.TestingTasks.Remove(task);
                ViewModel.DoneTasks.Remove(task);
            }
        }

        private void SendToTodoClick(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuItem)?.DataContext is TaskModel task)
            {
                MoveTask(task, ViewModel.TodoTasks, "À faire");
            }
        }

        private void SendToInProgressClick(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuItem)?.DataContext is TaskModel task)
            {
                MoveTask(task, ViewModel.InProgressTasks, "En cours");
            }
        }

        private void SendToTestingClick(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuItem)?.DataContext is TaskModel task)
            {
                MoveTask(task, ViewModel.TestingTasks, "En test");
            }
        }

        private void SendToDoneClick(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuItem)?.DataContext is TaskModel task)
            {
                MoveTask(task, ViewModel.DoneTasks, "Terminée");
            }
        }

        private void MoveTask(TaskModel task, System.Collections.ObjectModel.ObservableCollection<TaskModel> targetList, string column)
        {
            if (IsDateMissing(task, column))
            {
                var dialog = new DateMissingPopup(column, task);
                if (dialog.ShowDialog() == true)
                {
                    task.StartDate = dialog.StartDate;
                    task.DueDate = dialog.DueDate;
                    task.RequestedDate = dialog.RequestedDate;
                    task.CompletionDate = dialog.CompletionDate;
                }
                else
                {
                    return; // L'utilisateur a annulé, ne pas déplacer la tâche
                }
            }

            // Déterminer les colonnes qui seront modifiées
            string sourceColumn = null;
            if (ViewModel.TodoTasks.Contains(task)) sourceColumn = "À faire";
            else if (ViewModel.InProgressTasks.Contains(task)) sourceColumn = "En cours";
            else if (ViewModel.TestingTasks.Contains(task)) sourceColumn = "En test";
            else if (ViewModel.DoneTasks.Contains(task)) sourceColumn = "Terminée";

            // Retirer la tâche de toutes les collections
            ViewModel.TodoTasks.Remove(task);
            ViewModel.InProgressTasks.Remove(task);
            ViewModel.TestingTasks.Remove(task);
            ViewModel.DoneTasks.Remove(task);

            // Ajouter la tâche à la collection cible
            targetList.Add(task);

            // Rafraîchir uniquement les colonnes concernées
            if (sourceColumn != null && sourceColumn != column)
                RefreshView(sourceColumn);  // Rafraîchir la colonne source si elle est différente de la cible

            RefreshView(column);  // Rafraîchir la colonne cible
        }

        private bool IsDateMissing(TaskModel task, string column)
        {
            switch (column)
            {
                case "En cours":
                case "En test":
                    return !task.StartDate.HasValue || !task.DueDate.HasValue;
                case "Terminée":
                    return !task.StartDate.HasValue || !task.DueDate.HasValue || !task.CompletionDate.HasValue;
                default:
                    return false;
            }
        }

        private void UpdateTask(TaskModel original, TaskModel updated)
        {
            original.Title = updated.Title;
            original.Client = updated.Client;
            original.Description = updated.Description;
            original.StartDate = updated.StartDate;
            original.DueDate = updated.DueDate;
            original.RequestedDate = updated.RequestedDate;
            original.CompletionDate = updated.CompletionDate;
            original.PlannedTimeDays = updated.PlannedTimeDays;
            original.PlannedTimeHours = updated.PlannedTimeHours;
            original.ActualTimeDays = updated.ActualTimeDays;
            original.ActualTimeHours = updated.ActualTimeHours;

            // Rafraîchir les listes pour appliquer le tri
            RefreshAllListViews();
        }

        // Méthode pour rafraîchir une vue spécifique
        private void RefreshView(string columnName)
        {
            switch (columnName)
            {
                case "À faire":
                    ICollectionView todoView = CollectionViewSource.GetDefaultView(
                        ((CollectionViewSource)Resources["TodoTasksViewSource"]).Source);
                    todoView.Refresh();
                    break;
                case "En cours":
                    ICollectionView inProgressView = CollectionViewSource.GetDefaultView(
                        ((CollectionViewSource)Resources["InProgressTasksViewSource"]).Source);
                    inProgressView.Refresh();
                    break;
                case "En test":
                    ICollectionView testingView = CollectionViewSource.GetDefaultView(
                        ((CollectionViewSource)Resources["TestingTasksViewSource"]).Source);
                    testingView.Refresh();
                    break;
                case "Terminée":
                    ICollectionView doneView = CollectionViewSource.GetDefaultView(
                        ((CollectionViewSource)Resources["DoneTasksViewSource"]).Source);
                    doneView.Refresh();
                    break;
            }
        }

        // Méthode pour rafraîchir toutes les vues si nécessaire
        private void RefreshAllViews()
        {
            RefreshView("À faire");
            RefreshView("En cours");
            RefreshView("En test");
            RefreshView("Terminée");
        }

        private void RefreshAllListViews()
        {
            // Reconfigurer le tri
            ConfigureSorting();

            // Rafraîchir les listes
            CollectionViewSource.GetDefaultView(TodoList.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(InProgressList.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(TestingList.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(DoneList.ItemsSource).Refresh();
        }

        private void ListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            var listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            if (listViewItem != null)
            {
                _draggedItem = listViewItem;
                _draggedTask = listViewItem.Content as TaskModel;
            }
        }

        private void ListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedItem == null || _draggedTask == null)
                return;

            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;

            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            // Stocker la colonne source avant le déplacement
            string sourceColumn = null;
            if (sender == TodoList) sourceColumn = "À faire";
            else if (sender == InProgressList) sourceColumn = "En cours";
            else if (sender == TestingList) sourceColumn = "En test";
            else if (sender == DoneList) sourceColumn = "Terminée";

            // Créer une représentation visuelle de l'élément glissé
            DataObject dragData = new DataObject(typeof(TaskModel), _draggedTask);

            // Définir un effet visuel sur l'élément original pendant le glissement
            _draggedItem.Opacity = 0.5;

            // Démarrer l'opération de glisser-déposer
            DragDrop.DoDragDrop(_draggedItem, dragData, DragDropEffects.Move);

            // Réinitialiser l'opacité après le glisser-déposer
            _draggedItem.Opacity = 1.0;
            _draggedItem = null;
            _draggedTask = null;

            // La colonne cible sera rafraîchie dans la méthode de réception du drop
        }

        private void ListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TaskModel)))
            {
                e.Effects = DragDropEffects.Move;

                // Au lieu de modifier la bordure, on peut changer la couleur de fond
                if (sender is ListView listView)
                {
                    // Sauvegarde la couleur de fond originale
                    if (listView.Tag == null || !listView.Tag.ToString().Contains("Highlighted"))
                    {
                        string background = listView.Background != null ? listView.Background.ToString() : "null";
                        listView.Tag = background + "|Highlighted";

                        // Utiliser une couleur légèrement différente pour indiquer la zone de dépôt
                        listView.Background = new SolidColorBrush(Color.FromArgb(50, 59, 124, 212)); // Bleu clair semi-transparent
                    }
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ListView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TaskModel)))
            {
                e.Effects = DragDropEffects.Move;

                // Modifier le curseur pour indiquer une opération de déplacement
                Mouse.OverrideCursor = Cursors.Hand;
            }
            else
            {
                e.Effects = DragDropEffects.None;
                Mouse.OverrideCursor = Cursors.No;
            }
            e.Handled = true;
        }

        private void ListView_DragLeave(object sender, DragEventArgs e)
        {
            // Réinitialiser l'apparence de la liste quand le curseur la quitte
            if (sender is ListView listView && listView.Tag != null)
            {
                string[] tagParts = listView.Tag.ToString().Split('|');
                if (tagParts.Length > 1 && tagParts[1] == "Highlighted")
                {
                    // Restaurer la couleur de fond originale
                    if (tagParts[0] == "null")
                    {
                        listView.Background = null;
                    }
                    else
                    {
                        try
                        {
                            listView.Background = (Brush)new BrushConverter().ConvertFromString(tagParts[0]);
                        }
                        catch
                        {
                            // En cas d'erreur, simplement effacer le fond
                            listView.Background = null;
                        }
                    }

                    listView.Tag = tagParts[0];
                }
            }
        }

        private void OnDropTodo(object sender, DragEventArgs e)
        {
            // Réinitialiser le curseur
            Mouse.OverrideCursor = null;

            // Réinitialiser l'apparence de TOUTES les ListView
            ResetAllListViewStyles();

            if (e.Data.GetDataPresent(typeof(TaskModel)))
            {
                TaskModel task = e.Data.GetData(typeof(TaskModel)) as TaskModel;
                if (task != null)
                {
                    MoveTask(task, ViewModel.TodoTasks, "À faire");
                }
            }
        }

        private void OnDropInProgress(object sender, DragEventArgs e)
        {
            Mouse.OverrideCursor = null;
            ResetAllListViewStyles();

            if (e.Data.GetDataPresent(typeof(TaskModel)))
            {
                TaskModel task = e.Data.GetData(typeof(TaskModel)) as TaskModel;
                if (task != null)
                {
                    MoveTask(task, ViewModel.InProgressTasks, "En cours");
                }
            }
        }

        private void OnDropTesting(object sender, DragEventArgs e)
        {
            Mouse.OverrideCursor = null;
            ResetAllListViewStyles();

            if (e.Data.GetDataPresent(typeof(TaskModel)))
            {
                TaskModel task = e.Data.GetData(typeof(TaskModel)) as TaskModel;
                if (task != null)
                {
                    MoveTask(task, ViewModel.TestingTasks, "En test");
                }
            }
        }

        private void OnDropDone(object sender, DragEventArgs e)
        {
            Mouse.OverrideCursor = null;
            ResetAllListViewStyles();

            if (e.Data.GetDataPresent(typeof(TaskModel)))
            {
                TaskModel task = e.Data.GetData(typeof(TaskModel)) as TaskModel;
                if (task != null)
                {
                    MoveTask(task, ViewModel.DoneTasks, "Terminée");
                }
            }
        }

        // Nouvelle méthode pour réinitialiser toutes les ListView
        private void ResetAllListViewStyles()
        {
            ResetListViewStyle(TodoList);
            ResetListViewStyle(InProgressList);
            ResetListViewStyle(TestingList);
            ResetListViewStyle(DoneList);
        }

        // Méthode pour réinitialiser une ListView spécifique
        private void ResetListViewStyle(ListView listView)
        {
            if (listView != null && listView.Tag != null)
            {
                string[] tagParts = listView.Tag.ToString().Split('|');
                string originalBackground = tagParts[0];

                if (originalBackground == "null")
                {
                    listView.Background = null;
                }
                else
                {
                    try
                    {
                        listView.Background = (Brush)new BrushConverter().ConvertFromString(originalBackground);
                    }
                    catch
                    {
                        listView.Background = null;
                    }
                }

                listView.Tag = originalBackground;
            }
        }


        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T)
                    return (T)current;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }


        // Ajouter ces méthodes à votre classe MainWindow
        private void OnSaveKanbanClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Configurer la boîte de dialogue de sauvegarde
                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    DefaultExt = ".ant",
                    Filter = "Fichiers Kanban (.ant)|*.ant",
                    Title = "Sauvegarder le Kanban"
                };

                // Afficher la boîte de dialogue
                if (saveDialog.ShowDialog() == true)
                {
                    // Sauvegarder dans le fichier choisi
                    ViewModel.SaveToFile(saveDialog.FileName);
                    MessageBox.Show("Kanban sauvegardé avec succès.", "Sauvegarde réussie", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenKanbanClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Configurer la boîte de dialogue d'ouverture
                Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog
                {
                    DefaultExt = ".ant",
                    Filter = "Fichiers Kanban (.ant)|*.ant",
                    Title = "Ouvrir un Kanban"
                };

                // Afficher la boîte de dialogue
                if (openDialog.ShowDialog() == true)
                {
                    // Charger depuis le fichier choisi
                    ViewModel = KanbanViewModel.LoadFromFile(openDialog.FileName);
                    DataContext = ViewModel;
                    MessageBox.Show("Kanban chargé avec succès.", "Chargement réussi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowDescriptionCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Force le rafraîchissement des ListView pour mettre à jour l'affichage
            TodoList.Items.Refresh();
            InProgressList.Items.Refresh();
            TestingList.Items.Refresh();
            DoneList.Items.Refresh();
        }

        private void OnImportExcelClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Nécessaire pour le support des encodages étendus utilisés dans les fichiers Excel
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                // Configurer la boîte de dialogue d'ouverture
                Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog
                {
                    DefaultExt = ".xlsx",
                    Filter = "Fichiers Excel (.xlsx, .xls, .xlsm, .xltx, .xltm)|*.xlsx;*.xls;*.xlsm;*.xltx;*.xltm",
                    Title = "Importer des tâches depuis Excel"
                };

                // Afficher la boîte de dialogue
                if (openDialog.ShowDialog() == true)
                {
                    List<TaskModel> importedTasks = ImportTasksFromExcel(openDialog.FileName);

                    if (importedTasks.Count > 0)
                    {
                        // Ajouter les tâches importées à la liste "À faire"
                        foreach (var task in importedTasks)
                        {
                            ViewModel.TodoTasks.Add(task);
                        }

                        MessageBox.Show($"{importedTasks.Count} tâches ont été importées avec succès.",
                            "Importation réussie", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Rafraîchir l'affichage
                        RefreshAllListViews();
                    }
                    else
                    {
                        MessageBox.Show("Aucune tâche valide n'a été trouvée dans le fichier Excel.",
                            "Importation terminée", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Une erreur est survenue lors de l'importation: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<TaskModel> ImportTasksFromExcel(string filePath)
        {
            List<TaskModel> importedTasks = new List<TaskModel>();

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                // Détection automatique du format (XLS ou XLSX)
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // Convertir en DataSet pour un accès plus facile
                    var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true // Utiliser la première ligne comme en-têtes
                        }
                    });

                    // Prendre la première feuille de calcul
                    DataTable dataTable = dataSet.Tables[0];

                    // Rechercher les index des colonnes
                    int titleIndex = -1;
                    int clientIndex = -1;
                    int descriptionIndex = -1;
                    int requestDateIndex = -1;
                    int completionDateIndex = -1;

                    // Chercher par nom de colonne ou par index explicite (C=2, B=1, D=3, AS=44, AT=45 en 0-based)
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        string columnName = dataTable.Columns[i].ColumnName.Trim();

                        if (columnName == "Volufiche")
                            titleIndex = i;
                        else if (columnName == "Pour le compte de")
                            clientIndex = i;
                        else if (columnName == "Objet")
                            descriptionIndex = i;
                        else if (columnName == "Deadline client")
                            requestDateIndex = i;
                        else if (columnName == "Date de livraison prévue")
                            completionDateIndex = i;
                    }

                    // Si nous n'avons pas trouvé par nom, essayons par index connu
                    if (titleIndex == -1) titleIndex = 2; // C
                    if (clientIndex == -1) clientIndex = 1; // B
                    if (descriptionIndex == -1) descriptionIndex = 3; // D

                    // Pour les colonnes avancées, vérifions qu'elles existent
                    if (requestDateIndex == -1 && dataTable.Columns.Count > 44)
                        requestDateIndex = 44; // AS
                    if (completionDateIndex == -1 && dataTable.Columns.Count > 45)
                        completionDateIndex = 45; // AT

                    // Vérifier que les colonnes obligatoires sont accessibles
                    if (titleIndex >= dataTable.Columns.Count || clientIndex >= dataTable.Columns.Count)
                    {
                        MessageBox.Show("Le format du fichier Excel ne correspond pas à celui attendu. Impossible de trouver les colonnes obligatoires.",
                            "Format incorrect", MessageBoxButton.OK, MessageBoxImage.Error);
                        return importedTasks;
                    }

                    // Parcourir les lignes et créer les tâches
                    foreach (DataRow row in dataTable.Rows)
                    {
                        // Vérifier si les valeurs obligatoires ne sont pas nulles ou vides
                        if (row[titleIndex] == DBNull.Value || row[clientIndex] == DBNull.Value ||
                            string.IsNullOrWhiteSpace(row[titleIndex].ToString()) ||
                            string.IsNullOrWhiteSpace(row[clientIndex].ToString()))
                            continue;

                        TaskModel task = new TaskModel
                        {
                            Title = row[titleIndex].ToString().Trim(),
                            Client = row[clientIndex].ToString().Trim()
                        };

                        // Gérer la description
                        if (descriptionIndex >= 0 && descriptionIndex < dataTable.Columns.Count &&
                            row[descriptionIndex] != DBNull.Value)
                        {
                            task.Description = row[descriptionIndex].ToString();
                        }

                        // Gérer les dates
                        if (requestDateIndex >= 0 && requestDateIndex < dataTable.Columns.Count &&
                            row[requestDateIndex] != DBNull.Value)
                        {
                            if (DateTime.TryParse(row[requestDateIndex].ToString(), out DateTime requestDate))
                            {
                                task.RequestedDate = requestDate;
                            }
                        }

                        if (completionDateIndex >= 0 && completionDateIndex < dataTable.Columns.Count &&
                            row[completionDateIndex] != DBNull.Value)
                        {
                            if (DateTime.TryParse(row[completionDateIndex].ToString(), out DateTime completionDate))
                            {
                                task.CompletionDate = completionDate;
                            }
                        }

                        importedTasks.Add(task);
                    }
                }
            }

            return importedTasks;
        }
        private ICollectionView GetCollectionView(string columnName)
        {
            switch (columnName)
            {
                case "À faire":
                    return CollectionViewSource.GetDefaultView(ViewModel.TodoTasks);
                case "En cours":
                    return CollectionViewSource.GetDefaultView(ViewModel.InProgressTasks);
                case "En test":
                    return CollectionViewSource.GetDefaultView(ViewModel.TestingTasks);
                case "Terminée":
                    return CollectionViewSource.GetDefaultView(ViewModel.DoneTasks);
                default:
                    return null;
            }
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Configurer le tri pour chaque ListView
            ConfigureSorting();
        }

        private void ConfigureSorting()
        {
            // Colonne À Faire : trier par date prévue puis par titre
            ICollectionView todoView = CollectionViewSource.GetDefaultView(TodoList.ItemsSource);
            todoView.SortDescriptions.Clear();
            todoView.SortDescriptions.Add(new SortDescription("HasDueDate", ListSortDirection.Descending)); // Trier d'abord les tâches avec date
            todoView.SortDescriptions.Add(new SortDescription("DueDate", ListSortDirection.Ascending));      // Puis par date au plus tôt
            todoView.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Ascending));        // Puis par titre

            // Colonne En Cours : trier par date de début
            ICollectionView inProgressView = CollectionViewSource.GetDefaultView(InProgressList.ItemsSource);
            inProgressView.SortDescriptions.Clear();
            inProgressView.SortDescriptions.Add(new SortDescription("StartDate", ListSortDirection.Ascending));
            inProgressView.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Ascending));

            // Colonne En Test : trier par date de début
            ICollectionView testingView = CollectionViewSource.GetDefaultView(TestingList.ItemsSource);
            testingView.SortDescriptions.Clear();
            testingView.SortDescriptions.Add(new SortDescription("StartDate", ListSortDirection.Ascending));
            testingView.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Ascending));

            // Colonne Terminée : trier par date de finalisation
            ICollectionView doneView = CollectionViewSource.GetDefaultView(DoneList.ItemsSource);
            doneView.SortDescriptions.Clear();
            doneView.SortDescriptions.Add(new SortDescription("CompletionDate", ListSortDirection.Ascending));
            doneView.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Ascending));
        }
        private void OnShowScheduleClick(object sender, RoutedEventArgs e)
        {
            var scheduleWindow = new ScheduleWindow(ViewModel);
            scheduleWindow.Owner = this;
            scheduleWindow.ShowDialog();
        }

        private void OnShowGanttClick(object sender, RoutedEventArgs e)
        {
            var ganttWindow = new GanttWindow(ViewModel);
            ganttWindow.Owner = this;
            ganttWindow.ShowDialog();
        }
    }
}