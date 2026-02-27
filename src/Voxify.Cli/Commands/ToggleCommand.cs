namespace Voxify.Cli.Commands;

/// <summary>
/// Команда: Переключение режима записи (toggle transcription).
/// </summary>
public class ToggleCommand
{
    public static async Task<int> ExecuteAsync(Ipc.IpcClient client)
    {
        Console.WriteLine("[ToggleCommand] Отправка команды переключения записи...");

        var response = await client.SendCommandAsync("toggle");

        if (response == null)
        {
            Console.Error.WriteLine("Ошибка: Не получен ответ от сервера.");
            return 1;
        }

        if (response.Success)
        {
            Console.WriteLine($"✓ {response.Message}");
            return 0;
        }
        else
        {
            Console.Error.WriteLine($"✗ Ошибка: {response.Message}");
            return 1;
        }
    }
}
