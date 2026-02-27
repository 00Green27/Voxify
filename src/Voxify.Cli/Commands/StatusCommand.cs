namespace Voxify.Cli.Commands;

/// <summary>
/// Команда: Получение статуса приложения (status).
/// </summary>
public class StatusCommand
{
    public static async Task<int> ExecuteAsync(Ipc.IpcClient client)
    {
        Console.WriteLine("[StatusCommand] Запрос статуса...");

        var response = await client.SendCommandAsync("status");

        if (response == null)
        {
            Console.Error.WriteLine("Ошибка: Не получен ответ от сервера.");
            return 1;
        }

        if (response.Success)
        {
            var status = response.Data ?? "Unknown";
            Console.WriteLine($"Статус Voxify: {status}");
            return 0;
        }
        else
        {
            Console.Error.WriteLine($"✗ Ошибка: {response.Message}");
            return 1;
        }
    }
}
