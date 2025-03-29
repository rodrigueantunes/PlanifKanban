using System;
using System.Linq;
using System.Collections.ObjectModel;
using PlanifKanban.Models;
using PlanifKanban.Views;

namespace PlanifKanban.ViewModels
{
    public class TodoTaskComparer : System.Collections.IComparer
    {
        public int Compare(object x, object y)
        {
            TaskModel task1 = x as TaskModel;
            TaskModel task2 = y as TaskModel;

            if (task1 == null || task2 == null)
                return 0;

            // Si les deux tâches ont une date prévue, on les compare (au plus tôt d'abord)
            if (task1.DueDate.HasValue && task2.DueDate.HasValue)
                return task1.DueDate.Value.CompareTo(task2.DueDate.Value);

            // Si seule la première tâche a une date prévue, elle vient avant
            if (task1.DueDate.HasValue && !task2.DueDate.HasValue)
                return -1;

            // Si seule la deuxième tâche a une date prévue, elle vient avant
            if (!task1.DueDate.HasValue && task2.DueDate.HasValue)
                return 1;

            // Si aucune tâche n'a de date prévue, on compare par titre
            return string.Compare(task1.Title, task2.Title, StringComparison.Ordinal);
        }

        public void UpdateOriginalTask(KanbanViewModel viewModel, TaskWithSortDate taskWrapper, TaskModel updatedTask)
        {
            // Trouver la tâche originale dans toutes les collections
            TaskModel originalTask = FindOriginalTask(viewModel, taskWrapper);

            if (originalTask == null) return;

            // Mettre à jour toutes les propriétés de la tâche originale
            UpdateTaskProperties(originalTask, updatedTask);

            // Réorganiser les tâches si nécessaire
            ReorganizeTasks(viewModel, originalTask);
        }

        private TaskModel FindOriginalTask(KanbanViewModel viewModel, TaskWithSortDate taskWrapper)
        {
            return viewModel.TodoTasks
                .Concat(viewModel.InProgressTasks)
                .Concat(viewModel.TestingTasks)
                .Concat(viewModel.DoneTasks)
                .FirstOrDefault(t =>
                    t.Title == taskWrapper.Title &&
                    t.Client == taskWrapper.Client);
        }

        private void UpdateTaskProperties(TaskModel originalTask, TaskModel updatedTask)
        {
            originalTask.Title = updatedTask.Title;
            originalTask.Client = updatedTask.Client;
            originalTask.Description = updatedTask.Description;
            originalTask.StartDate = updatedTask.StartDate;
            originalTask.DueDate = updatedTask.DueDate;
            originalTask.RequestedDate = updatedTask.RequestedDate;
            originalTask.CompletionDate = updatedTask.CompletionDate;
            originalTask.PlannedTimeDays = updatedTask.PlannedTimeDays;
            originalTask.PlannedTimeHours = updatedTask.PlannedTimeHours;
            originalTask.ActualTimeDays = updatedTask.ActualTimeDays;
            originalTask.ActualTimeHours = updatedTask.ActualTimeHours;
        }

        private void ReorganizeTasks(KanbanViewModel viewModel, TaskModel task)
        {
            // Supprimer la tâche de toutes les collections
            RemoveTaskFromAllCollections(viewModel, task);

            // Conditions de réorganisation
            if (task.CompletionDate.HasValue && task.CompletionDate.Value.Date <= DateTime.Today)
            {
                // Tâche terminée si date de finalisation est aujourd'hui ou passée
                viewModel.DoneTasks.Add(task);
            }
            else if (task.StartDate.HasValue || task.DueDate.HasValue)
            {
                // Tâche en cours : uniquement date de début OU date prévue
                viewModel.InProgressTasks.Add(task);
            }
            else
            {
                // Par défaut dans tâches à faire
                viewModel.TodoTasks.Add(task);
            }
        }

        private void RemoveTaskFromAllCollections(KanbanViewModel viewModel, TaskModel task)
        {
            viewModel.TodoTasks.Remove(task);
            viewModel.InProgressTasks.Remove(task);
            viewModel.TestingTasks.Remove(task);
            viewModel.DoneTasks.Remove(task);
        }
    }
}