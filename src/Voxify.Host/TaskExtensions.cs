namespace Voxify;

/// <summary>
/// Extension methods for Task.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Waits for the task to complete within the specified timeout.
    /// </summary>
    public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
    {
        if (task == await Task.WhenAny(task, Task.Delay(timeout)))
        {
            await task;
        }
        else
        {
            throw new TimeoutException("The operation has timed out.");
        }
    }
}
