using Timer.Models;
using Timer.Services;

namespace Timer.Views
{
    /// <summary>
    /// Provides the console-based user interface for viewing and summarizing recorded task time logs.
    /// </summary>
    public class LogView
    {
        private readonly TimerServices _timerService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogView"/> class.
        /// </summary>
        /// <param name="timerService">The service used to retrieve user task logs.</param>
        public LogView(TimerServices timerService)
        {
            this._timerService = timerService;
        }

        /// <summary>
        /// Displays all recorded task logs and summary totals for the specified user.
        /// </summary>
        /// <param name="user">The currently authenticated <see cref="User"/>.</param>
        public void ShowLogs(User user)
        {
            Console.Clear();

            Console.WriteLine("==================== TIME LOGS ====================");
            Console.WriteLine($"User: {user.Name} ({user.Email})");
            Console.WriteLine();

            List<TaskItem> tasks = this._timerService.GetUserTasks(user.UserId);

            if (tasks.Count == 0)
            {
                Console.WriteLine("No task logs found yet. Start a new task from the dashboard!");
                Console.WriteLine("\nPress any key to return to dashboard...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine(
                $"{"#",-4} {"Task Name",-20} {"Start Date",-12} {"End Date",-12} {"Start",-11} {"End",-11} {"Target",-10} {"Extended",-10} {"Worked",-10} {"Total",-10}");
            Console.WriteLine(new string('-', 120));

            TimeSpan totalWorked = TimeSpan.Zero;
            TimeSpan totalSession = TimeSpan.Zero;

            for (int i = 0; i < tasks.Count; i++)
            {
                TaskItem task = tasks[i];
                totalWorked += task.ElapsedTime;
                totalSession += task.TotalDuration;

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
            Console.WriteLine($"Total Tasks Logged   : {tasks.Count}");
            Console.WriteLine($"Total Active Worked  : {FormatDuration(totalWorked)}");
            Console.WriteLine($"Total Session Span   : {FormatDuration(totalSession)}");
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
