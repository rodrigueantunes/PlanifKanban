using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Xml.Serialization;
using PlanifKanban.Models;
using PlanifKanban.Views;

namespace PlanifKanban.ViewModels
{
    [Serializable]
    public class KanbanViewModel
    {
        private ObservableCollection<TaskModel> _todoTasks;
        private ObservableCollection<TaskModel> _inProgressTasks;
        private ObservableCollection<TaskModel> _testingTasks;
        private ObservableCollection<TaskModel> _doneTasks;

        [XmlIgnore]
        public ListCollectionView TodoTasksView { get; private set; }
        [XmlIgnore]
        public ListCollectionView InProgressTasksView { get; private set; }
        [XmlIgnore]
        public ListCollectionView TestingTasksView { get; private set; }
        [XmlIgnore]
        public ListCollectionView DoneTasksView { get; private set; }

        public ObservableCollection<TaskModel> TodoTasks
        {
            get { return _todoTasks; }
            set
            {
                _todoTasks = value;
                if (_todoTasks != null)
                {
                    TodoTasksView = new ListCollectionView(_todoTasks);
                    ConfigureTodoSorting();
                }
            }
        }

        public ObservableCollection<TaskModel> InProgressTasks
        {
            get { return _inProgressTasks; }
            set
            {
                _inProgressTasks = value;
                if (_inProgressTasks != null)
                {
                    InProgressTasksView = new ListCollectionView(_inProgressTasks);
                    ConfigureInProgressSorting();
                }
            }
        }

        public ObservableCollection<TaskModel> TestingTasks
        {
            get { return _testingTasks; }
            set
            {
                _testingTasks = value;
                if (_testingTasks != null)
                {
                    TestingTasksView = new ListCollectionView(_testingTasks);
                    ConfigureTestingSorting();
                }
            }
        }

        public ObservableCollection<TaskModel> DoneTasks
        {
            get { return _doneTasks; }
            set
            {
                _doneTasks = value;
                if (_doneTasks != null)
                {
                    DoneTasksView = new ListCollectionView(_doneTasks);
                    ConfigureDoneSorting();
                }
            }
        }

        public KanbanViewModel()
        {
            TodoTasks = new ObservableCollection<TaskModel>();
            InProgressTasks = new ObservableCollection<TaskModel>();
            TestingTasks = new ObservableCollection<TaskModel>();
            DoneTasks = new ObservableCollection<TaskModel>();
        }

        public void AddTask(TaskModel task)
        {
            TodoTasks.Add(task);
        }

        private void ConfigureTodoSorting()
        {
            if (TodoTasksView != null)
            {
                TodoTasksView.SortDescriptions.Clear();
                TodoTasksView.CustomSort = new TodoTaskComparer();
            }
        }

        private void ConfigureInProgressSorting()
        {
            if (InProgressTasksView != null)
            {
                InProgressTasksView.SortDescriptions.Clear();
                InProgressTasksView.SortDescriptions.Add(new SortDescription("StartDate", ListSortDirection.Ascending));
            }
        }

        private void ConfigureTestingSorting()
        {
            if (TestingTasksView != null)
            {
                TestingTasksView.SortDescriptions.Clear();
                TestingTasksView.SortDescriptions.Add(new SortDescription("StartDate", ListSortDirection.Ascending));
            }
        }

        private void ConfigureDoneSorting()
        {
            if (DoneTasksView != null)
            {
                DoneTasksView.SortDescriptions.Clear();
                DoneTasksView.SortDescriptions.Add(new SortDescription("CompletionDate", ListSortDirection.Ascending));
            }
        }

        public void RefreshAllSorting()
        {
            ConfigureTodoSorting();
            ConfigureInProgressSorting();
            ConfigureTestingSorting();
            ConfigureDoneSorting();
        }

        public void SaveToFile(string filePath)
        {
            try
            {
                KanbanSaveModel saveModel = new KanbanSaveModel
                {
                    TodoTasks = new List<TaskModel>(TodoTasks),
                    InProgressTasks = new List<TaskModel>(InProgressTasks),
                    TestingTasks = new List<TaskModel>(TestingTasks),
                    DoneTasks = new List<TaskModel>(DoneTasks)
                };

                XmlSerializer serializer = new XmlSerializer(typeof(KanbanSaveModel));
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    serializer.Serialize(stream, saveModel);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de la sauvegarde du Kanban", ex);
            }
        }

        public static KanbanViewModel LoadFromFile(string filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(KanbanSaveModel));
                KanbanSaveModel saveModel;

                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    saveModel = (KanbanSaveModel)serializer.Deserialize(stream);
                }

                KanbanViewModel viewModel = new KanbanViewModel();

                if (saveModel.TodoTasks != null)
                    foreach (var task in saveModel.TodoTasks)
                        viewModel.TodoTasks.Add(task);

                if (saveModel.InProgressTasks != null)
                    foreach (var task in saveModel.InProgressTasks)
                        viewModel.InProgressTasks.Add(task);

                if (saveModel.TestingTasks != null)
                    foreach (var task in saveModel.TestingTasks)
                        viewModel.TestingTasks.Add(task);

                if (saveModel.DoneTasks != null)
                    foreach (var task in saveModel.DoneTasks)
                        viewModel.DoneTasks.Add(task);

                return viewModel;
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors du chargement du Kanban", ex);
            }
        }
    }

    [Serializable]
    public class KanbanSaveModel
    {
        public List<TaskModel> TodoTasks { get; set; }
        public List<TaskModel> InProgressTasks { get; set; }
        public List<TaskModel> TestingTasks { get; set; }
        public List<TaskModel> DoneTasks { get; set; }
    }
}