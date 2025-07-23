# API Integration Guide

This document provides detailed information about how LombdaAgent MAUI integrates with the LombdaAgent API, including supported endpoints, request/response formats, and streaming capabilities.

## ?? API Overview

LombdaAgent MAUI communicates with the LombdaAgent API through RESTful HTTP endpoints and Server-Sent Events (SSE) for streaming responses. The integration is handled by the `AgentApiService` class in the Core library.

## ??? Service Architecture

### Core Components

```
AgentApiService (Core)
??? IAgentApiService (Interface)
??? MauiAgentApiService (Platform-specific)
??? ConfigurableAgentApiService (URL management)
??? AgentApiService (Base implementation)
```

### Service Registration

Services are registered in `MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<IAgentApiService, MauiAgentApiService>();
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
builder.Services.AddSingleton<ISessionManagerService, SessionManagerService>();
```

## ?? Supported Endpoints

### Agent Management

#### List Agents
```http
GET /v1/agents
Accept: application/json
```

**Response:**
```json
["agent-id-1", "agent-id-2", "agent-id-3"]
```

#### Get Agent Details
```http
GET /v1/agents/{agentId}
Accept: application/json
```

**Response:**
```json
{
  "id": "agent-123",
  "name": "My Assistant"
}
```

#### Create Agent
```http
POST /v1/agents
Content-Type: application/json

{
  "name": "New Assistant",
  "agentType": "Default"
}
```

**Response:**
```json
{
  "id": "agent-456",
  "name": "New Assistant"
}
```

#### Get Agent Types
```http
GET /v1/agents/types
Accept: application/json
```

**Response:**
```json
["Default", "Specialized", "Creative"]
```

### Messaging

#### Send Message (Synchronous)
```http
POST /v1/agents/{agentId}/messages
Content-Type: application/json

{
  "text": "Hello, how can you help me?",
  "threadId": "thread-123"  // Optional
}
```

**Response:**
```json
{
  "agentId": "agent-123",
  "threadId": "thread-456",
  "text": "I'd be happy to help! What would you like to know?"
}
```

#### Send Message (Streaming)
```http
POST /v1/agents/{agentId}/messages/stream
Content-Type: application/json
Accept: text/event-stream
Cache-Control: no-cache
Connection: keep-alive

{
  "text": "Tell me a story",
  "threadId": "thread-123"  // Optional
}
```

## ?? Streaming Implementation

### Enhanced Streaming Events

The application supports a rich streaming event system with detailed event information:

#### Event Types

1. **connected** - Initial connection established
2. **created** - Stream creation with response metadata
3. **delta** - Text chunks as they arrive
4. **stream_complete** - Individual stream completion
5. **complete** - Final response with thread ID
6. **reasoning** - AI reasoning steps (for compatible models)
7. **error/stream_error** - Error events

### Server-Sent Events Format

```
event: connected
data: {}

event: created
data: {"responseId": "resp-123", "sequenceId": 1}

event: delta
data: {"text": "Hello", "sequenceId": 2, "outputIndex": 0}

event: delta
data: {"text": " there!", "sequenceId": 3, "outputIndex": 0}

event: complete
data: {"threadId": "thread-456", "text": "Hello there!"}
```

### Streaming Usage Examples

#### Basic Streaming
```csharp
await agentApiService.SendMessageStreamAsync(
    agentId: "agent-123",
    message: "Hello!",
    threadId: "thread-456",
    onMessageReceived: (text) => {
        // Handle incoming text chunks
        Console.WriteLine($"Received: {text}");
    },
    cancellationToken: cancellationToken
);
```

#### Enhanced Streaming with Events
```csharp
var resultThreadId = await agentApiService.SendMessageStreamWithEventsAsync(
    agentId: "agent-123",
    message: "Tell me about AI",
    threadId: "thread-456",
    onMessageReceived: (text) => {
        // Handle text chunks
        AppendToChat(text);
    },
    onEventReceived: (eventData) => {
        // Handle specific events
        switch (eventData.EventType)
        {
            case "connected":
                ShowStatus("Connected to stream");
                break;
            case "delta":
                LogDebug($"Chunk #{eventData.SequenceId}: {eventData.Text}");
                break;
            case "complete":
                ShowStatus($"Complete - Thread: {eventData.ThreadId}");
                break;
            case "error":
                ShowError($"Stream error: {eventData.Error}");
                break;
        }
    },
    cancellationToken: cancellationToken
);
```

## ?? Configuration Management

### Dynamic URL Configuration

The `MauiAgentApiService` supports dynamic URL changes:

```csharp
public class MauiAgentApiService : IAgentApiService
{
    private readonly IConfigurationService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private ConfigurableAgentApiService? _currentService;

    public async Task<List<string>> GetAgentsAsync()
    {
        var service = await GetOrCreateServiceAsync();
        return await service.GetAgentsAsync();
    }

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
}
```

### Secure Configuration Storage

Configuration is stored securely using platform-specific secure storage:

