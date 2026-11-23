using Timer.Models;
using Timer.Services;

namespace Timer.Views
{
    /// <summary>
    /// Provides the console-based dashboard interface for creating and timing tasks, viewing logs, and importing or exporting data.
    /// </summary>
    public class TimerView
    {
        private readonly TimerServices _timerService;
        private bool _targetReachedBannerShown;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerView"/> class.
        /// </summary>
        /// <param name="timerService">The service used to manage task creation, timing, and persistence.</param>
        public TimerView(TimerServices timerService)
        {
            this._timerService = timerService;
        }

        /// <summary>
        /// Displays the main dashboard menu and handles navigation for the logged-in user.
        /// </summary>
        /// <param name="user">The currently authenticated <see cref="User"/>.</param>
        public void ShowDashboard(User user)
        {
            while (true)
            {
                Console.Clear();

                Console.WriteLine("========== TASK TIMER DASHBOARD ==========");
                Console.WriteLine($"Welcome, {user.Name} ({user.Email})!");
                Console.WriteLine();
                Console.WriteLine("1. New Task");
                Console.WriteLine("2. Logs");
                Console.WriteLine("3. Import");
                Console.WriteLine("4. Export");
                Console.WriteLine("5. Logout");
                Console.WriteLine();

                Console.Write("Enter choice: ");
                string choice = Console.ReadLine() ?? string.Empty;

                switch (choice)
                {
                    case "1":
                        this.CreateAndRunTask(user);
                        break;

                    case "2":
                        this.ShowLogsMock();
                        break;

                    case "3":
                        this.ShowImportMock();
                        break;

                    case "4":
                        this.ShowExportMock();
                        break;

                    case "5":
                        return;

                    default:
                        Console.WriteLine("Invalid choice.");
                        Console.ReadKey();
                        break;
                }
            }
        }

        /// <summary>
        /// Prompts the user for task details and target duration (hours, minutes, and seconds), then starts the live task timer.
        /// </summary>
        /// <param name="user">The <see cref="User"/> creating the task.</param>
        private void CreateAndRunTask(User user)
        {
            Console.Clear();

            Console.WriteLine("========== NEW TASK ==========");
            Console.Write("Enter task name: ");
            string taskName = Console.ReadLine() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(taskName))
            {
                Console.WriteLine("\nTask name cannot be empty.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\nEnter target duration (press Enter for 0):");

            if (!this.TryReadDurationUnit("  Hours   : ", out int hours) ||
                !this.TryReadDurationUnit("  Minutes : ", out int minutes) ||
                !this.TryReadDurationUnit("  Seconds : ", out int seconds))
            {
                Console.WriteLine("\nInvalid duration. Please enter a non-negative whole number.");
                Console.ReadKey();
                return;
            }

            TimeSpan targetTime = new TimeSpan(hours, minutes, seconds);

            Console.WriteLine($"\nTarget Duration Set: {this.FormatDuration(targetTime)}");
            Console.WriteLine("Press ENTER to start the timer, or any other key to cancel...");

            if (Console.ReadKey(true).Key != ConsoleKey.Enter)
            {
                Console.WriteLine("\nTask cancelled.");
                Console.ReadKey();
                return;
            }

            this._targetReachedBannerShown = false;
            this._timerService.OnTargetTimeReached += this.HandleTargetTimeReached;

            try
            {
                TaskItem activeTask = this._timerService.StartNewTask(
                    user.UserId,
                    taskName,
                    targetTime);

                this.RunLiveTimer(activeTask);
            }
            finally
            {
                this._timerService.OnTargetTimeReached -= this.HandleTargetTimeReached;
            }
        }

        /// <summary>
        /// Prompts the user for a single non-negative integer time unit, defaulting to <c>0</c> if left blank.
        /// </summary>
        /// <param name="prompt">The console prompt to display.</param>
        /// <param name="value">When this method returns, contains the parsed non-negative integer value.</param>
        /// <returns><see langword="true"/> if input is blank or a valid non-negative integer; otherwise, <see langword="false"/>.</returns>
        private bool TryReadDurationUnit(string prompt, out int value)
        {
            Console.Write(prompt);
            string input = Console.ReadLine() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                value = 0;
                return true;
            }

            return int.TryParse(input, out value) && value >= 0;
        }

        /// <summary>
        /// Handles the <see cref="TimerServices.OnTargetTimeReached"/> event by playing a console alert and displaying a notification banner.
        /// </summary>
        /// <param name="task">The <see cref="TaskItem"/> whose effective target duration was reached.</param>
        private void HandleTargetTimeReached(TaskItem task)
        {
            this._targetReachedBannerShown = true;
            TimeSpan effectiveTarget = task.TargetTime + task.ExtendedTime;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[NOTIFICATION] Target time ({this.FormatDuration(effectiveTarget)}) reached for \"{task.TaskName}\"!");
            Console.ResetColor();
            Console.WriteLine();

