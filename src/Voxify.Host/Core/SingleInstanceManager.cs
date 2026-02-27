using System.Runtime.InteropServices;

namespace Voxify.Core;

/// <summary>
/// Manages single instance application behavior.
/// Ensures only one instance of the application runs at a time.
/// </summary>
public class SingleInstanceManager : IDisposable
{
    private const string MutexName = "VoxifySingleInstanceMutex";
    private const string WindowTitle = "Voxify";
    private const int SW_SHOW = 1;
    private const int SW_RESTORE = 9;

    private Mutex? _mutex;
    private bool _isFirstInstance;
    private bool _disposed;

    /// <summary>
    /// Gets whether this is the first (primary) instance of the application.
    /// </summary>
    public bool IsFirstInstance => _isFirstInstance;

    /// <summary>
    /// Event fired when a second instance tries to start.
    /// </summary>
    public event EventHandler<SecondInstanceEventArgs>? SecondInstanceStarted;

    public SingleInstanceManager()
    {
        _mutex = new Mutex(true, MutexName, out _isFirstInstance);

        if (!_isFirstInstance)
        {
            // This is a second instance - signal the first one
            SignalFirstInstance();
            _mutex?.Dispose();
            _mutex = null;
        }
    }

    /// <summary>
    /// Brings the first instance window to foreground.
    /// </summary>
    public void BringFirstInstanceToFront()
    {
        var processes = System.Diagnostics.Process.GetProcessesByName(
            System.Diagnostics.Process.GetCurrentProcess().ProcessName);

        foreach (var process in processes)
        {
            if (process.Id != System.Diagnostics.Process.GetCurrentProcess().Id)
            {
                // Try to find the main window
                IntPtr hWnd = process.MainWindowHandle;
                if (hWnd == IntPtr.Zero)
                {
                    // Try to find window by title
                    hWnd = FindWindow(null!, WindowTitle);
                }

                if (hWnd != IntPtr.Zero)
                {
                    ShowWindowAsync(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd);
                }
                break;
            }
        }
    }

    /// <summary>
    /// Sends arguments to the first instance via WM_COPYDATA.
    /// </summary>
    private void SignalFirstInstance()
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length <= 1)
            return;

        // Find first instance window
        var processes = System.Diagnostics.Process.GetProcessesByName(
            System.Diagnostics.Process.GetCurrentProcess().ProcessName);

        foreach (var process in processes)
        {
            if (process.Id != System.Diagnostics.Process.GetCurrentProcess().Id)
            {
                IntPtr hWnd = process.MainWindowHandle;
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = FindWindow(null!, WindowTitle);
                }

                if (hWnd != IntPtr.Zero)
                {
                    // Send command line arguments
                    string commandLine = string.Join(" ", args.Skip(1));
                    SendStringToWindow(hWnd, commandLine);
                }
                break;
            }
        }
    }

    /// <summary>
    /// Sends a string to another window using WM_COPYDATA.
    /// </summary>
    private static void SendStringToWindow(IntPtr hWnd, string data)
    {
        COPYDATASTRUCT cds = new COPYDATASTRUCT
        {
            dwData = IntPtr.Zero,
            lpData = Marshal.StringToHGlobalAnsi(data),
            cbData = data.Length + 1
        };

        try
        {
            SendMessage(hWnd, WM_COPYDATA, IntPtr.Zero, ref cds);
        }
        finally
        {
            Marshal.FreeHGlobal(cds.lpData);
        }
    }

    /// <summary>
    /// Handles incoming command line arguments from second instance.
    /// </summary>
    public void HandleSecondInstanceArgs(string args)
    {
        SecondInstanceStarted?.Invoke(this, new SecondInstanceEventArgs { Arguments = args });
    }

    // Windows API imports
    private const int WM_COPYDATA = 0x004A;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Event arguments for second instance detection.
/// </summary>
public class SecondInstanceEventArgs : EventArgs
{
    /// <summary>
    /// Command line arguments from the second instance.
    /// </summary>
    public string Arguments { get; set; } = string.Empty;
}
