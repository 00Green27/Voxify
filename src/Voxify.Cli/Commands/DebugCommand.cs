namespace Voxify.Cli.Commands;

/// <summary>
/// Команда: Включение/выключение режима отладки (debug).
/// </summary>
public class DebugCommand
{
    public static async Task<int> ExecuteAsync(Ipc.IpcClient client)
    {
        Console.WriteLine("[DebugCommand] Переключение режима отладки...");

        var response = await client.SendCommandAsync("debug");

        if (response == null)
        {
            Console.Error.WriteLine("Ошибка: Не получен ответ от сервера.");
            return 1;
        }

        if (response.Success)
        {
            Console.WriteLine($"✓ {response.Message}");
            if (!string.IsNullOrEmpty(response.Data))
            {
                Console.WriteLine($"  Режим отладки: {response.Data}");
            }
            return 0;
        }
        else
        {
            Console.Error.WriteLine($"✗ Ошибка: {response.Message}");
            return 1;
        }
    }
}
