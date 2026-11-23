using System.Text.Json;
using Timer.Interfaces;
using Timer.Models;

namespace Timer.Repositories
{
    /// <summary>
    /// Provides a JSON file-based implementation of <see cref="ITaskRepository"/> for managing <see cref="TaskItem"/> entities.
    /// </summary>
    public class JsonTaskRepository : ITaskRepository
    {
        private readonly string _filePath;
        private List<TaskItem> _tasks;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonTaskRepository"/> class,
        /// ensuring the data directory exists and loading existing tasks from <c>tasks.json</c>.
        /// </summary>
        public JsonTaskRepository()
        {
            string dataFolder = Path.Combine(
                Directory.GetCurrentDirectory(), "Data");

            Directory.CreateDirectory(dataFolder);

            this._filePath = Path.Combine(dataFolder, "tasks.json");

            if (File.Exists(this._filePath))
            {
                string json = File.ReadAllText(this._filePath);

                this._tasks = JsonSerializer.Deserialize<List<TaskItem>>(json)
                              ?? new List<TaskItem>();
            }
            else
            {
                this._tasks = new List<TaskItem>();
                this.Save();
            }
        }

        /// <summary>
        /// Adds a new task item to the in-memory collection and persists the changes to the JSON file.
        /// </summary>
        /// <param name="task">The <see cref="TaskItem"/> to add.</param>
        public void Add(TaskItem task)
        {
            this._tasks.Add(task);
            this.Save();
        }

        /// <summary>
        /// Retrieves all task items associated with a specific user, ordered by most recent start time.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of <see cref="TaskItem"/> objects belonging to the specified user.</returns>
        public List<TaskItem> GetByUserId(Guid userId)
        {
            return this._tasks
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.StartTime)
                .ToList();
        }

        /// <summary>
        /// Serializes the current list of tasks and writes it to the underlying JSON file.
        /// </summary>
        private void Save()
        {
            string json = JsonSerializer.Serialize(
                this._tasks,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                });

            File.WriteAllText(this._filePath, json);
        }
    }
}
