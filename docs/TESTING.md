# Testing Guide

This document provides comprehensive information about the testing strategy, test structure, and how to run tests for the LombdaAgent MAUI project.

## ?? Testing Philosophy

The LombdaAgent MAUI project follows a comprehensive testing strategy that includes:

- **Unit Tests**: Fast, isolated tests for individual components
- **Integration Tests**: Tests that verify multiple components working together
- **Live Integration Tests**: Tests against real API endpoints
- **Platform Tests**: Platform-specific functionality verification

## ?? Test Project Structure

```
LombdaAgentMAUI.Tests/
??? Unit/                           # Unit tests (fast, no external dependencies)
?   ??? Services/
?   ?   ??? ConfigurationServiceTests.cs
?   ?   ??? AgentApiServiceTests.cs
?   ??? Models/
?       ??? ApiModelsTests.cs
??? Integration/                    # Integration tests (with mocks)
?   ??? ServiceIntegrationTests.cs
?   ??? LiveServerIntegrationTests.cs
??? Configuration/                  # Test configuration
?   ??? TestConfiguration.cs
??? Helpers/                        # Test utilities
?   ??? TestDataFactory.cs
??? Mocks/                         # Mock implementations
?   ??? MockSecureStorageService.cs
??? Scripts/                       # Test automation
?   ??? run-live-tests.ps1
?   ??? run-live-tests.bat
??? TestBase.cs                    # Common test functionality
??? README.md                      # Test-specific documentation
```

## ??? Test Categories

Tests are organized using categories to enable selective test execution:

| Category | Description | Speed | Dependencies |
|----------|-------------|-------|--------------|
| **Unit** | Individual component tests | Fast | None |
| **Integration** | Multi-component tests | Medium | Mocks only |
| **Network** | Tests requiring API server | Slow | Live API |
| **Api** | API-related functionality | Medium | HTTP client |
| **Configuration** | Settings and config tests | Fast | Secure storage |
| **Models** | Data model tests | Fast | None |
| **Services** | Service layer tests | Medium | Varies |
| **Slow** | Long-running tests | Slow | AI processing |

## ?? Running Tests

### Quick Test Execution

```bash
# Run all unit tests (recommended for development)
dotnet test --filter "Category!=Network" --verbosity normal

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --filter "Category!=Network"

# Run specific category
dotnet test --filter "Category=Unit"
```

### Full Test Suite

```bash
# Run all tests including integration tests
dotnet test --verbosity normal

# Run with detailed output
dotnet test --verbosity diagnostic
```

### Live Integration Tests

**Prerequisites:**
1. LombdaAgent API server must be running
2. OpenAI API key must be configured

```bash
# Set environment variables
export OPENAI_API_KEY="your-openai-api-key-here"
export LOMBDA_API_URL="https://localhost:5001"  # Optional, defaults to localhost:5001

# Run live tests using PowerShell script (recommended)
./LombdaAgentMAUI.Tests/Scripts/run-live-tests.ps1

# Or run directly with dotnet
dotnet test --filter "Category=Network" --verbosity normal
```

#### PowerShell Script Options

```powershell
# Basic execution
.\run-live-tests.ps1

# Skip slow AI processing tests
.\run-live-tests.ps1 -SkipSlowTests

# Use custom API URL
.\run-live-tests.ps1 -ApiUrl "https://your-api-server.com"

# Verbose output
.\run-live-tests.ps1 -Verbose
```

## ?? Test Examples

### Unit Test Example

```csharp
[TestFixture]
[Category("Unit")]
[Category("Models")]
public class ChatMessageTests
{
    [Test]
    public void ChatMessage_PropertyChangedEvent_FiresWhenTextChanges()
    {
        // Arrange
        var message = new ChatMessage();
        var eventFired = false;
        message.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(ChatMessage.Text))
                eventFired = true;
        };

        // Act
        message.Text = "New text";

        // Assert
        Assert.That(eventFired, Is.True);
        Assert.That(message.Text, Is.EqualTo("New text"));
    }

    [Test]
    public void ChatMessage_IsMarkdown_DefaultsCorrectlyBasedOnUser()
    {
        // Arrange & Act
        var userMessage = new ChatMessage { IsUser = true };
        var agentMessage = new ChatMessage { IsUser = false };

        // Assert
        Assert.That(userMessage.IsMarkdown, Is.False);
        Assert.That(agentMessage.IsMarkdown, Is.True);
    }
}
```

