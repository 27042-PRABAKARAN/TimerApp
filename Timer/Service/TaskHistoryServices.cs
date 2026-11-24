using Timer.Interfaces;
using Timer.Models;

namespace Timer.Service
{
    /// <summary>
    /// Provides operations for retrieving, querying, and modifying historical task logs for users.
    /// </summary>
    public class TaskHistoryServices
    {
        private readonly ITaskRepository _taskRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskHistoryServices"/> class.
        /// </summary>
        /// <param name="taskRepository">The repository used to retrieve stored task items.</param>
        public TaskHistoryServices(ITaskRepository taskRepository)
        {
            this._taskRepository = taskRepository;
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
        /// Retrieves a single task record by its unique identifier.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task.</param>
        /// <returns>The matching <see cref="TaskItem"/>, or <see langword="null"/> if not found.</returns>
        public TaskItem? GetTaskById(Guid taskId)
        {
            return this._taskRepository.GetById(taskId);
        }

        /// <summary>
        /// Updates the recorded details of an existing historical task log.
        /// </summary>
        /// <param name="task">The modified task entity to update.</param>
        /// <returns><see langword="true"/> if the update succeeded; otherwise, <see langword="false"/>.</returns>
        public bool UpdateTask(TaskItem task)
        {
            return this._taskRepository.Update(task);
        }

        /// <summary>
        /// Permanently removes a task log from historical records.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task log to delete.</param>
        /// <returns><see langword="true"/> if the task was found and deleted; otherwise, <see langword="false"/>.</returns>
        public bool DeleteTask(Guid taskId)
        {
            return this._taskRepository.Delete(taskId);
        }

        /// <summary>
        /// Exports task records for a specific user into a structured CSV file, optionally limited to the most recent entries.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="userName">The name of the user, used for the default file naming scheme.</param>
        /// <param name="limit">Optional. The maximum number of recent records to export. Exports all if null.</param>
        /// <returns>The absolute file path where the CSV data was successfully saved.</returns>
        public string ExportTasksToCsv(Guid userId, string userName, int? limit = null)
        {
            List<TaskItem> tasks = this.GetUserTasks(userId);

            if (limit.HasValue && limit.Value > 0)
            {
                tasks = tasks
                    .OrderByDescending(t => t.StartDate)
                    .ThenByDescending(t => t.StartTime)
                    .Take(limit.Value)
                    .ToList();
            }

            string exportsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Exports");
            Directory.CreateDirectory(exportsFolder);

            // Clean the username for safe file path creation
            string safeName = string.Concat(userName.Split(Path.GetInvalidFileNameChars()));
            string fileName = $"{safeName}_task_history_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string fullPath = Path.Combine(exportsFolder, fileName);

            using (StreamWriter writer = new StreamWriter(fullPath, false, System.Text.Encoding.UTF8))
            {
                // Write CSV Header row
                writer.WriteLine("TaskName,StartDate,EndDate,StartTime,EndTime,TargetTime,ExtendedTime,ElapsedTime,TotalDuration");

                foreach (var task in tasks)
                {
                    // Escape commas in task names to prevent shifting columns
                    string safeTaskName = task.TaskName.Contains(",") ? $"\"{task.TaskName}\"" : task.TaskName;

                    writer.WriteLine($"{safeTaskName}," +
                                     $"{task.StartDate:yyyy-MM-dd}," +
                                     $"{(task.EndDate.HasValue ? task.EndDate.Value.ToString("yyyy-MM-dd") : "In Progress")}," +
                                     $"{task.StartTime:HH:mm:ss}," +
                                     $"{(task.EndTime.HasValue ? task.EndTime.Value.ToString("HH:mm:ss") : "-")}," +
                                     $"{task.TargetTime}," +
                                     $"{task.ExtendedTime}," +
                                     $"{task.ElapsedTime}," +
                                     $"{task.TotalDuration}");
                }
            }

            return fullPath;
        }
    }
}
