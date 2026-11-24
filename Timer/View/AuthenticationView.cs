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
        /// <param name="authService">The service used to han00
        /// dle user authentication and registration.</param>
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

        private User? Login()
        {
            Console.Clear();

            Console.WriteLine("========== LOGIN ==========");

            Console.Write("Email: ");
            string email = Console.ReadLine() ?? string.Empty;

            Console.Write("Password (Press Tab to show/hide): ");
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
            Console.WriteLine("Tip: Press [Tab] while typing a password to show or hide it.");
            Console.WriteLine();

            Console.Write("Name: ");
            string name = Console.ReadLine() ?? string.Empty;

            Console.Write("Email: ");
            string email = Console.ReadLine() ?? string.Empty;

            Console.Write("Password: ");
            string password = this.ReadMaskedPassword();

            Console.Write("Confirm Password: ");
            string confirmPassword = this.ReadMaskedPassword();

            // Check if passwords match before moving forward
            if (password != confirmPassword)
            {
                Console.WriteLine("\nPasswords do not match. Please try again.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\nDaily Work Start Time:");
            if (!this.TryReadStartTime(out TimeOnly dayStartTime))
            {
                Console.WriteLine("\nInvalid time entered. Please use Hour (1-12), Minute (0-59), and AM/PM.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            bool success = this._authService.SignUp(
                name,
                email,
                password,
                dayStartTime,
                out string message);

            Console.WriteLine("\n" + message);

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Prompts the user for 12-hour clock components (hour, minute, and AM/PM) and converts them into a <see cref="TimeOnly"/> value.
        /// </summary>
        /// <param name="startTime">When this method returns, contains the parsed <see cref="TimeOnly"/> value if valid.</param>
        /// <returns><see langword="true"/> if all inputs are valid; otherwise, <see langword="false"/>.</returns>
        private bool TryReadStartTime(out TimeOnly startTime)
        {
            startTime = default;

            Console.Write("  Hour (1-12)   : ");
            if (!int.TryParse(Console.ReadLine(), out int hour) || hour < 1 || hour > 12)
            {
                return false;
            }

            Console.Write("  Minute (0-59) : ");
            string minuteInput = Console.ReadLine() ?? string.Empty;
            int minute = 0;
            if (!string.IsNullOrWhiteSpace(minuteInput) &&
                (!int.TryParse(minuteInput, out minute) || minute < 0 || minute > 59))
            {
                return false;
            }

            Console.Write("  AM or PM      : ");
            string period = (Console.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();

            if (period != "AM" && period != "PM")
            {
                return false;
            }

            // Convert 12-hour clock to 24-hour clock
            int hour24 = hour % 12; // 12 AM -> 0, 12 PM -> 0 (plus 12 below)
            if (period == "PM")
            {
                hour24 += 12;
            }

            startTime = new TimeOnly(hour24, minute);
            return true;
        }

        /// <summary>
        /// Reads password input from the console, allowing the user to toggle visibility using the Tab key.
        /// </summary>
        /// <returns>The plain-text password entered by the user.</returns>
        private string ReadMaskedPassword()
        {
            StringBuilder password = new StringBuilder();
            bool isMasked = true;

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (keyInfo.Key == ConsoleKey.Tab)
                {
                    // Flip the visibility state
                    isMasked = !isMasked;

                    // Erase the currently displayed characters on the console
                    for (int i = 0; i < password.Length; i++)
                    {
                        Console.Write("\b \b");
                    }

                    // Reprint the password in the new state
                    Console.Write(isMasked ? new string('*', password.Length) : password.ToString());
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
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
                    Console.Write(isMasked ? '*' : keyInfo.KeyChar);
                }
            }

            return password.ToString();
        }
    }
}
