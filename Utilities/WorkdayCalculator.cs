using System;

namespace PlanifKanban.Utilities
{
    public static class WorkdayCalculator
    {
        // Vérifie si une date est un jour ouvrable (lundi-vendredi)
        public static bool IsWorkday(DateTime date)
        {
            return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
        }

        // Ajoute un nombre de jours ouvrables à une date
        public static DateTime AddWorkdays(DateTime startDate, int workDays)
        {
            DateTime result = startDate;
            int direction = workDays < 0 ? -1 : 1;
            workDays = Math.Abs(workDays);

            while (workDays > 0)
            {
                result = result.AddDays(direction);
                if (IsWorkday(result))
                {
                    workDays--;
                }
            }

            return result;
        }

        // Calcule le nombre de jours ouvrables entre deux dates
        public static int GetWorkdayCount(DateTime startDate, DateTime endDate)
        {
            // S'assurer que startDate <= endDate
            if (startDate > endDate)
            {
                var temp = startDate;
                startDate = endDate;
                endDate = temp;
            }

            int workDays = 0;
            DateTime current = startDate.Date;

            while (current <= endDate.Date)
            {
                if (IsWorkday(current))
                {
                    workDays++;
                }
                current = current.AddDays(1);
            }

            return workDays;
        }

        // Convertit le nombre de jours ouvrables en jours calendaires
        public static int WorkdaysToCalendarDays(DateTime startDate, int workDays)
        {
            DateTime endDate = AddWorkdays(startDate, workDays);
            return (endDate - startDate).Days;
        }

        // Obtenir la prochaine date ouvrable
        public static DateTime GetNextWorkday(DateTime date)
        {
            DateTime nextDay = date.AddDays(1);
            while (!IsWorkday(nextDay))
            {
                nextDay = nextDay.AddDays(1);
            }
            return nextDay;
        }

        // Obtenir la date ouvrable précédente
        public static DateTime GetPreviousWorkday(DateTime date)
        {
            DateTime prevDay = date.AddDays(-1);
            while (!IsWorkday(prevDay))
            {
                prevDay = prevDay.AddDays(-1);
            }
            return prevDay;
        }

        // Ajuste une date pour qu'elle tombe sur un jour ouvrable
        public static DateTime AdjustToWorkday(DateTime date)
        {
            // Si la date est déjà un jour ouvrable, la retourner telle quelle
            if (IsWorkday(date))
                return date;

            // Sinon, retourner le prochain jour ouvrable
            return GetNextWorkday(date);
        }
    }
}