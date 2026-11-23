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
    /// Manages task creation, stopwatch timing operations, target notifications, and persistence of completed tasks.
    /// </summary>
    public class TimerServices
    {
        private readonly ITaskRepository _taskRepository;
        private readonly Stopwatch _stopwatch;
        private TaskItem? _activeTask;
        private bool _targetNotified;

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
        /// Creates a new task and immediately starts the stopwatch, recording <see cref="TaskItem.StartDate"/> and <see cref="TaskItem.StartTime"/>.
        /// </summary>
        /// <param name="userId">The unique identifier of the user starting the task.</param>
        /// <param name="taskName">The name or title of the task.</param>
        /// <param name="targetTime">The initial planned target duration for the task.</param>
        /// <returns>The newly created and active <see cref="TaskItem"/>.</returns>
        public TaskItem StartNewTask(Guid userId, string taskName, TimeSpan targetTime)
        {
            DateTime now = DateTime.Now;

            this._activeTask = new TaskItem
            {
                TaskId = Guid.NewGuid(),
                UserId = userId,
                TaskName = taskName.Trim(),
                StartDate = DateOnly.FromDateTime(now),
                StartTime = now,
                EndTime = null,
                TargetTime = targetTime,
                ExtendedTime = TimeSpan.Zero,
                TotalDuration = TimeSpan.Zero,
            };

            this._targetNotified = false;
            this._stopwatch.Restart();
            return this._activeTask;
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
            TimeSpan elapsed = this._stopwatch.Elapsed;

            if (this._activeTask != null)
            {
                TimeSpan effectiveTarget = this._activeTask.TargetTime + this._activeTask.ExtendedTime;

                if (!this._targetNotified
                    && effectiveTarget > TimeSpan.Zero
                    && elapsed >= effectiveTarget)
                {
                    this._targetNotified = true;
                    this.OnTargetTimeReached?.Invoke(this._activeTask);
                }
            }

            return elapsed;
        }

        /// <summary>
        /// Stops the stopwatch, sets <see cref="TaskItem.EndTime"/> and <see cref="TaskItem.TotalDuration"/>, saves the task to the repository, and clears the active task.
        /// </summary>
        /// <returns>The completed and saved <see cref="TaskItem"/>, or <see langword="null"/> if no task was active.</returns>
        public TaskItem? StopAndSaveTask()
        {
            if (this._activeTask == null)
            {
                return null;
            }

            this._stopwatch.Stop();

            this._activeTask.EndTime = DateTime.Now;
            this._activeTask.TotalDuration = this._stopwatch.Elapsed;

            this._taskRepository.Add(this._activeTask);

            TaskItem completedTask = this._activeTask;
            this._activeTask = null;
            this._targetNotified = false;
            this._stopwatch.Reset();

            return completedTask;
        }

        /// <summary>
        /// Retrieves all saved task logs for the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of <see cref="TaskItem"/> records belonging to the user.</returns>
        public List<TaskItem> GetUserTasks(Guid userId)
        {
            return this._taskRepository.GetByUserId(userId);
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

            TimeSpan effectiveTarget = this._activeTask.TargetTime + this._activeTask.ExtendedTime;
            if (this._stopwatch.Elapsed < effectiveTarget)
            {
                this._targetNotified = false;
            }

            return true;
        }
    }
}
