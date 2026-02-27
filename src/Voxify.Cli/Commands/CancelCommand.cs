namespace Voxify.Cli.Commands;

/// <summary>
/// Command: Cancel current operation (cancel).
/// </summary>
public class CancelCommand
{
    public static async Task<int> ExecuteAsync(Ipc.IpcClient client)
    {
        Console.WriteLine("[CancelCommand] Sending cancel command...");

        var response = await client.SendCommandAsync("cancel");

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
