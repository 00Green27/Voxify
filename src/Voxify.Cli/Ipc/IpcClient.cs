using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace Voxify.Cli.Ipc;

/// <summary>
/// IPC клиент для отправки команд в Voxify.Host через Named Pipes.
/// </summary>
public class IpcClient : IDisposable
{
    private const string PipeName = "VoxifyIpcPipe";
    private NamedPipeClientStream? _pipe;
    private bool _disposed;

    /// <summary>
    /// Подключается к IPC серверу.
    /// </summary>
    public async Task<bool> ConnectAsync(int timeoutMs = 2000)
    {
        try
        {
            _pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await _pipe.ConnectAsync(timeoutMs);
            return _pipe.IsConnected;
        }
        catch (TimeoutException)
        {
            Console.Error.WriteLine("[IpcClient] Timeout: Voxify.Host не запущен или IPC сервер недоступен.");
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[IpcClient] Error connecting: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Отправляет команду и получает ответ.
    /// </summary>
    public async Task<IpcResponse?> SendCommandAsync(string commandType, Dictionary<string, string>? parameters = null)
    {
        if (_pipe == null || !_pipe.IsConnected)
        {
            Console.Error.WriteLine("[IpcClient] Not connected to IPC server.");
            return null;
        }

        try
        {
            // Создаём команду
            var command = new IpcCommand
            {
                Type = commandType,
                Parameters = parameters
            };

            // Сериализуем в JSON
            var json = JsonSerializer.Serialize(command);
            var bytes = Encoding.UTF8.GetBytes(json);

            // Отправляем
            await _pipe.WriteAsync(bytes, 0, bytes.Length);
            await _pipe.FlushAsync();

            // Читаем ответ
            var buffer = new byte[1024];
            var length = await _pipe.ReadAsync(buffer, 0, buffer.Length);

            if (length > 0)
            {
                var responseJson = Encoding.UTF8.GetString(buffer, 0, length);
                return JsonSerializer.Deserialize<IpcResponse>(responseJson);
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[IpcClient] Error sending command: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _pipe?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// IPC команда.
/// </summary>
public class IpcCommand
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, string>? Parameters { get; set; }
}

/// <summary>
/// IPC ответ.
/// </summary>
public class IpcResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; }
}
