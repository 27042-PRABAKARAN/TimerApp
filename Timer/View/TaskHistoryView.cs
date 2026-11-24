using Timer.Models;
using Timer.Service;

namespace Timer.Views
{
    /// <summary>
    /// Provides the console-based user interface for viewing and summarizing the most recent recorded task time logs.
    /// </summary>
    public class TaskHistoryView
    {
        private const int MaxRecentLogs = 5;
        private readonly TaskHistoryServices _taskHistoryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskHistoryView"/> class.
        /// </summary>
        /// <param name="taskHistoryService">The service used to retrieve user task logs.</param>
        public TaskHistoryView(TaskHistoryServices taskHistoryService)
        {
            this._taskHistoryService = taskHistoryService;
        }

        /// <summary>
        /// Displays the last 5 recorded task logs and summary totals for the specified user.
        /// </summary>
        /// <param name="user">The currently authenticated <see cref="User"/>.</param>
        public void ShowLogs(User user)
        {
            Console.Clear();

            Console.WriteLine("==================== RECENT TIME LOGS (LAST 5) ====================");
            Console.WriteLine($"User: {user.Name} ({user.Email})");
            Console.WriteLine();

            List<TaskItem> allTasks = this._taskHistoryService.GetUserTasks(user.UserId);

            if (allTasks.Count == 0)
            {
                Console.WriteLine("No task logs found yet. Start a new task from the dashboard!");
                Console.WriteLine("\nPress any key to return to dashboard...");
                Console.ReadKey();
                return;
            }

            List<TaskItem> recentTasks = allTasks.Take(MaxRecentLogs).ToList();

            Console.WriteLine(
                $"{"#",-4} {"Task Name",-20} {"Start Date",-12} {"End Date",-12} {"Start",-11} {"End",-11} {"Target",-10} {"Extended",-10} {"Worked",-10} {"Total",-10}");
            Console.WriteLine(new string('-', 120));

            TimeSpan recentWorked = TimeSpan.Zero;
            TimeSpan recentSession = TimeSpan.Zero;

            for (int i = 0; i < recentTasks.Count; i++)
            {
                TaskItem task = recentTasks[i];
                recentWorked += task.ElapsedTime;
                recentSession += task.TotalDuration;

                string taskName = task.TaskName.Length > 18
                    ? task.TaskName.Substring(0, 15) + "..."
                    : task.TaskName;

                string endDateText = task.EndDate?.ToString() ?? "In Progress";
                string endTimeText = task.EndTime?.ToString("hh:mm:ss tt") ?? "-";

                Console.WriteLine(
                    $"{i + 1,-4} " +
                    $"{taskName,-20} " +
                    $"{task.StartDate,-12} " +
                    $"{endDateText,-12} " +
                    $"{task.StartTime:hh:mm:ss tt} " +
                    $"{endTimeText,-11} " +
                    $"{FormatDuration(task.TargetTime),-10} " +
                    $"{FormatDuration(task.ExtendedTime),-10} " +
                    $"{FormatDuration(task.ElapsedTime),-10} " +
                    $"{FormatDuration(task.TotalDuration),-10}");
            }

            Console.WriteLine(new string('-', 120));
            Console.WriteLine($"Showing              : {recentTasks.Count} of {allTasks.Count} total tasks");
            Console.WriteLine($"Recent Active Worked : {FormatDuration(recentWorked)}");
            Console.WriteLine($"Recent Session Span  : {FormatDuration(recentSession)}");
            Console.WriteLine("\nPress any key to return to dashboard...");
            Console.ReadKey();
        }

        /// <summary>
        /// Formats a <see cref="TimeSpan"/> into a standard <c>HH:mm:ss</c> display string.
        /// </summary>
        /// <param name="duration">The duration to format.</param>
        /// <returns>A formatted <c>HH:mm:ss</c> string.</returns>
        private static string FormatDuration(TimeSpan duration)
        {
            return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }
}
