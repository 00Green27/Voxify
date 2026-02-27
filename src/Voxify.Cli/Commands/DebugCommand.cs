namespace Voxify.Cli.Commands;

/// <summary>
/// Command: Toggle debug mode (debug).
/// </summary>
public class DebugCommand
{
    public static async Task<int> ExecuteAsync(Ipc.IpcClient client)
    {
        Console.WriteLine("[DebugCommand] Toggling debug mode...");

        var response = await client.SendCommandAsync("debug");

        if (response == null)
        {
            Console.Error.WriteLine("Error: No response from server.");
            return 1;
        }

        if (response.Success)
        {
            Console.WriteLine($"✓ {response.Message}");
            if (!string.IsNullOrEmpty(response.Data))
            {
                Console.WriteLine($"  Debug mode: {response.Data}");
            }
            return 0;
        }
        else
        {
            Console.Error.WriteLine($"✗ Error: {response.Message}");
            return 1;
        }
    }
}