### Integration Test Example

```csharp
[TestFixture]
[Category("Integration")]
[Category("Services")]
public class ServiceIntegrationTests
{
    private IServiceProvider _serviceProvider;
    private IAgentApiService _apiService;
    private IConfigurationService _configService;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        
        // Register services as they would be in the real app
        services.AddSingleton<ISecureStorageService, MockSecureStorageService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddHttpClient();
        services.AddSingleton<IAgentApiService, MauiAgentApiService>();

        _serviceProvider = services.BuildServiceProvider();
        _apiService = _serviceProvider.GetRequiredService<IAgentApiService>();
        _configService = _serviceProvider.GetRequiredService<IConfigurationService>();
    }

    [Test]
    public async Task ConfigurationService_Integration_PersistsApiUrl()
    {
        // Arrange
        const string testUrl = "https://test-api.example.com";

        // Act
        await _configService.SetApiUrlAsync(testUrl);
        var retrievedUrl = await _configService.GetApiUrlAsync();

        // Assert
        Assert.That(retrievedUrl, Is.EqualTo(testUrl));
    }
}
```

### Live API Test Example

```csharp
[TestFixture]
[Category("Network")]
[Category("Api")]
public class LiveApiTests
{
    private IAgentApiService _apiService;
    private string _testAgentId;

    [SetUp]
    public async Task SetUp()
    {
        var config = TestConfiguration.GetLiveTestConfiguration();
        
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(config.ApiUrl);
        
        _apiService = new AgentApiService(httpClient);

        // Create a test agent for the test session
        var agentResponse = await _apiService.CreateAgentAsync($"Test-{Guid.NewGuid():N}");
        _testAgentId = agentResponse?.Id ?? throw new InvalidOperationException("Failed to create test agent");
    }

    [Test]
    [Category("Slow")]
    public async Task SendMessage_StreamingEnabled_ReceivesIncrementalResponse()
    {
        // Arrange
        var receivedChunks = new List<string>();
        var receivedEvents = new List<StreamingEventData>();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        // Act
        var resultThreadId = await _apiService.SendMessageStreamWithEventsAsync(
            _testAgentId,
            "Please count from 1 to 3, with a brief pause between each number.",
            null,
            onMessageReceived: text => {
                receivedChunks.Add(text);
                Console.WriteLine($"Received chunk: '{text}'");
            },
            onEventReceived: eventData => {
                receivedEvents.Add(eventData);
                Console.WriteLine($"Event: {eventData.EventType}");
            },
            cancellationSource.Token
        );

        // Assert
        Assert.That(receivedChunks.Count, Is.GreaterThan(0), "Should receive at least one text chunk");
        Assert.That(resultThreadId, Is.Not.Null.And.Not.Empty, "Should return a thread ID");
        
        // Verify we received expected events
        Assert.That(receivedEvents.Any(e => e.EventType == "connected"), "Should receive connected event");
        Assert.That(receivedEvents.Any(e => e.EventType == "complete"), "Should receive complete event");
        
        var deltaEvents = receivedEvents.Where(e => e.EventType == "delta").ToList();
        Assert.That(deltaEvents.Count, Is.GreaterThan(0), "Should receive delta events");
    }
}
```

## ??? Test Utilities and Helpers

### Test Data Factory

```csharp
public static class TestDataFactory
{
    public static ChatMessage CreateUserMessage(string text = "Test message")
    {
        return new ChatMessage
        {
            Text = text,
            IsUser = true,
            Timestamp = DateTime.Now
        };
    }

    public static ChatMessage CreateAgentMessage(string text = "Test response")
    {
        return new ChatMessage
        {
            Text = text,
            IsUser = false,
            IsMarkdown = true,
            Timestamp = DateTime.Now
        };
    }

    public static AgentResponse CreateAgentResponse(string? id = null, string? name = null)
    {
        return new AgentResponse
        {
            Id = id ?? $"agent-{Guid.NewGuid():N}",
            Name = name ?? "Test Agent"
        };
    }
}
```

### Mock Services

```csharp
public class MockSecureStorageService : ISecureStorageService
{
    private readonly Dictionary<string, string> _storage = new();

    public Task<string?> GetAsync(string key)
    {
        _storage.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task SetAsync(string key, string value)
    {
        _storage[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _storage.Remove(key);
        return Task.CompletedTask;
    }

    public void Clear() => _storage.Clear();
}
```

