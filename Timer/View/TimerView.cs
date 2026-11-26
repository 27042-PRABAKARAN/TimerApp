using System.Collections.Concurrent;
using Spectre.Console;
using Timer.Models;
using Timer.Services;
using Timer.View;

namespace Timer.Views
{
    /// <summary>
    /// Provides the console-based dashboard for creating tasks, tracking time, viewing task history, and managing task records.
    /// </summary>
    public class TimerView
    {
        private readonly TimerServices _timerService;
        private readonly TaskHistoryView _logView;
        private readonly ExportView _exportView;
        private readonly ImportView _importView;
        private readonly TaskView _taskView;
        private readonly AnalyticalDashboard _analyticalDashboard;
        private readonly ConcurrentQueue<string> _dailyReminderMessages = new ConcurrentQueue<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerView"/> class.
        /// </summary>
        /// <param name="timerService">The service used to manage task creation, timing, and persistence.</param>
        /// <param name="logView">The view responsible for displaying and managing task history.</param>
        /// <param name="exportView">The view responsible for displaying and managing Export history.</param>
        /// <param name="importView">The view responsible for displaying and managing Import history.</param>
        /// <param name="taskView">The view responsible for displaying and managing Task timer.</param>
        /// <param name="analyticalDashboard">The view responsible for displaying and managing analytical dashboard history.</param>
        public TimerView(TimerServices timerService, TaskHistoryView logView, ExportView exportView, ImportView importView, AnalyticalDashboard analyticalDashboard, TaskView taskView)
        {
            this._timerService = timerService;
            this._logView = logView;
            this._exportView = exportView;
            this._importView = importView;
            this._analyticalDashboard = analyticalDashboard;
            this._taskView = taskView;
        }

        /// <summary>
        /// Displays the main dashboard menu and handles navigation for the logged-in user.
        /// </summary>
        /// <param name="user">The currently authenticated user.</param>
        public void ShowDashboard(User user)
        {
            this._timerService.OnDailyWorkReminder += this.HandleDailyWorkReminder;
            this._timerService.StartDailyReminderMonitor(user);

            try
            {
                while (true)
                {
                    AnsiConsole.Clear();
                    this.DisplayDailyReminders();
                    this.RenderDashboard(user);

                    List<string> choices = new List<string>
                    {
                        this._timerService.ActiveTask != null
                            ? "Return to Active Task"
                            : "New Task",
                        "Task History",
                        "Export Data",
                        "Import Logs",
                        "Analytical Dashboard",
                        "Logout",
                    };

                    string choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Select an option")
                            .AddChoices(choices));

                    switch (choice)
                    {
                        case "Return to Active Task":
                            if (this._timerService.ActiveTask != null)
                            {
                                bool startNextImmediately = this._taskView.ExecuteTimerWorkflow(
                                    this._timerService.ActiveTask);

                                if (startNextImmediately)
                                {
                                    this._taskView.CreateAndRunTask(user);
                                }
                            }

                            break;

                        case "New Task":
                            this._taskView.CreateAndRunTask(user);
                            break;

                        case "Task History":
                            bool resumedFromHistory = this._logView.ShowLogs(user);

                            if (resumedFromHistory && this._timerService.ActiveTask != null)
                            {
                                bool startNextImmediately = this._taskView.ExecuteTimerWorkflow(
                                    this._timerService.ActiveTask);

                                if (startNextImmediately)
                                {
                                    this._taskView.CreateAndRunTask(user);
                                }
                            }

                            break;

                        case "Export Data":
                            this._exportView.TriggerHistoryExport(user);
                            break;

                        case "Import Logs":
                            this._importView.ImportData(user);
                            break;

                        case "Analytical Dashboard":
                            this._analyticalDashboard.ShowAnalyticsDashboard(user);
                            break;

                        case "Logout":
                            if (this._timerService.ActiveTask != null)
                            {
                                string logoutChoice = AnsiConsole.Prompt(
                                    new SelectionPrompt<string>()
                                        .Title("An active task is running. Select an option:")
                                        .AddChoices(
                                            "Save task and logout",
                                            "Return to dashboard"));

                                if (logoutChoice == "Save task and logout")
                                {
                                    this._timerService.StopAndSaveTask();
                                    return;
                                }

                                break;
                            }

                            return;
                    }
                }
            }
            finally
            {
                this._timerService.StopDailyReminderMonitor();
                this._timerService.OnDailyWorkReminder -= this.HandleDailyWorkReminder;
            }
        }

        /// <summary>
        /// Displays the dashboard information, including user details and the current task status.
        /// </summary>
        /// <param name="user">The currently authenticated user.</param>
        private void RenderDashboard(User user)
        {
            AnsiConsole.MarkupLine("[bold]TASK TIMER[/]");
            AnsiConsole.Write(new Rule());

            var userTable = new Table()
                .Border(TableBorder.Simple)
                .AddColumn("User")
                .AddColumn("Email")
                .AddColumn("Daily Start");

            userTable.AddRow(
                Markup.Escape(user.Name),
                Markup.Escape(user.Email),
                user.DayStartTime.ToString("hh:mm tt"));

            AnsiConsole.Write(userTable);

            TaskItem? activeTask = this._timerService.ActiveTask;

            AnsiConsole.MarkupLine("\n[bold]TASK STATUS[/]");

            if (activeTask != null)
            {
                string status = this._timerService.IsRunning
                    ? "[green]Running[/]"
                    : "[yellow]Paused[/]";

                AnsiConsole.MarkupLine($"Task: {Markup.Escape(activeTask.TaskName)}");
                AnsiConsole.MarkupLine($"Status: {status}");
                AnsiConsole.MarkupLine(
                    $"Elapsed: {this.FormatDuration(this._timerService.GetElapsedTime())}");

                AnsiConsole.MarkupLine(
                    $"Target: {this.FormatDuration(activeTask.TargetTime + activeTask.ExtendedTime)}");
            }
            else
            {
                AnsiConsole.MarkupLine("[grey]No active task.[/]");
            }

            AnsiConsole.WriteLine();
        }

        /// <summary>
        /// Queues a daily work reminder for display on the dashboard.
        /// </summary>
        /// <param name="user">The user receiving the reminder.</param>
        private void HandleDailyWorkReminder(User user)
        {
            this._dailyReminderMessages.Enqueue(
                $"Good day, {user.Name}! It is your scheduled work start time ({user.DayStartTime:hh:mm tt}).");
        }

        /// <summary>
        /// Displays pending daily work reminder messages.
        /// </summary>
        private void DisplayDailyReminders()
        {
            while (this._dailyReminderMessages.TryDequeue(out string? message))
            {
                AnsiConsole.MarkupLine(
                    $"[cyan]{Markup.Escape(message)}[/]\n");
            }
        }

        /// <summary>
        /// Formats a duration into HH:mm:ss.
        /// </summary>
        /// <param name="duration">The duration to format.</param>
        /// <returns>The formatted duration.</returns>
        private string FormatDuration(TimeSpan duration)
        {
            return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }
}