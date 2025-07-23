# System Architecture

This document provides a comprehensive overview of the LombdaAgent MAUI application architecture, including design patterns, data flow, and architectural decisions.

## ??? High-Level Architecture

LombdaAgent MAUI follows a clean architecture pattern with clear separation of concerns across multiple layers:

```
???????????????????????????????????????????????????????????????
?                    Presentation Layer                       ?
?  ???????????????????  ???????????????????                 ?
?  ?   MainPage      ?  ?  SettingsPage   ?                 ?
?  ?   (Chat UI)     ?  ?  (Config UI)    ?                 ?
?  ???????????????????  ???????????????????                 ?
?           ?                     ?                          ?
?           ???????????????????????                          ?
?????????????????????????????????????????????????????????????
?              Platform Services Layer                       ?
?  ???????????????????????????????????????????????????????  ?
?  ?  MauiAgentApiService  ?  MauiSecureStorageService  ?  ?
?  ?  ConfigurableApiSvc   ?  (Platform-Specific)       ?  ?
?  ???????????????????????????????????????????????????????  ?
?????????????????????????????????????????????????????????????
?                Core Business Logic Layer                   ?
?  ????????????????  ???????????????????  ????????????????? ?
?  ? AgentApiSvc  ?  ? ConfigSvc       ?  ? SessionMgr    ? ?
?  ? (HTTP/SSE)   ?  ? (Settings)      ?  ? (Threads)     ? ?
?  ????????????????  ???????????????????  ????????????????? ?
?????????????????????????????????????????????????????????????
?                   Data Layer                               ?
?  ????????????????  ???????????????????  ????????????????? ?
?  ?   Models     ?  ? Secure Storage  ?  ?  HTTP Client  ? ?
?  ? (DTOs/Msgs)  ?  ?  (Encrypted)    ?  ?  (Networking) ? ?
?  ????????????????  ???????????????????  ????????????????? ?
?????????????????????????????????????????????????????????????
                      ?
?????????????????????????????????????????????????????????????
?              External Dependencies                         ?
?  ????????????????  ???????????????????  ????????????????? ?
?  ? LombdaAgent  ?  ?    OpenAI       ?  ?   Platform    ? ?
?  ?     API      ?  ?     API         ?  ?    APIs       ? ?
?  ????????????????  ???????????????????  ????????????????? ?
???????????????????????????????????????????????????????????????
```

## ?? Core Design Patterns

### 1. Model-View-ViewModel (MVVM)

The application uses MVVM pattern for clean separation of UI and business logic:

```csharp
// View (XAML)
<ContentPage x:Class="LombdaAgentMAUI.MainPage">
    <StackLayout>
        <Picker ItemsSource="{Binding Agents}" 
                SelectedItem="{Binding SelectedAgent}"/>
        <CollectionView ItemsSource="{Binding Messages}"/>
        <Entry Text="{Binding MessageText}" 
               Command="{Binding SendMessageCommand}"/>
    </StackLayout>
</ContentPage>

// ViewModel (Implicit - using direct binding)
public partial class MainPage : ContentPage
{
    public ObservableCollection<ChatMessage> Messages { get; }
    public ObservableCollection<string> Agents { get; }
    public string MessageText { get; set; }
    public string SelectedAgent { get; set; }
    
    // Commands and business logic
}
```

### 2. Dependency Injection

Services are registered and injected throughout the application:

```csharp
// Service Registration (MauiProgram.cs)
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        // Platform Services
        builder.Services.AddSingleton<ISecureStorageService, MauiSecureStorageService>();
        
        // Core Services
        builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
        builder.Services.AddSingleton<IAgentApiService, MauiAgentApiService>();
        builder.Services.AddSingleton<ISessionManagerService, SessionManagerService>();
        
        // Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<SettingsPage>();
        
        return builder.Build();
    }
}
```

### 3. Repository Pattern

API services abstract data access and provide a consistent interface:

