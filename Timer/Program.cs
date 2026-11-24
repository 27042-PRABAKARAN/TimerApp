using Timer.Interfaces;
using Timer.Models;
using Timer.Repositories;
using Timer.Services;
using Timer.Views;

namespace Timer
{
    /// <summary>
    /// Entry point of the application.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            IUserRepository userRepository =
                new JsonUserRepository();

            ITaskRepository taskRepository =
                new JsonTaskRepository();

            AuthenticationServices authService =
                new AuthenticationServices(userRepository);

            TimerServices timerService =
                new TimerServices(taskRepository);

            AuthenticationView authView =
                new AuthenticationView(authService);

            LogView logView =
                new LogView(timerService);

            TimerView timerView =
                new TimerView(timerService, logView);

            while (true)
            {
                User? loggedInUser = authView.ShowLoginPage();

                if (loggedInUser == null)
                {
                    break;
                }

                timerView.ShowDashboard(loggedInUser);
            }

            Console.WriteLine("\nThank you for using Task Timer!");
        }
    }
}
