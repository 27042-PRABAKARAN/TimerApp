using System.Text;
using Timer.Models;
using Timer.Services;

namespace Timer.Views
{
    /// <summary>
    /// Provides the console-based user interface for user login and registration.
    /// </summary>
    public class AuthenticationView
    {
        private readonly AuthenticationServices _authService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationView"/> class.
        /// </summary>
        /// <param name="authService">The service used to handle user authentication and registration.</param>
        public AuthenticationView(AuthenticationServices authService)
        {
            this._authService = authService;
        }

        /// <summary>
        /// Displays the main authentication menu and processes user choices for logging in, signing up, or exiting.
        /// </summary>
        /// <returns>The authenticated <see cref="User"/> if login succeeds; otherwise, <see langword="null"/> if the user chooses to exit.</returns>
        public User? ShowLoginPage()
        {
            while (true)
            {
                Console.Clear();

                Console.WriteLine("========== TASK TIMER ==========");
                Console.WriteLine("           LOGIN PAGE");
                Console.WriteLine();
                Console.WriteLine("1. Login");
                Console.WriteLine("2. Sign Up");
                Console.WriteLine("3. Exit");
                Console.WriteLine();

                Console.Write("Enter choice: ");
                string choice = Console.ReadLine() ?? string.Empty;

                switch (choice)
                {
                    case "1":
                        User? user = this.Login();
                        if (user != null)
                        {
                            return user;
                        }

                        break;

                    case "2":
                        this.SignUp();
                        break;

                    case "3":
                        return null;

                    default:
                        Console.WriteLine("Invalid choice.");
                        Console.ReadKey();
                        break;
                }
            }
        }

        /// <summary>
        /// Prompts the user for email and password credentials and attempts to authenticate them.
        /// </summary>
        /// <returns>The authenticated <see cref="User"/> if valid; otherwise, <see langword="null"/>.</returns>
        private User? Login()
        {
            Console.Clear();

            Console.WriteLine("========== LOGIN ==========");

            Console.Write("Email: ");
            string email = Console.ReadLine() ?? string.Empty;

            Console.Write("Password: ");
            string password = this.ReadMaskedPassword();

            User? user = this._authService.Login(email, password);

            if (user == null)
            {
                Console.WriteLine("\nInvalid email or password.");
                Console.ReadKey();
                return null;
            }

            Console.WriteLine("\nLogin successful!");
            Console.ReadKey();

            return user;
        }

        /// <summary>
        /// Prompts the user for registration details and attempts to create a new account.
        /// </summary>
        private void SignUp()
        {
            Console.Clear();

            Console.WriteLine("========== SIGN UP ==========");
            Console.WriteLine("Password rules: 8+ chars, uppercase, lowercase, number, and symbol (no spaces).");
            Console.WriteLine();

            Console.Write("Name: ");
            string name = Console.ReadLine() ?? string.Empty;

            Console.Write("Email: ");
            string email = Console.ReadLine() ?? string.Empty;

            Console.Write("Password: ");
            string password = Console.ReadLine() ?? string.Empty;

            bool success = this._authService.SignUp(
                name,
                email,
                password,
                out string message);

            Console.WriteLine("\n" + message);

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Reads password input from the console while displaying asterisks (<c>*</c>) instead of plain-text characters.
        /// </summary>
        /// <returns>The plain-text password entered by the user.</returns>
        private string ReadMaskedPassword()
        {
            StringBuilder password = new StringBuilder();

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    password.Append(keyInfo.KeyChar);
                    Console.Write("*");
                }
            }

            return password.ToString();
        }
    }
}
