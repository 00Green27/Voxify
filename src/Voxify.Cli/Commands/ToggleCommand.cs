namespace Voxify.Cli.Commands;

/// <summary>
/// Command: Toggle recording mode (toggle transcription).
/// </summary>
public class ToggleCommand
{
    public static async Task<int> ExecuteAsync(Ipc.IpcClient client)
    {
        Console.WriteLine("[ToggleCommand] Sending toggle recording command...");

        var response = await client.SendCommandAsync("toggle");

        if (response == null)
        {
            Console.Error.WriteLine("Error: No response from server.");
            return 1;
        }

        if (response.Success)
        {
            Console.WriteLine($"✓ {response.Message}");
            return 0;
        }
        else
        {
            Console.Error.WriteLine($"✗ Error: {response.Message}");
            return 1;
        }
    }
}
