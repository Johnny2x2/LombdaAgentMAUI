using Microsoft.UI.Xaml;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LombdaAgentMAUI.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        // Add unhandled exception handler for Windows UI
        this.UnhandledException += OnUnhandledException;
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    /// <summary>
    /// Handles unhandled exceptions in the Windows UI thread
    /// </summary>
    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true; // Prevent the app from crashing

        try
        {
            string message = $"WinUI UnhandledException: {e.Message}";
            Debug.WriteLine(message);
            Debug.WriteLine(e.Exception);

            if (Debugger.IsAttached)
                Debugger.Break();

            // You could log to a file or service here
            // You could also show a user-friendly error dialog
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in Windows error handler: {ex}");
        }
    }
}

