using System.Diagnostics;

namespace LombdaAgentMAUI;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		
		// Set up global exception handling
		AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
		TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
	
	private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		var exception = e.ExceptionObject as Exception;
		LogUnhandledException(exception, "AppDomain.CurrentDomain.UnhandledException");
	}

	private void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
		e.SetObserved(); // Prevent the application from crashing
	}

	private void LogUnhandledException(Exception? exception, string source)
	{
		try
		{
			string message = $"Unhandled exception from {source}:";
			Debug.WriteLine(message);
			Debug.WriteLine(exception);

			if (Debugger.IsAttached)
				Debugger.Break();

			// You could log to a file or service here
			// You could also show a user-friendly error dialog
			
			// Optional: Display an alert to the user
			// MainThread.BeginInvokeOnMainThread(async () => 
			// {
			//     await Current?.MainPage?.DisplayAlert("Error", "An unexpected error occurred. Please restart the application.", "OK");
			// });
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error in error handler: {ex}");
		}
	}
}