using Spectre.Console;
using Timer.Models;
using Timer.Service;

namespace Timer.View
{
    /// <summary>
    /// Analytical dashboard page
    /// </summary>
    public class AnalyticalDashboard
    {
        private readonly TaskHistoryServices _taskHistoryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalyticalDashboard"/> class.
        /// </summary>
        /// <param name="taskHistoryService">The service used to retrieve and modify user task logs.</param>
        public AnalyticalDashboard(TaskHistoryServices taskHistoryService)
        {
            this._taskHistoryService = taskHistoryService;
        }

        /// <summary>
        /// Displays an analytical dashboard containing task statistics and work duration summaries.
        /// </summary>
        /// <param name="user">The currently authenticated user.</param>
        public void ShowAnalyticsDashboard(User user)
        {
            AnsiConsole.Clear();

            AnsiConsole.MarkupLine("[bold]ANALYTICAL DASHBOARD[/]");
            AnsiConsole.Write(new Rule());

            List<TaskItem> tasks = this._taskHistoryService.GetUserTasks(user.UserId);

            if (tasks.Count == 0)
            {
                AnsiConsole.MarkupLine(
                    "\n[grey]No task records available for analysis.[/]");

                this.WaitForUser();
                return;
            }

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            DateOnly weekStart = today.AddDays(
                -(((int)today.DayOfWeek + 6) % 7));

            DateOnly monthStart = new DateOnly(
                today.Year,
                today.Month,
                1);

            List<TaskItem> todayTasks = tasks
                .Where(t => t.StartDate == today)
                .ToList();

            List<TaskItem> weekTasks = tasks
                .Where(t => t.StartDate >= weekStart && t.StartDate <= today)
                .ToList();

            List<TaskItem> monthTasks = tasks
                .Where(t => t.StartDate >= monthStart && t.StartDate <= today)
                .ToList();

            int completedTasks = tasks.Count(t => t.EndDate.HasValue);
            int inProgressTasks = tasks.Count - completedTasks;

            TimeSpan totalWorked = TimeSpan.FromTicks(
                tasks.Sum(t => t.ElapsedTime.Ticks));

            TimeSpan totalExtended = TimeSpan.FromTicks(
                tasks.Sum(t => t.ExtendedTime.Ticks));

            TimeSpan totalTarget = TimeSpan.FromTicks(
                tasks.Sum(t => t.TargetTime.Ticks));

            TimeSpan todayWorked = TimeSpan.FromTicks(
                todayTasks.Sum(t => t.ElapsedTime.Ticks));

            TimeSpan weekWorked = TimeSpan.FromTicks(
                weekTasks.Sum(t => t.ElapsedTime.Ticks));

            TimeSpan monthWorked = TimeSpan.FromTicks(
                monthTasks.Sum(t => t.ElapsedTime.Ticks));

            TimeSpan averageWorked = completedTasks > 0
                ? TimeSpan.FromTicks(
                    totalWorked.Ticks / completedTasks)
                : TimeSpan.Zero;

            TaskItem? longestTask = tasks
                .OrderByDescending(t => t.ElapsedTime)
                .FirstOrDefault();

            double completionRate = tasks.Count > 0
                ? (double)completedTasks / tasks.Count * 100
                : 0;

            AnsiConsole.MarkupLine(
                $"User: [cyan]{Markup.Escape(user.Name)}[/]");

            AnsiConsole.MarkupLine(
                $"Report Date: [cyan]{today:dd-MM-yyyy}[/]\n");

            AnsiConsole.MarkupLine("[bold]TASK OVERVIEW[/]");

            var overviewTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Metric")
                .AddColumn(new TableColumn("Count").RightAligned());

            overviewTable.AddRow("Total Tasks", tasks.Count.ToString());
            overviewTable.AddRow("Completed Tasks", completedTasks.ToString());
            overviewTable.AddRow("In Progress Tasks", inProgressTasks.ToString());
            overviewTable.AddRow("Tasks Today", todayTasks.Count.ToString());
            overviewTable.AddRow("Tasks This Week", weekTasks.Count.ToString());
            overviewTable.AddRow("Tasks This Month", monthTasks.Count.ToString());
            overviewTable.AddRow("Completion Rate", $"{completionRate:F2}%");

            AnsiConsole.Write(overviewTable);

            AnsiConsole.MarkupLine("\n[bold]WORK DURATION ANALYSIS[/]");

            var durationTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Metric")
                .AddColumn(new TableColumn("Duration").RightAligned());

            durationTable.AddRow(
                "Total Time Worked",
                this.FormatDuration(totalWorked));

            durationTable.AddRow(
                "Total Extended Time",
                this.FormatDuration(totalExtended));

            durationTable.AddRow(
                "Total Target Time",
                this.FormatDuration(totalTarget));

            durationTable.AddRow(
                "Average Time Per Completed Task",
                this.FormatDuration(averageWorked));

            durationTable.AddRow(
                "Time Worked Today",
                this.FormatDuration(todayWorked));

            durationTable.AddRow(
                "Time Worked This Week",
                this.FormatDuration(weekWorked));

            durationTable.AddRow(
                "Time Worked This Month",
                this.FormatDuration(monthWorked));

            AnsiConsole.Write(durationTable);

            AnsiConsole.MarkupLine("\n[bold]TASK PERFORMANCE[/]");

            var performanceTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Metric")
                .AddColumn("Details");

            performanceTable.AddRow(
                "Longest Task",
                longestTask != null ? Markup.Escape(longestTask.TaskName) : "-");

            performanceTable.AddRow(
                "Longest Task Duration",
                longestTask != null ? this.FormatDuration(longestTask.ElapsedTime) : "00:00:00");

            AnsiConsole.Write(performanceTable);

            AnsiConsole.Write(new Rule());

            AnsiConsole.MarkupLine(
                "[grey]Week starts on Monday. Statistics are based on task start dates.[/]");

            this.WaitForUser();
        }

        /// <summary>
        /// Waits for the user to press a key before continuing.
        /// </summary>
        private void WaitForUser()
        {
            AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
            Console.ReadKey(true);
        }

        /// <summary>
        /// Formats a <see cref="TimeSpan"/> into a standard HH:mm:ss display string.
        /// </summary>
        /// <param name="duration">The duration to format.</param>
        /// <returns>The formatted duration string.</returns>
        private string FormatDuration(TimeSpan duration)
        {
            return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }
}
