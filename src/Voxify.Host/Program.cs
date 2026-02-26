using Voxify.UI;

namespace Voxify;

/// <summary>
/// Точка входа приложения Voxify.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
        // Запускаем главную форму
        Application.Run(new MainForm());
    }
}
