using Timer.Models;
using Timer.Service;
using Timer.Services;

namespace Timer.Views
{
    /// <summary>
    /// Provides the console-based user interface for viewing, editing, deleting, and resuming recorded task time logs.
    /// </summary>
    public class TaskHistoryView
    {
        private const int MaxRecentLogs = 5;
        private readonly TaskHistoryServices _taskHistoryService;
        private readonly TimerServices _timerService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskHistoryView"/> class.
        /// </summary>
        /// <param name="taskHistoryService">The service used to retrieve and modify user task logs.</param>
        /// <param name="timerService">The active timer service engine used to resume tasks.</param>
        public TaskHistoryView(TaskHistoryServices taskHistoryService, TimerServices timerService)
        {
            this._taskHistoryService = taskHistoryService;
            this._timerService = timerService;
        }

        /// <summary>
        /// Displays the last 5 recorded task logs with interactive management prompts for editing, deleting, or resuming.
        /// </summary>
        /// <param name="user">The currently authenticated <see cref="User"/>.</param>
        public void ShowLogs(User user)
        {
            while (true)
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

                // Slice data to grab the 5 absolute most recent tasks
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
                Console.WriteLine(new string('-', 120));

                Console.WriteLine("\n[E] Edit Name | [D] Delete Log | [R] Resume Task | [B] Back to Dashboard");
                Console.Write("Select an action: ");
                string choice = Console.ReadLine()?.ToUpper() ?? string.Empty;

                if (choice == "B")
                {
                    break;
                }

                if (choice == "E" || choice == "D" || choice == "R")
                {
                    Console.Write($"Enter task # (1-{recentTasks.Count}): ");
                    if (int.TryParse(Console.ReadLine(), out int index) && index >= 1 && index <= recentTasks.Count)
                    {
                        TaskItem selectedTask = recentTasks[index - 1];

                        switch (choice)
                        {
                            case "E": this.HandleEdit(selectedTask); break;
                            case "D": this.HandleDelete(selectedTask); break;
                            case "R": this.HandleResume(selectedTask); return; // Exit view to hand off to active dashboard loop
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid selection. Press any key to try again...");
                        Console.ReadKey();
                    }
                }
            }
        }

        /// <summary>
        /// Visual workspace engine wrapper that coordinates file generation pathways and displays delivery notifications.
        /// </summary>
        /// <param name="user">The currently authenticated user context whose records are being targeted.</param>
        public void TriggerHistoryExport(User user)
        {
            Console.Clear();
            Console.WriteLine("==================== EXPORT HISTORY DATA ====================");
            Console.Write("Enter number of recent records to export (or press Enter for all): ");

            string? input = Console.ReadLine();
            int? limit = null;

            if (!string.IsNullOrWhiteSpace(input))
            {
                if (int.TryParse(input, out int parsedLimit) && parsedLimit > 0)
                {
                    limit = parsedLimit;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Invalid number entered. Defaulting to exporting all records...");
                    Console.ResetColor();
                }
            }

            Console.WriteLine("\nPreparing records for data compilation...");

            try
            {
                // Pass the optional limit into the updated service method
                string destinationPath = this._taskHistoryService.ExportTasksToCsv(user.UserId, user.Name, limit);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[SUCCESS] Export process finished flawlessly!");
                Console.ResetColor();
                Console.WriteLine($"File location: {destinationPath}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] Failed to compile text matrix streams: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to return to dashboard...");
            Console.ReadKey();
        }

        /// <summary>
        /// Handles the interactive console workflow for updating an existing task's title.
        /// </summary>
        /// <param name="task">The <see cref="TaskItem"/> context to edit.</param>
        private void HandleEdit(TaskItem task)
        {
            Console.Clear();
            Console.WriteLine("==================== EDIT TASK NAME ====================");
            Console.WriteLine($"Current Name: {task.TaskName}");
            Console.Write("Enter new task name (leave blank to cancel): ");

            string newName = Console.ReadLine()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(newName))
            {
                task.TaskName = newName;
                this._taskHistoryService.UpdateTask(task);
                Console.WriteLine("\nTask updated successfully! Press any key to continue...");
            }
            else
            {
                Console.WriteLine("\nNo changes made. Press any key to continue...");
            }
            Console.ReadKey();
        }

        /// <summary>
        /// Handles the interactive console workflow for confirming and executing a task deletion.
        /// </summary>
        /// <param name="task">The <see cref="TaskItem"/> context to remove.</param>
        private void HandleDelete(TaskItem task)
        {
            Console.Write($"\nAre you sure you want to permanently delete '{task.TaskName}'? (Y/N): ");
            if (Console.ReadLine()?.ToUpper() == "Y")
            {
                this._taskHistoryService.DeleteTask(task.TaskId);
                Console.WriteLine("Task deleted successfully. Press any key to continue...");
            }
            else
            {
                Console.WriteLine("Deletion cancelled. Press any key to continue...");
            }
            Console.ReadKey();
        }

        /// <summary>
        /// Attaches the selected historical task back to the active timer tracking engine.
        /// </summary>
        /// <param name="task">The <see cref="TaskItem"/> context to load and resume tracking.</param>
        private void HandleResume(TaskItem task)
        {
            Console.Clear();
            Console.WriteLine("==================== RESUMING HISTORICAL TASK ====================");
            Console.WriteLine($"Resuming tracking context for: '{task.TaskName}'");
            Console.WriteLine($"Previously Logged Time   : {FormatDuration(task.ElapsedTime)}");
            Console.WriteLine("\nLoading stopwatch core engine... tracking resumed.");

            this._timerService.ResumeExistingTask(task);

            Console.WriteLine("\nResumed successfully! Press any key to jump back into your active dashboard loop...");
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
