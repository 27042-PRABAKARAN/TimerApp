using Timer.Models;

namespace Timer.Interfaces
{
    /// <summary>
    /// Defines the contract for data access operations related to <see cref="TaskItem"/> entities.
    /// </summary>
    public interface ITaskRepository
    {
        /// <summary>
        /// Adds a new task item to the repository.
        /// </summary>
        /// <param name="task">The <see cref="TaskItem"/> to add.</param>
        void Add(TaskItem task);

        /// <summary>
        /// Retrieves all task items associated with a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of <see cref="TaskItem"/> objects belonging to the specified user.</returns>
        List<TaskItem> GetByUserId(Guid userId);
    }
}
