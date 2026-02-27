using Voxify.Cli.Commands;
using Voxify.Cli.Ipc;

namespace Voxify.Cli;

/// <summary>
/// Voxify CLI - command-line utility for managing Voxify.Host.
/// </summary>
public class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 1;
        }

        // Parse arguments
        var command = args[0].TrimStart('-').ToLower();

        // Create IPC client
        using var client = new IpcClient();

        // Connect to server
        if (!await client.ConnectAsync())
        {
            Console.Error.WriteLine("Error: Failed to connect to Voxify.Host.");
            Console.Error.WriteLine("Make sure Voxify is running.");
            return 1;
        }

        // Execute command
        if (command == "toggle" || command == "toggle-transcription")
        {
            return await ToggleCommand.ExecuteAsync(client);
        }
        else if (command == "cancel")
        {
            return await CancelCommand.ExecuteAsync(client);
        }
        else if (command == "status")
        {
            return await StatusCommand.ExecuteAsync(client);
        }
        else if (command == "debug")
        {
            return await DebugCommand.ExecuteAsync(client);
        }
        else if (command == "help" || command == "-h" || command == "--help")
        {
            PrintHelp();
            return 0;
        }
        else
        {
            Console.Error.WriteLine($"Unknown command: {command}");
            PrintHelp();
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"Voxify CLI - utility for managing Voxify

Usage:
  voxify <command>

Commands:
  toggle, toggle-transcription   Toggle recording mode (start/stop)
  cancel                         Cancel current operation
  status                         Show current application status
  debug                          Enable/disable debug mode
  help                           Show this help message

Examples:
  voxify toggle                  Start/stop recording
  voxify status                  Check current state
  voxify cancel                  Cancel recognition

Note:
  CLI requires Voxify.Host to be running.");
    }
}
