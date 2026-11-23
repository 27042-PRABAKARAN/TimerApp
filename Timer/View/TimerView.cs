using Timer.Models;

namespace Timer.Views
{
    /// <summary>
    /// Provides a mock dashboard console interface for managing tasks, viewing logs, and importing or exporting data.
    /// </summary>
    public class TimerView
    {
        /// <summary>
        /// Displays the main dashboard menu and handles mock navigation for the logged-in user.
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
                        this.ShowNewTaskMock();
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
        /// Displays a placeholder screen for creating and timing a new task.
        /// </summary>
        private void ShowNewTaskMock()
        {
            Console.Clear();
            Console.WriteLine("========== NEW TASK ==========");
            Console.WriteLine("[Mock] Task creation and live timer will appear here.");
            Console.WriteLine("\nPress any key to return to dashboard...");
            Console.ReadKey();
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
