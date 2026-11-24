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
        private readonly TaskHistoryView _logView;
        private bool _targetReachedBannerShown;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerView"/> class.
        /// </summary>
        /// <param name="timerService">The service used to manage task creation, timing, and persistence.</param>
        /// <param name="logView">The view responsible for displaying recorded task logs.</param>
        public TimerView(TimerServices timerService, TaskHistoryView logView)
        {
            this._timerService = timerService;
            this._logView = logView;
        }

        /// <summary>
        /// Displays the main dashboard menu and handles navigation for the logged-in user.
        /// </summary>
        /// <param name="user">The currently authenticated <see cref="User"/>.</param>
        public void ShowDashboard(User user)
        {
            this._timerService.OnDailyWorkReminder += this.HandleDailyWorkReminder;
            this._timerService.StartDailyReminderMonitor(user);

            try
            {
                while (true)
                {
                    Console.Clear();

                    Console.WriteLine("========== TASK TIMER DASHBOARD ==========");
                    Console.WriteLine($"Welcome, {user.Name} ({user.Email})!");
                    Console.WriteLine($"Daily Work Start Time: {user.DayStartTime:hh:mm tt}");
                    Console.WriteLine();
                    Console.WriteLine("1. New Task");
                    Console.WriteLine("2. History");
                    Console.WriteLine("3. Export Data");
                    Console.WriteLine("4. Logout");
                    Console.WriteLine();

                    Console.Write("Enter choice: ");
                    string choice = Console.ReadLine() ?? string.Empty;

                    switch (choice)
                    {
                        case "1":
                            this.CreateAndRunTask(user);
                            break;

                        case "2":
                            this._logView.ShowLogs(user);

                            // Check if a task was loaded into the engine from the History View resume selection
                            if (this._timerService.ActiveTask != null)
                            {
                                bool startNextImmediately = this.ExecuteTimerWorkflow(this._timerService.ActiveTask);

                                if (startNextImmediately)
                                {
                                    this.CreateAndRunTask(user);
                                }
                            }

                            break;

                        case "3":
                            // Route directly to your new custom file generation interface layout
                            this._logView.TriggerHistoryExport(user);
                            break;

                        case "4":
                            return;

                        default:
                            Console.WriteLine("Invalid choice.");
                            Console.ReadKey();
                            break;
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
        /// Prompts the user for task details and target duration (hours, minutes, and seconds) and setups execution context.
        /// </summary>
        /// <param name="user">The <see cref="User"/> creating the task.</param>
        private void CreateAndRunTask(User user)
        {
            while (true)
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

                // Initialize a clean state item and push straight into the shared timer engine loop
                TaskItem activeTask = this._timerService.StartNewTask(user.UserId, taskName, targetTime);
                bool loopNextTask = this.ExecuteTimerWorkflow(activeTask);

                if (!loopNextTask)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Attaches necessary event notifications, boots your active UI tracking environment loop, and detaches when finished.
        /// </summary>
        /// <param name="task">The <see cref="TaskItem"/> to be monitored live on screen.</param>
        /// <returns><see langword="true"/> if the caller should continue running the task creation workflow loop; otherwise, <see langword="false"/>.</returns>
        private bool ExecuteTimerWorkflow(TaskItem task)
        {
            this._targetReachedBannerShown = false;
            this._timerService.OnTargetTimeReached += this.HandleTargetTimeReached;

            try
            {
                return this.RunLiveTimer(task);
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
        /// Runs the interactive live-updating loop on the terminal screen, parsing tracking commands until the session stops.
        /// </summary>
        /// <param name="task">The active tracking context to map to the user interface.</param>
        /// <returns><see langword="true"/> if the caller should continue running the task creation loop; otherwise, <see langword="false"/>.</returns>
        private bool RunLiveTimer(TaskItem task)
        {
            // Initialise the display header card when the window launches
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
                        Console.WriteLine($"Task           : {completedTask.TaskName}");
                        Console.WriteLine($"Start Date     : {completedTask.StartDate}");
                        Console.WriteLine($"End Date       : {completedTask.EndDate}");
                        Console.WriteLine($"Start Time     : {completedTask.StartTime:hh:mm:ss tt}");
                        Console.WriteLine($"End Time       : {completedTask.EndTime:hh:mm:ss tt}");
                        Console.WriteLine($"Target         : {this.FormatDuration(completedTask.TargetTime)}");
                        Console.WriteLine($"Extended       : +{this.FormatDuration(completedTask.ExtendedTime)}");
                        Console.WriteLine($"Elapsed Worked : {this.FormatDuration(completedTask.ElapsedTime)}");
                        Console.WriteLine($"Total Duration : {this.FormatDuration(completedTask.TotalDuration)}");
                    }

                    Console.WriteLine();
                    Console.WriteLine("1. Start Next Task");
                    Console.WriteLine("2. Back to Dashboard");
                    Console.WriteLine();

                    while (true)
                    {
                        Console.Write("Enter choice: ");
                        string nextChoice = Console.ReadLine() ?? string.Empty;

                        if (nextChoice == "1")
                        {
                            return true;
                        }

                        if (nextChoice == "2")
                        {
                            return false;
                        }

                        Console.WriteLine("Invalid choice. Please enter 1 or 2.");
                    }
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
        }

        /// <summary>
        /// Handles the <see cref="TimerServices.OnDailyWorkReminder"/> event by playing a console alert and reminding the user to start working.
        /// </summary>
        /// <param name="user">The <see cref="User"/> whose daily start time has arrived.</param>
        private void HandleDailyWorkReminder(User user)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n[DAILY REMINDER] Good day, {user.Name}! It's {user.DayStartTime:hh:mm tt} — time to start your timer and begin work!");
            Console.ResetColor();
            Console.WriteLine();
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
    }
}
