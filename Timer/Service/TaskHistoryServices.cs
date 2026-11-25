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

            return this.SaveTasksToCsv(tasks, userName);
        }

        /// <summary>
        /// Exports task records for a specific user into a structured CSV file, filtered by a specific EndDate range.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="userName">The name of the user, used for the default file naming scheme.</param>
        /// <param name="filterStartDate">The starting date of the filter range (inclusive).</param>
        /// <param name="filterEndDate">The ending date of the filter range (inclusive).</param>
        /// <returns>The absolute file path where the CSV data was successfully saved.</returns>
        public string ExportTasksToCsv(Guid userId, string userName, DateOnly filterStartDate, DateOnly filterEndDate)
        {
            List<TaskItem> tasks = this.GetUserTasks(userId);
            tasks = tasks
                .Where(t => t.EndDate.HasValue &&
                t.EndDate.Value >= filterStartDate &&
                t.EndDate.Value <= filterEndDate)
                .OrderByDescending(t => t.EndDate)
                .ThenByDescending(t => t.EndTime)
                .ToList();

            return this.SaveTasksToCsv(tasks, userName);
        }

        /// <summary>
        /// Imports task logs from a structured CSV file and saves them to the repository for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user who will own these logs.</param>
        /// <param name="filePath">The absolute or relative path to the source CSV file.</param>
        /// <returns>The total number of tasks successfully parsed and saved to the repository.</returns>
        public int ImportTasksFromCsv(Guid userId, string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified CSV file could not be found.", filePath);
            }

            int successCount = 0;

            using (StreamReader reader = new StreamReader(filePath, System.Text.Encoding.UTF8))
            {
                // Skip the header row
                string? headerLine = reader.ReadLine();
                if (headerLine == null)
                {
                    return 0;
                }

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // Custom parse to handle commas wrapped inside double quotes safely
                    List<string> columns = new List<string>();
                    bool inQuotes = false;
                    int start = 0;

                    for (int i = 0; i < line.Length; i++)
                    {
                        if (line[i] == '"')
                        {
                            inQuotes = !inQuotes;
                        }
                        else if (line[i] == ',' && !inQuotes)
                        {
                            columns.Add(line.Substring(start, i - start));
                            start = i + 1;
                        }
                    }

                    columns.Add(line.Substring(start));

                    if (columns.Count < 9)
                    {
                        continue;
                    }

                    try
                    {
                        string rawTaskName = columns[0].Trim();
                        if (rawTaskName.StartsWith("\"") && rawTaskName.EndsWith("\"") && rawTaskName.Length >= 2)
                        {
                            rawTaskName = rawTaskName.Substring(1, rawTaskName.Length - 2).Replace("\"\"", "\"");
                        }

                        // Combine DateOnly and Time representations to build proper DateTime instances for Start/End
                        DateOnly parsedStartDate = DateOnly.ParseExact(columns[1].Trim(), "yyyy-MM-dd");
                        TimeSpan parsedStartTime = TimeSpan.Parse(columns[3].Trim());
                        DateTime combinedStartTime = parsedStartDate.ToDateTime(TimeOnly.FromTimeSpan(parsedStartTime));

                        DateTime? combinedEndTime = null;
                        string rawEndDate = columns[2].Trim();
                        string rawEndTime = columns[4].Trim();

                        if (rawEndDate != "In Progress" && rawEndTime != "-")
                        {
                            DateOnly parsedEndDate = DateOnly.ParseExact(rawEndDate, "yyyy-MM-dd");
                            TimeSpan parsedEndTimeValue = TimeSpan.Parse(rawEndTime);
                            combinedEndTime = parsedEndDate.ToDateTime(TimeOnly.FromTimeSpan(parsedEndTimeValue));
                        }

                        TaskItem task = new TaskItem
                        {
                            TaskId = Guid.NewGuid(),
                            UserId = userId,
                            TaskName = rawTaskName,
                            StartDate = parsedStartDate,
                            EndDate = rawEndDate == "In Progress" ? null : DateOnly.ParseExact(rawEndDate, "yyyy-MM-dd"),
                            StartTime = combinedStartTime,
                            EndTime = combinedEndTime,
                            TargetTime = TimeSpan.Parse(columns[5].Trim()),
                            ExtendedTime = TimeSpan.Parse(columns[6].Trim()),
                            ElapsedTime = TimeSpan.Parse(columns[7].Trim()),
                            TotalDuration = TimeSpan.Parse(columns[8].Trim()),
                        };

                        // Persist the entity via your void Add method setup
                        this._taskRepository.Add(task);
                        successCount++;
                    }
                    catch
                    {
                        // Row conversion failed; skip record and proceed with execution loop
                        continue;
                    }
                }
            }

            return successCount;
        }

        /// <summary>
        /// Core reusable logic to handle CSV formatting, file naming, directory creation, and writing.
        /// </summary>
        private string SaveTasksToCsv(List<TaskItem> tasks, string userName)
        {
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
                    // Escape commas and double quotes in task names to prevent breaking columns
                    string safeTaskName = task.TaskName ?? string.Empty;
                    if (safeTaskName.Contains(",") || safeTaskName.Contains("\""))
                    {
                        safeTaskName = $"\"{safeTaskName.Replace("\"", "\"\"")}\"";
                    }

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
