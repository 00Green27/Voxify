using Voxify.Config;
using Voxify.Core;
using Voxify.UI;

namespace Voxify;

/// <summary>
/// Точка входа приложения Voxify.
/// </summary>
public static class Program
{
    private static SingleInstanceManager? _singleInstanceManager;
    private static MainForm? _mainForm;

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Load configuration
        var configManager = new ConfigurationManager();
        var config = configManager.Settings;

        // Check single instance mode
        if (config.SystemIntegration.SingleInstance)
        {
            _singleInstanceManager = new SingleInstanceManager();

            if (!_singleInstanceManager.IsFirstInstance)
            {
                // This is a second instance - exit after signaling first instance
                return;
            }
        }

        // Create and configure main form
        _mainForm = new MainForm();

        // Set window title for single instance detection
        if (_singleInstanceManager != null)
        {
            _mainForm.Text = "Voxify";
        }

        // Handle second instance arguments
        if (_singleInstanceManager != null)
        {
            _singleInstanceManager.SecondInstanceStarted += OnSecondInstanceStarted;
        }

        // Run the application
        Application.Run(_mainForm);

        // Cleanup
        _singleInstanceManager?.Dispose();
    }

    /// <summary>
    /// Handles arguments from a second instance trying to start.
    /// </summary>
    private static void OnSecondInstanceStarted(object? sender, SecondInstanceEventArgs e)
    {
        if (_mainForm == null)
            return;

        // Bring main form to front
        _mainForm.Invoke(new Action(() =>
        {
            _mainForm.BringToFront();
            if (_mainForm.WindowState == FormWindowState.Minimized)
            {
                _mainForm.WindowState = FormWindowState.Normal;
            }
        }));

        // Process arguments (e.g., CLI commands)
        if (!string.IsNullOrEmpty(e.Arguments))
        {
            _mainForm.Invoke(new Action(() =>
            {
                _mainForm.HandleExternalArguments(e.Arguments);
            }));
        }
    }
}
