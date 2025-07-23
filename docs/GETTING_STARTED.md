# Getting Started with LombdaAgent MAUI

This guide will help you get LombdaAgent MAUI up and running on your development machine and understand the basics of using the application.

## ?? Prerequisites

Before you begin, ensure you have the following installed on your development machine:

### Required Software

1. **.NET 9 SDK**
   - Download from [Microsoft .NET](https://dotnet.microsoft.com/download)
   - Verify installation: `dotnet --version`

2. **Visual Studio 2022** (Version 17.8 or later)
   - Install the "MAUI" workload during installation
   - Or add it later via Visual Studio Installer

3. **Platform-specific SDKs** (for target platforms):
   - **Windows**: Included with Visual Studio
   - **Android**: Android SDK (via Visual Studio)
   - **iOS/macOS**: Xcode (macOS only)

### API Dependencies

1. **LombdaAgent API Server**
   - Must be running and accessible
   - Default URL: `https://localhost:5001`

2. **OpenAI API Key**
   - Required for AI functionality
   - Configure in your API server environment

## ?? Quick Start

### 1. Clone and Build
# Clone the repository
git clone https://github.com/lombda/LombdaAgentMAUI.git
cd LombdaAgentMAUI

# Restore packages
dotnet restore

# Build the solution
dotnet build
### 2. Run the Application

#### Visual Studio
1. Open `LombdaAgentMAUI.sln` in Visual Studio 2022
2. Select your target platform (Windows, Android, etc.)
3. Press F5 or click "Start Debugging"

#### Command Line
# Windows
dotnet run --project LombdaAgentMAUI -f net9.0-windows10.0.19041.0

# Other platforms available:
# -f net9.0-ios (requires macOS)
# -f net9.0-maccatalyst (requires macOS)
# -f net9.0-android (requires Android SDK)
### 3. Initial Configuration

When you first launch the app:

1. **Navigate to Settings**
   - Click the "Settings" tab at the bottom

2. **Configure API Endpoint**
   - Enter your LombdaAgent API URL (e.g., `https://localhost:5001`)
   - Click "Test Connection" to verify

3. **Create Your First Agent**
   - Return to the "Chat" tab
   - Click "Create Agent"
   - Enter a name (e.g., "My Assistant")
   - Click "Create"

4. **Start Chatting**
   - Select your agent from the dropdown
   - Type a message and press Enter
   - Watch the streaming response appear!

## ??? Project Structure

Understanding the project structure will help you navigate and contribute:
LombdaAgentMAUI/
??? LombdaAgentMAUI/               # Main MAUI Application
?   ??? MainPage.xaml              # Primary chat interface
?   ??? SettingsPage.xaml          # Configuration page
?   ??? AppShell.xaml              # Navigation shell
?   ??? MauiProgram.cs             # Service registration
?   ??? Services/                  # Platform implementations
?   ?   ??? MauiAgentApiService.cs # MAUI-specific API service
?   ?   ??? MauiSecureStorageService.cs # Secure storage
?   ?   ??? ConfigurableAgentApiService.cs # Configurable API
?   ??? Converters/                # XAML value converters
?   ?   ??? ChatConverters.cs      # Chat-specific converters
?   ??? Controls/                  # Custom controls
?   ?   ??? MarkdownView.cs        # Markdown rendering
?   ??? Platforms/                 # Platform-specific code
?       ??? iOS/
?       ??? Android/
?       ??? Windows/
?       ??? MacCatalyst/
??? LombdaAgentMAUI.Core/          # Shared Business Logic
?   ??? Models/                    # Data models
?   ?   ??? ApiModels.cs           # API request/response models
?   ??? Services/                  # Core services
?       ??? AgentApiService.cs     # API communication
?       ??? ConfigurationService.cs # Settings management
?       ??? SessionManagerService.cs # Session handling
??? LombdaAgentMAUI.Tests/         # Test Suite
    ??? Unit/                      # Unit tests
    ??? Integration/               # Integration tests
    ??? Live/                      # Live API tests
## ?? Development Environment Setup

### Visual Studio Configuration

1. **Install Required Workloads**- .NET Multi-platform App UI development
   - Mobile development with .NET (for Android)
2. **Configure Android Emulator** (if targeting Android)
   - Open Android Device Manager
   - Create a new virtual device
   - Choose API level 21 or higher

3. **Configure iOS Simulator** (macOS only)
   - Requires Xcode installation
   - Simulator automatically available

### Platform-Specific Setup

#### Windows Development
- No additional setup required
- Can develop for Windows, Android
- Cannot develop for iOS/macOS

#### macOS Development
- Can develop for all platforms
- Requires Xcode for iOS development
- Requires Android SDK for Android development

## ?? Running Tests

The project includes comprehensive tests to ensure quality:

### Quick Test Run# Run all unit tests (fast)
dotnet test --filter "Category!=Network"
### Full Test Suite# Run all tests including integration tests
dotnet test
### Live API Tests# Requires running API server
# Set environment variable first:
export OPENAI_API_KEY="your-openai-api-key"

# Run network tests
dotnet test --filter "Category=Network"
## ?? First Development Task

Try making a simple change to get familiar with the codebase:

### Add a Welcome Message

1. **Open** `LombdaAgentMAUI/MainPage.xaml.cs`

2. **Find** the constructor or page load method

3. **Add** a welcome message to the chat:private void AddWelcomeMessage()
{
    Messages.Add(new ChatMessage 
    { 
        Text = "Welcome to LombdaAgent MAUI! Create an agent to get started.",
        IsUser = false,
        Timestamp = DateTime.Now
       });
   }
4. **Build and run** to see your change

## ?? Common Issues

### Build Issues

**"MAUI workload not found"**# Install MAUI workload
dotnet workload install maui
**"Android SDK not found"**
- Install Android SDK via Visual Studio Installer
- Or set `ANDROID_HOME` environment variable

### Runtime Issues

**"API connection failed"**
- Ensure LombdaAgent API server is running
- Check API URL in settings
- Verify network connectivity

**"No agents available"**
- Create an agent using the "Create Agent" button
- Check API server logs for errors

### Platform Issues

**iOS build fails**
- Requires macOS for development
- Ensure Xcode is installed and up to date

**Android deployment fails**
- Check Android emulator is running
- Verify USB debugging is enabled (for physical devices)

## ?? Next Steps

Now that you have the basics working:

1. **Explore the UI** - Try all the features in the app
2. **Read the Code** - Understand the MVVM pattern used
3. **Run Tests** - See how comprehensive testing works
4. **Make Changes** - Try customizing the UI or adding features
5. **Read API Documentation** - Understand the LombdaAgent API integration

## ?? Getting Help

If you encounter issues:

1. **Check the logs** - Enable detailed logging in the app
2. **Search issues** - Look through existing GitHub issues
3. **Ask questions** - Create a new issue or discussion
4. **Join the community** - Connect with other developers

## ?? Useful Resources

- [.NET MAUI Documentation](https://docs.microsoft.com/en-us/dotnet/maui/)
- [LombdaAiAgents Library](https://github.com/lombda/LombdaAiAgents)
- [OpenAI API Documentation](https://platform.openai.com/docs)
- [XAML Documentation](https://docs.microsoft.com/en-us/xaml/)

---

Happy coding! ??