```csharp
public interface IAgentApiService
{
    Task<List<string>> GetAgentsAsync();
    Task<AgentResponse?> CreateAgentAsync(string name, string agentType = "Default");
    Task<MessageResponse?> SendMessageAsync(string agentId, string message, string? threadId = null);
    Task<string?> SendMessageStreamWithEventsAsync(/* parameters */);
}
```

### 4. Strategy Pattern

Different implementations for different platforms:

```csharp
// Platform-Agnostic Interface
public interface ISecureStorageService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task RemoveAsync(string key);
}

// MAUI Implementation
public class MauiSecureStorageService : ISecureStorageService
{
    public async Task<string?> GetAsync(string key) 
        => await SecureStorage.GetAsync(key);
}

// Test Implementation
public class MockSecureStorageService : ISecureStorageService
{
    private Dictionary<string, string> _storage = new();
    public Task<string?> GetAsync(string key) 
        => Task.FromResult(_storage.TryGetValue(key, out var value) ? value : null);
}
```

## ?? Data Flow Architecture

### 1. User Interaction Flow

```
User Input ? UI Event ? Command/Handler ? Service Call ? API Request ? Response Processing ? UI Update
```

**Example: Sending a Message**

```csharp
private async void OnSendMessageClicked(object sender, EventArgs e)
{
    // 1. UI Event
    var messageText = MessageEntry.Text;
    
    // 2. Service Call
    if (_isStreamingEnabled)
    {
        await _agentApiService.SendMessageStreamWithEventsAsync(
            _selectedAgentId,
            messageText,
            _currentThreadId,
            onMessageReceived: OnStreamingTextReceived,
            onEventReceived: OnStreamingEventReceived
        );
    }
    else
    {
        var response = await _agentApiService.SendMessageAsync(
            _selectedAgentId, 
            messageText, 
            _currentThreadId
        );
        
        // 3. UI Update
        if (response != null)
        {
            Messages.Add(new ChatMessage 
            { 
                Text = response.Text, 
                IsUser = false 
            });
        }
    }
}
```

### 2. Configuration Flow

```
Settings UI ? Configuration Service ? Secure Storage ? Service Notification ? Service Recreation
```

```csharp
// Settings Page
private async void OnSaveSettingsClicked(object sender, EventArgs e)
{
    await _configurationService.SetApiUrlAsync(ApiUrlEntry.Text);
    // Service will automatically recreate with new URL
}

// Configuration Service
public async Task SetApiUrlAsync(string url)
{
    await _secureStorage.SetAsync("ApiUrl", url);
    ApiUrlChanged?.Invoke(url); // Notify subscribers
}

// MauiAgentApiService (responds to URL changes)
private async Task<ConfigurableAgentApiService> GetOrCreateServiceAsync()
{
    var currentUrl = await _configService.GetApiUrlAsync();
    
    if (_currentService == null || _currentService.BaseUrl != currentUrl)
    {
        _currentService?.Dispose();
        var httpClient = _httpClientFactory.CreateClient();
        _currentService = new ConfigurableAgentApiService(httpClient, currentUrl);
    }
    
    return _currentService;
}
```

### 3. Streaming Data Flow

```
User Message ? HTTP POST ? Server-Sent Events ? Event Parser ? UI Updates (Real-time)
```

```csharp
// Streaming Response Processing
private async Task ProcessStreamingResponse(Stream stream)
{
    using var reader = new StreamReader(stream);
    
    while (!reader.EndOfStream)
    {
        var line = await reader.ReadLineAsync();
        
        if (line.StartsWith("event: "))
        {
            currentEvent = line.Substring(7).Trim();
        }
        else if (line.StartsWith("data: "))
        {
            var data = line.Substring(6);
            
            switch (currentEvent)
            {
                case "delta":
                    // Real-time text chunk
                    await onMessageReceived(ParseDeltaText(data));
                    break;
                    
                case "complete":
                    // Final response
                    var response = JsonSerializer.Deserialize<MessageResponse>(data);
                    return response.ThreadId;
            }
        }
    }
}
```