            try
            {
                Console.Beep();
            }
            catch
            {
                // Ignore if system beep is unsupported on the current terminal.
            }
        }

        /// <summary>
        /// Displays a live stopwatch screen supporting pause, resume, target extension, and stop-and-save controls.
        /// </summary>
        /// <param name="task">The active <see cref="TaskItem"/> being timed.</param>
        private void RunLiveTimer(TaskItem task)
        {
            this.RenderTimerHeader(task);

            while (true)
            {
                while (!Console.KeyAvailable)
                {
                    TimeSpan elapsed = this._timerService.GetElapsedTime();
                    string status = this._timerService.IsRunning ? "RUNNING" : "PAUSED ";
                    string targetFlag = this._targetReachedBannerShown ? " [TARGET REACHED]" : string.Empty;

                    Console.Write($"\r[{status}] Elapsed: {this.FormatDuration(elapsed)}{targetFlag}   ");
                    Thread.Sleep(200);
                }

                ConsoleKey key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.P)
                {
                    if (this._timerService.IsRunning)
                    {
                        this._timerService.Pause();
                    }
                    else
                    {
                        this._timerService.Resume();
                    }
                }
                else if (key == ConsoleKey.E)
                {
                    this.PromptExtendTargetTime(task);
                    this.RenderTimerHeader(task);
                }
                else if (key == ConsoleKey.S)
                {
                    TaskItem? completedTask = this._timerService.StopAndSaveTask();

                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("========== TASK SAVED ==========");

                    if (completedTask != null)
                    {
                        Console.WriteLine($"Task       : {completedTask.TaskName}");
                        Console.WriteLine($"Start Date : {completedTask.StartDate}");
                        Console.WriteLine($"Start Time : {completedTask.StartTime:hh:mm:ss tt}");
                        Console.WriteLine($"End Time   : {completedTask.EndTime:hh:mm:ss tt}");
                        Console.WriteLine($"Target     : {this.FormatDuration(completedTask.TargetTime)}");
                        Console.WriteLine($"Extended   : +{this.FormatDuration(completedTask.ExtendedTime)}");
                        Console.WriteLine($"Duration   : {this.FormatDuration(completedTask.TotalDuration)}");
                    }

                    Console.WriteLine("\nPress any key to return to dashboard...");
                    Console.ReadKey();
                    return;
                }
            }
        }

        /// <summary>
        /// Renders the static task metadata and keyboard controls at the top of the live timer screen.
        /// </summary>
        /// <param name="task">The active <see cref="TaskItem"/> being timed.</param>
        private void RenderTimerHeader(TaskItem task)
        {
            Console.Clear();

            Console.WriteLine("========== TASK TIMER ==========");
            Console.WriteLine($"Task       : {task.TaskName}");
            Console.WriteLine($"Start Date : {task.StartDate}");
            Console.WriteLine($"Start Time : {task.StartTime:hh:mm:ss tt}");
            Console.WriteLine($"Target     : {this.FormatDuration(task.TargetTime)}");
            Console.WriteLine($"Extended   : +{this.FormatDuration(task.ExtendedTime)} (Total Target: {this.FormatDuration(task.TargetTime + task.ExtendedTime)})");
            Console.WriteLine();
            Console.WriteLine("[P] Pause/Resume   [E] Extend Target   [S] Stop & Save");
            Console.WriteLine();
        }

        /// <summary>
        /// Prompts the user for additional hours, minutes, and seconds to extend the active task's target duration.
        /// </summary>
        /// <param name="task">The active <see cref="TaskItem"/> whose target is being extended.</param>
        private void PromptExtendTargetTime(TaskItem task)
        {
            bool wasRunning = this._timerService.IsRunning;
            if (wasRunning)
            {
                this._timerService.Pause();
            }

            Console.WriteLine();
            Console.WriteLine("\n--- Extend Target Time (Timer Paused) ---");

            if (this.TryReadDurationUnit("  Add Hours   : ", out int hours) &&
                this.TryReadDurationUnit("  Add Minutes : ", out int minutes) &&
                this.TryReadDurationUnit("  Add Seconds : ", out int seconds))
            {
                TimeSpan extension = new TimeSpan(hours, minutes, seconds);

                if (this._timerService.ExtendTargetTime(extension))
                {
                    TimeSpan effectiveTarget = task.TargetTime + task.ExtendedTime;
                    if (this._timerService.GetElapsedTime() < effectiveTarget)
                    {
                        this._targetReachedBannerShown = false;
                    }

                    Console.WriteLine($"\nExtended by {this.FormatDuration(extension)}. Total Extended: +{this.FormatDuration(task.ExtendedTime)} | New Target: {this.FormatDuration(effectiveTarget)}");
                }
                else
                {
                    Console.WriteLine("\nNo time added.");
                }
            }
            else
            {
                Console.WriteLine("\nInvalid duration. Extension cancelled.");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);

            if (wasRunning)
            {
                this._timerService.Resume();
            }
        }

        /// <summary>
        /// Formats a <see cref="TimeSpan"/> into a standard <c>HH:mm:ss</c> display string.
        /// </summary>
        /// <param name="duration">The duration to format.</param>
        /// <returns>A formatted <c>HH:mm:ss</c> string.</returns>
        private string FormatDuration(TimeSpan duration)
        {
            return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        /// <summary>
        /// Displays a placeholder screen for viewing recorded task time logs.
        /// </summary>
        private void ShowLogsMock()
        {
            Console.Clear();
            Console.WriteLine("========== TIME LOGS ==========");
            Console.WriteLine("[Mock] Recorded task durations and history will appear here.");
            Console.WriteLine("\nPress any key to return to dashboard...");
            Console.ReadKey();
        }

        /// <summary>
        /// Displays a placeholder screen for importing task logs from an external file.
        /// </summary>
        private void ShowImportMock()
        {
            Console.Clear();
            Console.WriteLine("========== IMPORT TASKS ==========");
            Console.WriteLine("[Mock] Import tasks from JSON/CSV file will appear here.");
            Console.WriteLine("\nPress any key to return to dashboard...");
            Console.ReadKey();
        }

        /// <summary>
        /// Displays a placeholder screen for exporting task logs to an external file.
        /// </summary>
        private void ShowExportMock()
        {
            Console.Clear();
            Console.WriteLine("========== EXPORT TASKS ==========");
            Console.WriteLine("[Mock] Export tasks to JSON/CSV file will appear here.");
            Console.WriteLine("\nPress any key to return to dashboard...");
            Console.ReadKey();
        }
    }
}
