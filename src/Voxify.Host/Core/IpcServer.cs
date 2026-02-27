using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace Voxify.Core;

/// <summary>
/// IPC server for receiving commands from CLI client via Named Pipes.
/// </summary>
public class IpcServer : IDisposable
{
    private const string PipeName = "VoxifyIpcPipe";
    private readonly CancellationTokenSource _cts = new();
    private Task? _serverTask;
    private bool _disposed;

    /// <summary>
    /// Event fired when a command is received (synchronous handling).
    /// </summary>
    public event EventHandler<IpcCommandReceivedEventArgs>? CommandReceived;

    /// <summary>
    /// Event fired when a command is received (asynchronous handling with response).
    /// </summary>
    public event Func<IpcCommand, Task<IpcResponse>>? CommandReceivedAsync;

    /// <summary>
    /// Starts the IPC server.
    /// </summary>
    public void Start()
    {
        _serverTask = RunServerAsync(_cts.Token);
    }

    /// <summary>
    /// Stops the IPC server.
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

                // Wait for client connection
                await pipeServer.WaitForConnectionAsync(cancellationToken);

                // Read command
                var buffer = new byte[1024];
                var length = await pipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (length > 0)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, length);
                    var command = JsonSerializer.Deserialize<IpcCommand>(json);

                    if (command != null)
                    {
                        IpcResponse response;

                        // Try asynchronous handling
                        if (CommandReceivedAsync != null)
                        {
                            response = await CommandReceivedAsync.Invoke(command);
                        }
                        else
                        {
                            // Synchronous handling (for backward compatibility)
                            CommandReceived?.Invoke(this, new IpcCommandReceivedEventArgs(command));
                            response = new IpcResponse { Success = true, Message = $"Command '{command.Type}' processed" };
                        }

                        // Send response
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
                // Log error but continue running
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
/// IPC command from CLI client.
/// </summary>
public class IpcCommand
{
    /// <summary>
    /// Command type: toggle, cancel, status, debug.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Additional command parameters.
    /// </summary>
    public Dictionary<string, string>? Parameters { get; set; }
}

/// <summary>
/// IPC response from server.
/// </summary>
public class IpcResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; }
}

/// <summary>
/// Command received event arguments.
/// </summary>
public class IpcCommandReceivedEventArgs : EventArgs
{
    public IpcCommand Command { get; }

    public IpcCommandReceivedEventArgs(IpcCommand command)
    {
        Command = command;
    }
}
