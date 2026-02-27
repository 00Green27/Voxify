namespace Voxify.Cli.Commands;

/// <summary>
/// Команда: Отмена текущей операции (cancel).
/// </summary>
public class CancelCommand
{
    public static async Task<int> ExecuteAsync(Ipc.IpcClient client)
    {
        Console.WriteLine("[CancelCommand] Отправка команды отмены...");

        var response = await client.SendCommandAsync("cancel");

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