```csharp
public interface ISecureStorageService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task RemoveAsync(string key);
}

// Platform implementation
public class MauiSecureStorageService : ISecureStorageService
{
    public async Task<string?> GetAsync(string key)
    {
        return await SecureStorage.GetAsync(key);
    }

    public async Task SetAsync(string key, string value)
    {
        await SecureStorage.SetAsync(key, value);
    }

    public async Task RemoveAsync(string key)
    {
        SecureStorage.Remove(key);
    }
}
```

## ?? Security Considerations

### API Communication

1. **HTTPS Only**: All API communication should use HTTPS in production
2. **Timeout Configuration**: Reasonable timeouts prevent hanging requests
3. **Error Handling**: Comprehensive error handling with user-friendly messages

```csharp
private static HttpClient CreateHttpClient(string baseUrl)
{
    var client = new HttpClient();
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // Configurable timeout
    return client;
}
```

### Sensitive Data Storage

- **API URLs**: Stored in secure storage
- **User Preferences**: Encrypted platform storage
- **Session Data**: Temporary, cleared on app restart
- **Tokens**: If implemented, stored securely

## ?? Error Handling

### HTTP Error Responses

```csharp
try
{
    var response = await _httpClient.GetAsync("v1/agents");
    response.EnsureSuccessStatusCode();
    // Process successful response
}
catch (HttpRequestException ex)
{
    System.Diagnostics.Debug.WriteLine($"HTTP Error: {ex.Message}");
    // Handle network errors
}
catch (TaskCanceledException ex)
{
    System.Diagnostics.Debug.WriteLine($"Request timeout: {ex.Message}");
    // Handle timeouts
}
catch (JsonException ex)
{
    System.Diagnostics.Debug.WriteLine($"JSON parsing error: {ex.Message}");
    // Handle malformed responses
}
```

### Streaming Error Handling

```csharp
event: stream_error
data: {"error": "Model temporarily unavailable", "code": "MODEL_UNAVAILABLE"}
```

Error events are handled in the streaming implementation:

```csharp
case "stream_error":
case "error":
    var errorData = JsonSerializer.Deserialize<StreamingEventData>(data);
    await onMessageReceived($"Error: {errorData.Error}");
    if (onEventReceived != null)
    {
        await onEventReceived(errorData);
    }
    break;
```

## ?? Performance Optimization

### HTTP Client Management

- **Connection Pooling**: Uses `IHttpClientFactory` for efficient connection management
- **Timeout Configuration**: Prevents resource leaks from hanging connections
- **Proper Disposal**: Ensures resources are cleaned up correctly

### Streaming Optimization

- **Buffer Management**: Efficient text buffer handling for large responses
- **Cancellation Support**: Proper cancellation token usage
- **Memory Management**: Minimal memory allocation during streaming

## ?? Testing API Integration

### Unit Tests

Test individual API methods in isolation:

```csharp
[Test]
public async Task GetAgentsAsync_ReturnsAgentList()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When("*/v1/agents")
           .Respond("application/json", "[\"agent1\", \"agent2\"]");
    
    var client = mockHttp.ToHttpClient();
    var service = new AgentApiService(client);

    // Act
    var agents = await service.GetAgentsAsync();

    // Assert
    Assert.That(agents.Count, Is.EqualTo(2));
    Assert.That(agents[0], Is.EqualTo("agent1"));
}
```

### Integration Tests

Test the complete API integration:

```csharp
[Test, Category("Network")]
public async Task CreateAgent_WithValidData_ReturnsAgentResponse()
{
    // Requires running API server
    var response = await _apiService.CreateAgentAsync("Test Agent");
    
    Assert.That(response, Is.Not.Null);
    Assert.That(response.Name, Is.EqualTo("Test Agent"));
    Assert.That(response.Id, Is.Not.Null.And.Not.Empty);
}
```

### Live Streaming Tests

Test streaming functionality against a real API:

```csharp
[Test, Category("Network"), Category("Slow")]
public async Task StreamMessage_ReceivesIncrementalResponse()
{
    var chunks = new List<string>();
    var events = new List<StreamingEventData>();
    
    await _apiService.SendMessageStreamWithEventsAsync(
        agentId: "test-agent",
        message: "Count to 5",
        threadId: null,
        onMessageReceived: text => chunks.Add(text),
        onEventReceived: eventData => events.Add(eventData)
    );
    
    Assert.That(chunks.Count, Is.GreaterThan(0));
    Assert.That(events.Any(e => e.EventType == "connected"));
    Assert.That(events.Any(e => e.EventType == "complete"));
}
```

## ?? Related Documentation

- [Getting Started Guide](GETTING_STARTED.md) - Basic setup and usage
- [Architecture Overview](ARCHITECTURE.md) - System design details
- [Streaming Guide](../LombdaAgentMAUI/STREAMING_GUIDE.md) - Enhanced streaming features
- [Testing Guide](TESTING.md) - Comprehensive testing strategies

---

This API integration provides a robust foundation for AI agent communication with comprehensive error handling, streaming support, and platform-specific optimizations.