using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace Voxify.Core;

/// <summary>
/// IPC сервер для приёма команд от CLI клиента через Named Pipes.
/// </summary>
public class IpcServer : IDisposable
{
    private const string PipeName = "VoxifyIpcPipe";
    private readonly CancellationTokenSource _cts = new();
    private Task? _serverTask;
    private bool _disposed;

    /// <summary>
    /// Событие при получении команды (синхронная обработка).
    /// </summary>
    public event EventHandler<IpcCommandReceivedEventArgs>? CommandReceived;

    /// <summary>
    /// Событие при получении команды (асинхронная обработка с ответом).
    /// </summary>
    public event Func<IpcCommand, Task<IpcResponse>>? CommandReceivedAsync;

    /// <summary>
    /// Запускает IPC сервер.
    /// </summary>
    public void Start()
    {
        _serverTask = RunServerAsync(_cts.Token);
    }

    /// <summary>
    /// Останавливает IPC сервер.
    /// </summary>
    public void Stop()
    {
        _cts.Cancel();
        _serverTask?.Wait(1000);
    }

    private async Task RunServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

                // Ждём подключения клиента
                await pipeServer.WaitForConnectionAsync(cancellationToken);

                // Читаем команду
                var buffer = new byte[1024];
                var length = await pipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (length > 0)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, length);
                    var command = JsonSerializer.Deserialize<IpcCommand>(json);

                    if (command != null)
                    {
                        IpcResponse response;

                        // Пробуем асинхронную обработку
                        if (CommandReceivedAsync != null)
                        {
                            response = await CommandReceivedAsync.Invoke(command);
                        }
                        else
                        {
                            // Синхронная обработка (для обратной совместимости)
                            CommandReceived?.Invoke(this, new IpcCommandReceivedEventArgs(command));
                            response = new IpcResponse { Success = true, Message = $"Command '{command.Type}' processed" };
                        }

                        // Отправляем ответ
                        var responseJson = JsonSerializer.Serialize(response);
                        var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                        await pipeServer.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
                    }
                }

                pipeServer.Disconnect();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Логгируем ошибку, но продолжаем работу
                Console.WriteLine($"[IpcServer] Error: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _cts.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// IPC команда от CLI клиента.
/// </summary>
public class IpcCommand
{
    /// <summary>
    /// Тип команды: toggle, cancel, status, debug.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Дополнительные параметры команды.
    /// </summary>
    public Dictionary<string, string>? Parameters { get; set; }
}

/// <summary>
/// IPC ответ от сервера.
/// </summary>
public class IpcResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; }
}

/// <summary>
/// Аргументы события получения команды.
/// </summary>
public class IpcCommandReceivedEventArgs : EventArgs
{
    public IpcCommand Command { get; }

    public IpcCommandReceivedEventArgs(IpcCommand command)
    {
        Command = command;
    }
}