### Test Configuration

```csharp
public static class TestConfiguration
{
    public static LiveTestConfiguration GetLiveTestConfiguration()
    {
        var apiUrl = Environment.GetEnvironmentVariable("LOMBDA_API_URL") 
                     ?? "https://localhost:5001";
        
        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        
        if (string.IsNullOrEmpty(openAiApiKey))
        {
            throw new InvalidOperationException(
                "OPENAI_API_KEY environment variable is required for live tests");
        }

        return new LiveTestConfiguration
        {
            ApiUrl = apiUrl,
            OpenAiApiKey = openAiApiKey,
            RequestTimeout = TimeSpan.FromMinutes(2)
        };
    }
}

public class LiveTestConfiguration
{
    public string ApiUrl { get; set; } = string.Empty;
    public string OpenAiApiKey { get; set; } = string.Empty;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(1);
}
```

## ?? Test Debugging

### Debugging Unit Tests

1. **Set Breakpoints**: Use Visual Studio or VS Code debugging
2. **Test Explorer**: Run individual tests through the Test Explorer
3. **Debug Output**: Use `Debug.WriteLine()` for test output

```csharp
[Test]
public void DebugExample()
{
    Debug.WriteLine("Starting test execution");
    
    // Test code here
    
    Debug.WriteLine("Test completed successfully");
}
```

### Debugging Integration Tests

1. **Mock Verification**: Verify mock calls and state
2. **Service State**: Inspect service configurations
3. **Logging**: Enable detailed logging in test services

### Debugging Live Tests

1. **API Server Logs**: Monitor server logs during test execution
2. **Network Traffic**: Use Fiddler or similar tools to inspect HTTP traffic
3. **Environment Variables**: Verify all required environment variables are set

```bash
# Verify environment setup
echo $OPENAI_API_KEY
echo $LOMBDA_API_URL

# Check API server status
curl -X GET https://localhost:5001/v1/agents
```

## ?? Test Coverage

### Measuring Coverage

```bash
# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report (requires reportgenerator tool)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

### Coverage Goals

| Component | Target Coverage | Current Status |
|-----------|----------------|----------------|
| **Core Models** | 95%+ | ? |
| **Core Services** | 90%+ | ? |
| **API Integration** | 85%+ | ?? |
| **MAUI Services** | 80%+ | ?? |
| **UI Logic** | 70%+ | ?? |

## ?? Troubleshooting Tests

### Common Issues

**"Tests fail to discover"**
- Ensure test projects reference `Microsoft.NET.Test.Sdk`
- Verify test methods have `[Test]` attributes
- Check that test classes have `[TestFixture]` attributes

**"Live tests fail with connection errors"**
- Verify API server is running and accessible
- Check environment variables are set correctly
- Ensure firewall allows connections

**"Streaming tests timeout"**
- Increase timeout values for slow networks
- Check OpenAI API key validity
- Verify API server has proper OpenAI integration

### Test Environment Setup

```bash
# Verify .NET SDK
dotnet --version

# Restore test packages
dotnet restore LombdaAgentMAUI.Tests

# Build test project
dotnet build LombdaAgentMAUI.Tests

# List available tests
dotnet test LombdaAgentMAUI.Tests --list-tests
```

## ?? Continuous Integration

### GitHub Actions Example

```yaml
name: Test Suite

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run Unit Tests
      run: dotnet test --no-build --filter "Category!=Network" --verbosity normal

  integration-tests:
    runs-on: ubuntu-latest
    needs: unit-tests
    if: github.event_name == 'push'
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Run Integration Tests
      env:
        OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
      run: |
        # Start API server in background
        dotnet run --project LombdaAgentAPI &
        sleep 30
        
        # Run integration tests
        dotnet test --filter "Category=Network" --verbosity normal
```

## ?? Related Documentation

- [Getting Started](GETTING_STARTED.md) - Project setup and basics
- [API Integration](API_INTEGRATION.md) - API communication details
- [Architecture Overview](ARCHITECTURE.md) - System design
- [Contributing Guidelines](../CONTRIBUTING.md) - Development guidelines

---

A comprehensive testing strategy ensures the reliability and maintainability of the LombdaAgent MAUI application across all supported platforms.