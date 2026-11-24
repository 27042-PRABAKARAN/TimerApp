using System.Diagnostics;
using Timer.Interfaces;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// Represents the method that handles the event when a task reaches its target duration.
    /// </summary>
    /// <param name="task">The <see cref="TaskItem"/> that reached its target duration.</param>
    public delegate void TargetTimeReachedHandler(TaskItem task);

    /// <summary>
    /// Represents the method that handles the daily work start reminder notification.
    /// </summary>
    /// <param name="user">The <see cref="User"/> whose daily work start time has arrived.</param>
    public delegate void DailyWorkReminderHandler(User user);

    /// <summary>
    /// Manages task creation, stopwatch timing operations, target and daily notifications, and persistence of completed tasks.
    /// </summary>
    public class TimerServices
    {
        private readonly ITaskRepository _taskRepository;
        private readonly Stopwatch _stopwatch;
        private TaskItem? _activeTask;
        private bool _targetNotified;
        private TimeSpan _previouslyAccumulatedTime;

        private System.Timers.Timer? _dailyReminderTimer;
        private User? _monitoredUser;
        private DateOnly? _lastDailyNotificationDate;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerServices"/> class.
        /// </summary>
        /// <param name="taskRepository">The repository used to persist and retrieve task items.</param>
        public TimerServices(ITaskRepository taskRepository)
        {
            this._taskRepository = taskRepository;
            this._stopwatch = new Stopwatch();
        }

        /// <summary>
        /// Occurs once when the active task's elapsed time reaches or exceeds its effective target duration (<see cref="TaskItem.TargetTime"/> + <see cref="TaskItem.ExtendedTime"/>).
        /// </summary>
        public event TargetTimeReachedHandler? OnTargetTimeReached;

        /// <summary>
        /// Occurs once per day when the system clock reaches the logged-in user's <see cref="User.DayStartTime"/>.
        /// </summary>
        public event DailyWorkReminderHandler? OnDailyWorkReminder;

        /// <summary>
        /// Gets a value indicating whether the stopwatch is currently running.
        /// </summary>
        /// <value><see langword="true"/> if the timer is running; otherwise, <see langword="false"/>.</value>
        public bool IsRunning => this._stopwatch.IsRunning;

        /// <summary>
        /// Gets the task currently being timed, or <see langword="null"/> if no task is active.
        /// </summary>
        /// <value>The active <see cref="TaskItem"/> instance, if any.</value>
        public TaskItem? ActiveTask => this._activeTask;

        /// <summary>
        /// Starts monitoring the system clock for the specified user's daily work start time.
        /// </summary>
        /// <param name="user">The logged-in <see cref="User"/> to notify.</param>
        public void StartDailyReminderMonitor(User user)
        {
            this.StopDailyReminderMonitor();

            this._monitoredUser = user;
            this._lastDailyNotificationDate = null;

            this._dailyReminderTimer = new System.Timers.Timer(1000);
            this._dailyReminderTimer.Elapsed += (sender, e) => this.CheckDailyWorkStartTime();
            this._dailyReminderTimer.AutoReset = true;
            this._dailyReminderTimer.Start();
        }

        /// <summary>
        /// Stops monitoring for the daily work start time and releases timer resources.
        /// </summary>
        public void StopDailyReminderMonitor()
        {
            if (this._dailyReminderTimer != null)
            {
                this._dailyReminderTimer.Stop();
                this._dailyReminderTimer.Dispose();
                this._dailyReminderTimer = null;
            }

            this._monitoredUser = null;
        }

        /// <summary>
        /// Creates a new task and immediately starts the stopwatch, recording <see cref="TaskItem.StartDate"/> and <see cref="TaskItem.StartTime"/>.
        /// </summary>
        /// <param name="userId">The unique identifier of the user starting the task.</param>
        /// <param name="taskName">The name or title of the task.</param>
        /// <param name="targetTime">The initial planned target duration for the task.</param>
        /// <returns>The newly created and active <see cref="TaskItem"/> instance.</returns>
        public TaskItem StartNewTask(Guid userId, string taskName, TimeSpan targetTime)
        {
            DateTime now = DateTime.Now;

            this._activeTask = new TaskItem
            {
                TaskId = Guid.NewGuid(),
                UserId = userId,
                TaskName = taskName.Trim(),
                StartDate = DateOnly.FromDateTime(now),
                EndDate = null,
                StartTime = now,
                EndTime = null,
                TargetTime = targetTime,
                ExtendedTime = TimeSpan.Zero,
                ElapsedTime = TimeSpan.Zero,
                TotalDuration = TimeSpan.Zero,
            };

            this._previouslyAccumulatedTime = TimeSpan.Zero;
            this._targetNotified = false;
            this._stopwatch.Restart();
            return this._activeTask;
        }

        /// <summary>
        /// Loads a previously saved historical task context and restarts the stopwatch tracking engine.
        /// </summary>
        /// <param name="task">The historical <see cref="TaskItem"/> record to resume tracking for.</param>
        public void ResumeExistingTask(TaskItem task)
        {
            this._activeTask = task;
            this._activeTask.EndDate = null;
            this._activeTask.EndTime = null;

            this._previouslyAccumulatedTime = task.ElapsedTime;
            this._targetNotified = false;
            this._stopwatch.Restart();
        }

        /// <summary>
        /// Pauses the active stopwatch without resetting the accumulated duration.
        /// </summary>
        public void Pause()
        {
            if (this._stopwatch.IsRunning)
            {
                this._stopwatch.Stop();
            }
        }

        /// <summary>
        /// Resumes a paused stopwatch.
        /// </summary>
        public void Resume()
        {
            if (this._activeTask != null && !this._stopwatch.IsRunning)
            {
                this._stopwatch.Start();
            }
        }

        /// <summary>
        /// Gets the elapsed duration recorded so far and triggers <see cref="OnTargetTimeReached"/> if the effective target time is met.
        /// </summary>
        /// <returns>A <see cref="TimeSpan"/> representing the current elapsed time.</returns>
        public TimeSpan GetElapsedTime()
        {
            TimeSpan totalElapsed = this._previouslyAccumulatedTime + this._stopwatch.Elapsed;

            if (this._activeTask != null)
            {
                TimeSpan effectiveTarget = this._activeTask.TargetTime + this._activeTask.ExtendedTime;

                if (!this._targetNotified
                    && effectiveTarget > TimeSpan.Zero
                    && totalElapsed >= effectiveTarget)
                {
                    this._targetNotified = true;
                    this.OnTargetTimeReached?.Invoke(this._activeTask);
                }
            }

            return totalElapsed;
        }

        /// <summary>
        /// Stops the stopwatch, sets <see cref="TaskItem.EndDate"/>, <see cref="TaskItem.EndTime"/>, <see cref="TaskItem.ElapsedTime"/>, and <see cref="TaskItem.TotalDuration"/>, saves the task, and clears the active task.
        /// </summary>
        /// <returns>The completed and saved <see cref="TaskItem"/>, or <see langword="null"/> if no task was active.</returns>
        public TaskItem? StopAndSaveTask()
        {
            if (this._activeTask == null)
            {
                return null;
            }

            this._stopwatch.Stop();

            DateTime endNow = DateTime.Now;
            this._activeTask.EndDate = DateOnly.FromDateTime(endNow);
            this._activeTask.EndTime = endNow;
            this._activeTask.ElapsedTime = this._previouslyAccumulatedTime + this._stopwatch.Elapsed;
            this._activeTask.TotalDuration = endNow - this._activeTask.StartTime;

            this._taskRepository.Add(this._activeTask);

            TaskItem completedTask = this._activeTask;
            this._activeTask = null;
            this._previouslyAccumulatedTime = TimeSpan.Zero;
            this._targetNotified = false;
            this._stopwatch.Reset();

            return completedTask;
        }

        /// <summary>
        /// Adds extra time to <see cref="TaskItem.ExtendedTime"/> and re-arms the target notification if the new effective target exceeds the current elapsed time.
        /// </summary>
        /// <param name="additionalTime">The extra <see cref="TimeSpan"/> to add to the task's extended duration.</param>
        /// <returns><see langword="true"/> if an active task was updated with a positive duration; otherwise, <see langword="false"/>.</returns>
        public bool ExtendTargetTime(TimeSpan additionalTime)
        {
            if (this._activeTask == null || additionalTime <= TimeSpan.Zero)
            {
                return false;
            }

            this._activeTask.ExtendedTime += additionalTime;

            TimeSpan totalElapsed = this._previouslyAccumulatedTime + this._stopwatch.Elapsed;
            TimeSpan effectiveTarget = this._activeTask.TargetTime + this._activeTask.ExtendedTime;

            if (totalElapsed < effectiveTarget)
            {
                this._targetNotified = false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the current time matches the monitored user's <see cref="User.DayStartTime"/> and invokes <see cref="OnDailyWorkReminder"/> once per day.
        /// </summary>
        private void CheckDailyWorkStartTime()
        {
            if (this._monitoredUser == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateOnly today = DateOnly.FromDateTime(now);
            TimeOnly currentTime = TimeOnly.FromDateTime(now);

            if (this._lastDailyNotificationDate != today &&
                currentTime.Hour == this._monitoredUser.DayStartTime.Hour &&
                currentTime.Minute == this._monitoredUser.DayStartTime.Minute)
            {
                this._lastDailyNotificationDate = today;
                this.OnDailyWorkReminder?.Invoke(this._monitoredUser);
            }
        }
    }
}
