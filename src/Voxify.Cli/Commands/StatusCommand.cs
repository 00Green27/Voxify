namespace Voxify.Cli.Commands;

/// <summary>
/// Command: Get application status (status).
/// </summary>
public class StatusCommand
{
    public static async Task<int> ExecuteAsync(Ipc.IpcClient client)
    {
        Console.WriteLine("[StatusCommand] Requesting status...");

        var response = await client.SendCommandAsync("status");

        if (response == null)
        {
            Console.Error.WriteLine("Error: No response from server.");
            return 1;
        }

        if (response.Success)
        {
            var status = response.Data ?? "Unknown";
            Console.WriteLine($"Voxify status: {status}");
            return 0;
        }
        else
        {
            Console.Error.WriteLine($"✗ Error: {response.Message}");
            return 1;
        }
    }
}
