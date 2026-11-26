using Spectre.Console;
using Timer.Models;
using Timer.Service;

namespace Timer.View
{
    /// <summary>
    /// To display and manage import option
    /// </summary>
    public class ImportView
    {
        private readonly CsvServices _csvServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportView"/> class.
        /// </summary>
        /// <param name="csvServices">The import export service.</param>
        public ImportView(CsvServices csvServices)
        {
            this._csvServices = csvServices;
        }

        /// <summary>
        /// Imports task history records from a CSV file and displays the import results.
        /// </summary>
        /// <param name="user">The user under whose account the records will be imported.</param>
        public void ImportData(User user)
        {
            AnsiConsole.Clear();

            AnsiConsole.MarkupLine("[bold]IMPORT HISTORY DATA[/]");
            AnsiConsole.Write(new Rule());

            AnsiConsole.MarkupLine(
                $"Target user: {Markup.Escape(user.Name)} ({Markup.Escape(user.Email)})");

            string importPath = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter the CSV file path:")
                    .Validate(input =>
                        !string.IsNullOrWhiteSpace(input)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("File path cannot be empty.")));

            importPath = importPath.Trim('"', '\'');

            if (!File.Exists(importPath))
            {
                AnsiConsole.MarkupLine("[red]The specified file could not be found.[/]");
                this.WaitForUser();
                return;
            }

            if (!string.Equals(
                Path.GetExtension(importPath),
                ".csv",
                StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[red]Please select a CSV file.[/]");
                this.WaitForUser();
                return;
            }

            try
            {
                TaskImportResult result = this._csvServices.ImportTasksFromCsv(
                    user.UserId,
                    importPath);

                AnsiConsole.MarkupLine("\n[bold]IMPORT SUMMARY[/]");
                AnsiConsole.Write(new Rule());

                var table = new Table()
                    .Border(TableBorder.Simple)
                    .AddColumn("Result")
                    .AddColumn("Count");

                table.AddRow(
                    "[green]Valid / Imported[/]",
                    result.ValidCount.ToString());

                table.AddRow(
                    "[red]Invalid[/]",
                    result.InvalidCount.ToString());

                table.AddRow(
                    "[yellow]Duplicated / Skipped[/]",
                    result.DuplicateCount.ToString());

                AnsiConsole.Write(table);

                if (result.ValidCount > 0)
                {
                    AnsiConsole.MarkupLine(
                        "\n[green]Import completed successfully.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine(
                        "\n[yellow]No new valid records were imported.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine(
                    $"[red]Import failed: {Markup.Escape(ex.Message)}[/]");
            }

            this.WaitForUser();
        }

        /// <summary>
        /// Waits for the user to press a key before continuing.
        /// </summary>
        private void WaitForUser()
        {
            AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
            Console.ReadKey(true);
        }
    }
}