## ??? Service Architecture

### Core Services Layer

#### 1. AgentApiService
**Responsibility**: HTTP communication with LombdaAgent API

```csharp
public class AgentApiService : IAgentApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // Standard HTTP methods
    public async Task<List<string>> GetAgentsAsync() { /* ... */ }
    public async Task<AgentResponse?> CreateAgentAsync(string name) { /* ... */ }
    
    // Streaming methods with enhanced event handling
    public async Task<string?> SendMessageStreamWithEventsAsync(/* ... */) { /* ... */ }
}
```

**Key Features**:
- HTTP request/response handling
- Server-Sent Events (SSE) processing
- JSON serialization/deserialization
- Error handling and retries
- Connection management

#### 2. ConfigurationService
**Responsibility**: Application settings and preferences

```csharp
public class ConfigurationService : IConfigurationService
{
    private readonly ISecureStorageService _secureStorage;
    
    public async Task<string> GetApiUrlAsync()
    {
        return await _secureStorage.GetAsync("ApiUrl") ?? DefaultApiUrl;
    }
    
    public async Task SetApiUrlAsync(string url)
    {
        await _secureStorage.SetAsync("ApiUrl", url);
        ApiUrlChanged?.Invoke(url);
    }
    
    public event Action<string>? ApiUrlChanged;
}
```

**Key Features**:
- Secure storage integration
- Default value management
- Change notifications
- Validation and sanitization

#### 3. SessionManagerService
**Responsibility**: Conversation thread management

```csharp
public class SessionManagerService : ISessionManagerService
{
    private string? _currentThreadId;
    private readonly Dictionary<string, List<ChatMessage>> _conversations = new();
    
    public string? CurrentThreadId => _currentThreadId;
    
    public void StartNewSession()
    {
        _currentThreadId = null;
    }
    
    public void UpdateThreadId(string threadId)
    {
        _currentThreadId = threadId;
    }
}
```

**Key Features**:
- Thread ID management
- Conversation history
- Session persistence
- Multi-agent support

### Platform Services Layer

#### 1. MauiAgentApiService
**Responsibility**: Dynamic API service management

```csharp
public class MauiAgentApiService : IAgentApiService
{
    private readonly IConfigurationService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private ConfigurableAgentApiService? _currentService;
    
    // Delegates all calls to current service instance
    public async Task<List<string>> GetAgentsAsync()
    {
        var service = await GetOrCreateServiceAsync();
        return await service.GetAgentsAsync();
    }
    
    // Recreates service when URL changes
    private async Task<ConfigurableAgentApiService> GetOrCreateServiceAsync()
    {
        var currentUrl = await _configService.GetApiUrlAsync();
        
        if (_currentService?.BaseUrl != currentUrl)
        {
            _currentService?.Dispose();
            _currentService = new ConfigurableAgentApiService(
                _httpClientFactory.CreateClient(), 
                currentUrl
            );
        }
        
        return _currentService;
    }
}
```

#### 2. MauiSecureStorageService
**Responsibility**: Platform-specific secure storage

```csharp
public class MauiSecureStorageService : ISecureStorageService
{
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.GetAsync(key);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage error: {ex.Message}");
            return null;
        }
    }
    
    public async Task SetAsync(string key, string value)
    {
        await SecureStorage.SetAsync(key, value);
    }
    
    public Task RemoveAsync(string key)
    {
        SecureStorage.Remove(key);
        return Task.CompletedTask;
    }
}
```

## ?? Cross-Platform Considerations

### Platform Abstraction

The architecture abstracts platform-specific functionality through interfaces:

```csharp
// Cross-platform interface
public interface ISecureStorageService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task RemoveAsync(string key);
}

// Platform implementations
#if WINDOWS
public class WindowsSecureStorageService : ISecureStorageService { /* ... */ }
#elif IOS
public class iOSSecureStorageService : ISecureStorageService { /* ... */ }
#elif ANDROID
public class AndroidSecureStorageService : ISecureStorageService { /* ... */ }
#endif
```

