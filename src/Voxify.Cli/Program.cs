using Voxify.Cli.Commands;
using Voxify.Cli.Ipc;

namespace Voxify.Cli;

/// <summary>
/// Voxify CLI - утилита командной строки для управления Voxify.Host.
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

        // Парсим аргументы
        var command = args[0].TrimStart('-').ToLower();

        // Создаём IPC клиент
        using var client = new IpcClient();

        // Подключаемся к серверу
        if (!await client.ConnectAsync())
        {
            Console.Error.WriteLine("Ошибка: Не удалось подключиться к Voxify.Host.");
            Console.Error.WriteLine("Убедитесь, что Voxify запущен.");
            return 1;
        }

        // Выполняем команду
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
            Console.Error.WriteLine($"Неизвестная команда: {command}");
            PrintHelp();
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"Voxify CLI - утилита для управления Voxify

Использование:
  voxify <команда>

Команды:
  toggle, toggle-transcription   Переключить режим записи (старт/стоп)
  cancel                         Отменить текущую операцию
  status                         Показать текущий статус приложения
  debug                          Включить/выключить режим отладки
  help                           Показать эту справку

Примеры:
  voxify toggle                  Начать/остановить запись
  voxify status                  Узнать текущее состояние
  voxify cancel                  Отменить распознавание

Примечание:
  Для работы CLI необходимо, чтобы Voxify.Host был запущен.");
    }
}
