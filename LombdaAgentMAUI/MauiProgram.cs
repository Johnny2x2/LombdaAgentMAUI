using Microsoft.Extensions.Logging;
using LombdaAgentMAUI.Core.Services;
using LombdaAgentMAUI.Services;
using System.Diagnostics;

namespace LombdaAgentMAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Debug.WriteLine("Creating MAUI App...");
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register the secure storage service first
        builder.Services.AddSingleton<ISecureStorageService, MauiSecureStorageService>();
        
        // Register core services that depend on secure storage
        builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
        builder.Services.AddSingleton<ISessionManagerService, SessionManagerService>();
        
        // Register file services
        Debug.WriteLine("Registering FilePickerService...");
        builder.Services.AddSingleton<IFilePickerService, FilePickerService>();
        
        // Register API services
        builder.Services.AddSingleton<IAgentApiService, MauiAgentApiService>();
        
        // Register pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<FileUploadDialog>();
        builder.Services.AddTransient<FilePickerTestPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        Debug.WriteLine("MAUI App builder configuration complete.");
        return builder.Build();
    }
}
