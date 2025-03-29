using System;
using System.Xml.Serialization;

namespace PlanifKanban.Models
{
    [Serializable]
    public class TaskModel
    {
        public string Title { get; set; }
        public string Client { get; set; }
        public string Description { get; set; }  // Nouvelle propriété
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? RequestedDate { get; set; }
        public DateTime? CompletionDate { get; set; }

        public double PlannedTimeDays { get; set; }
        public double PlannedTimeHours { get; set; }
        public double ActualTimeDays { get; set; }
        public double ActualTimeHours { get; set; }

        [XmlIgnore] // Ne pas sérialiser cette propriété calculée
        public bool HasDueDate
        {
            get { return DueDate.HasValue; }
        }
    }
}