### Dependency Registration

Platform-specific services are registered conditionally:

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        // Cross-platform registration
        builder.Services.AddSingleton<ISecureStorageService, MauiSecureStorageService>();
        
        // Platform-specific registrations
#if WINDOWS
        builder.Services.AddSingleton<IFileService, WindowsFileService>();
#elif ANDROID
        builder.Services.AddSingleton<IFileService, AndroidFileService>();
#endif
        
        return builder.Build();
    }
}
```

## ?? Security Architecture

### Data Protection

1. **Secure Storage**: Sensitive data encrypted using platform storage
2. **HTTPS Communication**: All API calls use secure transport
3. **Input Validation**: User input sanitized and validated
4. **Error Handling**: Sensitive information not exposed in errors

### Security Layers

```
???????????????????????????????????????
?         Application Layer            ?
?  • Input validation                 ?
?  • Error sanitization              ?
???????????????????????????????????????
???????????????????????????????????????
?       Communication Layer           ?
?  • HTTPS/TLS encryption            ?
?  • Certificate validation          ?
???????????????????????????????????????
???????????????????????????????????????
?         Storage Layer               ?
?  • Platform secure storage         ?
?  • Encrypted preferences           ?
???????????????????????????????????????
???????????????????????????????????????
?         Platform Layer              ?
?  • OS-level security               ?
?  • Keychain/Credential Manager     ?
???????????????????????????????????????
```

## ?? Performance Considerations

### Memory Management

1. **Disposable Resources**: Proper disposal of HTTP clients and streams
2. **Weak References**: Event handlers use weak references where appropriate
3. **Collection Management**: Observable collections optimized for UI binding

### Network Optimization

1. **Connection Pooling**: HTTP client factory for efficient connection reuse
2. **Streaming**: Real-time data processing without buffering entire responses
3. **Cancellation**: Proper cancellation token usage for request cancellation

### UI Performance

1. **Async Operations**: All long-running operations use async/await
2. **Main Thread**: UI updates dispatched to main thread
3. **Virtualization**: Large collections use virtualized controls

## ?? Testability Architecture

### Dependency Injection for Testing

All dependencies are injected, making components easily testable:

```csharp
[TestFixture]
public class AgentApiServiceTests
{
    private MockHttpMessageHandler _mockHttp;
    private HttpClient _httpClient;
    private AgentApiService _service;
    
    [SetUp]
    public void SetUp()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _service = new AgentApiService(_httpClient);
    }
    
    [Test]
    public async Task GetAgentsAsync_ReturnsExpectedAgents()
    {
        // Arrange
        _mockHttp.When("*/v1/agents")
                .Respond("application/json", "[\"agent1\", \"agent2\"]");
        
        // Act
        var agents = await _service.GetAgentsAsync();
        
        // Assert
        Assert.That(agents.Count, Is.EqualTo(2));
    }
}
```

### Service Abstractions

Core business logic depends on abstractions, not concrete implementations:

```csharp
public class MainPage : ContentPage
{
    private readonly IAgentApiService _apiService;
    private readonly IConfigurationService _configService;
    
    // Dependencies injected, not created
    public MainPage(IAgentApiService apiService, IConfigurationService configService)
    {
        _apiService = apiService;
        _configService = configService;
        InitializeComponent();
    }
}
```

## ?? Related Documentation

- [Getting Started](GETTING_STARTED.md) - Development setup
- [API Integration](API_INTEGRATION.md) - API communication details
- [Testing Guide](TESTING.md) - Testing strategies and examples
- [Contributing Guidelines](../CONTRIBUTING.md) - Development standards

---

This architecture provides a solid foundation for a maintainable, testable, and scalable cross-platform application while leveraging the full power of .NET MAUI and modern development practices.