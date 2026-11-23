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

            AuthenticationServices authService =
                new AuthenticationServices(userRepository);

            AuthenticationView authView =
                new AuthenticationView(authService);

            TimerView timerView =
                new TimerView();

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
