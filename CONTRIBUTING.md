# Contributing to LombdaAgent MAUI

Thank you for your interest in contributing to LombdaAgent MAUI! This document provides guidelines and information for contributors.

## ?? How to Contribute

### Types of Contributions

We welcome various types of contributions:

- **Bug Reports**: Help us identify and fix issues
- **Feature Requests**: Suggest new functionality
- **Code Contributions**: Bug fixes, new features, improvements
- **Documentation**: Improve existing docs or add new ones
- **Testing**: Add tests, improve test coverage
- **Performance**: Optimize existing code
- **UI/UX**: Improve user interface and experience

### Before You Start

1. **Search existing issues** to avoid duplicates
2. **Read the documentation** to understand the project
3. **Check the roadmap** to see planned features
4. **Join discussions** to get feedback on your ideas

## ?? Getting Started

### Development Setup

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:git clone https://github.com/your-username/LombdaAgentMAUI.git
   cd LombdaAgentMAUI
3. **Set up the development environment**:# Install .NET 9 SDK
# Install Visual Studio 2022 with MAUI workload

# Restore packages
dotnet restore

# Build the solution
dotnet build
4. **Run tests** to ensure everything works:dotnet test --filter "Category!=Network"
5. **Create a branch** for your work:git checkout -b feature/your-feature-name
# or
git checkout -b fix/issue-number
### Development Workflow

1. **Make your changes** following our coding standards
2. **Add tests** for new functionality
3. **Update documentation** if needed
4. **Run the full test suite**:dotnet test5. **Build and test** on target platforms
6. **Commit your changes** with clear messages
7. **Push to your fork** and create a pull request

## ?? Coding Standards

### C# Code Style

Follow Microsoft's C# coding conventions with these project-specific guidelines:

#### Naming Conventions
// Public members: PascalCase
public class AgentApiService
public string AgentName { get; set; }
public async Task GetAgentsAsync()

// Private fields: _camelCase
private readonly HttpClient _httpClient;
private string _currentThreadId;

// Method parameters and local variables: camelCase
public async Task SendMessage(string agentId, string messageText)
{
    var response = await _httpClient.PostAsync(url, content);
}

// Constants: PascalCase
public const string DefaultApiUrl = "https://localhost:5001";
private const int MaxRetryAttempts = 3;

// Interface names: IPascalCase
public interface IAgentApiService
public interface IConfigurationService
#### File Organization
// 1. Using statements (sorted alphabetically)
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LombdaAgentMAUI.Core.Models;
using Microsoft.Extensions.Logging;

// 2. Namespace
namespace LombdaAgentMAUI.Core.Services
{
    // 3. Class documentation
    /// <summary>
    /// Service for communicating with the LombdaAgent API
    /// </summary>
    public class AgentApiService : IAgentApiService
    {
        // 4. Private fields
        private readonly HttpClient _httpClient;
        private readonly ILogger<AgentApiService> _logger;
        
        // 5. Constructor
        public AgentApiService(HttpClient httpClient, ILogger<AgentApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        
        // 6. Public properties
        public string BaseUrl => _httpClient.BaseAddress?.ToString() ?? string.Empty;
        
        // 7. Public methods
        public async Task<List<string>> GetAgentsAsync()
        {
            // Implementation
        }
        
        // 8. Private methods
        private async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
        {
            // Implementation
        }
    }
}
#### Error Handling
// Good: Specific exception handling with logging
public async Task<List<string>> GetAgentsAsync()
{
    try
    {
        var response = await _httpClient.GetAsync("v1/agents");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<string>>(content, _jsonOptions) 
               ?? new List<string>();
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "HTTP error while fetching agents");
        return new List<string>();
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "JSON parsing error while fetching agents");
        return new List<string>();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error while fetching agents");
        throw; // Re-throw unexpected exceptions
    }
}

// Bad: Generic exception swallowing
public async Task<List<string>> GetAgentsAsync()
{
    try
    {
        // ... implementation
    }
    catch
    {
        return new List<string>(); // Don't do this!
    }
}
### XAML Standards

#### Layout and Naming
<!-- Good: Clear structure with meaningful names -->
<ContentPage x:Class="LombdaAgentMAUI.MainPage"
             xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             Title="LombdaAgent Chat">
    
    <Grid x:Name="MainGrid" 
          RowDefinitions="Auto,*,Auto"
          Padding="20">
        
        <!-- Agent Selection -->
        <StackLayout x:Name="AgentSelectionLayout" 
                     Grid.Row="0"
                     Orientation="Horizontal"
                     Spacing="10">
            
            <Label Text="Agent:" 
                   VerticalOptions="Center" />
            
            <Picker x:Name="AgentPicker"
                    ItemsSource="{Binding Agents}"
                    SelectedItem="{Binding SelectedAgent}"
                    HorizontalOptions="FillAndExpand" />
        </StackLayout>
        
        <!-- Chat Messages -->
        <CollectionView x:Name="MessagesCollectionView"
                        Grid.Row="1"
                        ItemsSource="{Binding Messages}"
                        Margin="0,10">
            
            <CollectionView.ItemTemplate>
                <DataTemplate>
                    <!-- Message template -->
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>
        
        <!-- Message Input -->
        <StackLayout x:Name="MessageInputLayout"
                     Grid.Row="2"
                     Orientation="Horizontal"
                     Spacing="10">
            
            <Entry x:Name="MessageEntry"
                   Placeholder="Type your message..."
                   Text="{Binding MessageText}"
                   HorizontalOptions="FillAndExpand" />
            
            <Button x:Name="SendButton"
                    Text="Send"
                    Command="{Binding SendMessageCommand}" />
        </StackLayout>
    </Grid>
</ContentPage>
### Testing Standards

#### Unit Test Structure
[TestFixture]
[Category("Unit")]
[Category("Services")]
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
    
    [TearDown]
    public void TearDown()
    {
        _service?.Dispose();
        _httpClient?.Dispose();
    }
    
    [Test]
    public async Task GetAgentsAsync_WithValidResponse_ReturnsAgentList()
    {
        // Arrange
        var expectedAgents = new[] { "agent1", "agent2" };
        _mockHttp.When("*/v1/agents")
                .Respond("application/json", JsonSerializer.Serialize(expectedAgents));
        
        // Act
        var result = await _service.GetAgentsAsync();
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result, Is.EquivalentTo(expectedAgents));
    }
    
    [Test]
    public async Task GetAgentsAsync_WithHttpError_ReturnsEmptyList()
    {
        // Arrange
        _mockHttp.When("*/v1/agents")
                .Respond(HttpStatusCode.InternalServerError);
        
        // Act
        var result = await _service.GetAgentsAsync();
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }
}
#### Test Categories

Use appropriate test categories:
[Category("Unit")]        // Fast, isolated tests
[Category("Integration")] // Multi-component tests
[Category("Network")]     // Tests requiring external API
[Category("Slow")]        // Long-running tests
[Category("UI")]          // User interface tests
## ?? Documentation Standards

### Code Documentation
/// <summary>
/// Sends a message to the specified agent and returns the response.
/// </summary>
/// <param name="agentId">The unique identifier of the target agent</param>
/// <param name="message">The message text to send</param>
/// <param name="threadId">Optional thread ID for conversation context</param>
/// <returns>The agent's response, or null if an error occurred</returns>
/// <exception cref="ArgumentException">Thrown when agentId or message is empty</exception>
/// <exception cref="HttpRequestException">Thrown when the API request fails</exception>
public async Task<MessageResponse?> SendMessageAsync(
    string agentId, 
    string message, 
    string? threadId = null)
{
    // Implementation
}
### README Updates

When adding new features, update relevant documentation:

- **README.md**: Update feature list and usage examples
- **API_INTEGRATION.md**: Document new API endpoints
- **ARCHITECTURE.md**: Update architecture diagrams if needed
- **TESTING.md**: Add testing guidelines for new features

### Inline Comments
public async Task<string?> SendMessageStreamAsync(/* parameters */)
{
    // Create HTTP request with streaming headers
    using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };
    
    // Enable Server-Sent Events
    request.Headers.Add("Accept", "text/event-stream");
    request.Headers.Add("Cache-Control", "no-cache");
    
    // Send request with response headers read immediately
    // This allows us to start processing the stream before the entire response is received
    using var response = await _httpClient.SendAsync(
        request, 
        HttpCompletionOption.ResponseHeadersRead, 
        cancellationToken
    );
    
    // Process streaming response
    await ProcessStreamingResponse(response, onMessageReceived, cancellationToken);
}
## ?? Pull Request Process

### Before Submitting

1. **Run all tests**:dotnet test
2. **Check code formatting**:dotnet format
3. **Build all platforms** (if possible):dotnet build -f net9.0-windows10.0.19041.0
dotnet build -f net9.0-ios
dotnet build -f net9.0-android
4. **Update documentation** if needed

### Pull Request Template

When creating a pull request, include:
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update
- [ ] Performance improvement

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing performed

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] Tests pass locally
- [ ] No new warnings introduced

## Related Issues
Fixes #123
Relates to #456
### Review Process

1. **Automated checks** must pass (CI/CD)
2. **Code review** by maintainers
3. **Testing** on multiple platforms (if applicable)
4. **Documentation review**
5. **Approval** and merge

## ?? Bug Reports

### Bug Report Template
**Describe the Bug**
A clear description of what the bug is.

**Steps to Reproduce**
1. Go to '...'
2. Click on '....'
3. Scroll down to '....'
4. See error

**Expected Behavior**
What you expected to happen.

**Actual Behavior**
What actually happened.

**Screenshots**
If applicable, add screenshots.

**Environment**
- OS: [e.g., Windows 11, iOS 17]
- .NET Version: [e.g., 9.0.0]
- App Version: [e.g., 1.0.0]

**Additional Context**
Any other context about the problem.

**Logs**Paste relevant log entries here
## ?? Feature Requests

### Feature Request Template
**Feature Description**
A clear description of the feature you'd like to see.

**Problem Statement**
What problem would this feature solve?

**Proposed Solution**
How you envision this feature working.

**Alternatives Considered**
Other approaches you've considered.

**Additional Context**
Screenshots, mockups, or examples.

**Implementation Notes**
Technical considerations (if any).
## ?? Development Guidelines

### Performance Considerations

1. **Async/Await**: Use async operations for I/O bound work
2. **Memory Management**: Dispose resources properly
3. **UI Threading**: Keep UI responsive with async operations
4. **Caching**: Cache frequently accessed data appropriately

### Security Guidelines

1. **Input Validation**: Validate all user inputs
2. **Secure Storage**: Use platform secure storage for sensitive data
3. **HTTPS**: Always use secure communication
4. **Error Messages**: Don't expose sensitive information in errors

### Accessibility

1. **Labels**: Provide meaningful labels for UI elements
2. **Navigation**: Ensure keyboard/screen reader navigation works
3. **Contrast**: Maintain sufficient color contrast
4. **Text Size**: Support dynamic text sizing

## ?? Project Structure

When adding new files, follow the established structure:
LombdaAgentMAUI/
??? LombdaAgentMAUI/           # MAUI App Project
?   ??? Views/                 # XAML pages and custom views
?   ??? ViewModels/           # View models (if using explicit MVVM)
?   ??? Services/             # Platform-specific services
?   ??? Converters/           # Value converters
?   ??? Controls/             # Custom controls
?   ??? Resources/            # Images, fonts, styles
?   ??? Platforms/            # Platform-specific code
??? LombdaAgentMAUI.Core/     # Shared Logic
?   ??? Models/               # Data models and DTOs
?   ??? Services/             # Business logic services
?   ??? Interfaces/           # Service contracts
?   ??? Extensions/           # Extension methods
??? LombdaAgentMAUI.Tests/    # Test Project
?   ??? Unit/                 # Unit tests
?   ??? Integration/          # Integration tests
?   ??? Helpers/              # Test helpers
?   ??? Mocks/                # Mock implementations
??? docs/                     # Documentation
    ??? GETTING_STARTED.md
    ??? API_INTEGRATION.md
    ??? ARCHITECTURE.md
    ??? TESTING.md
## ?? Recognition

Contributors will be recognized in:

- **README.md**: Contributors section
- **CHANGELOG.md**: Release notes
- **GitHub**: Contributor graphs and statistics

## ?? Getting Help

- **GitHub Issues**: For bugs and feature requests
- **GitHub Discussions**: For questions and ideas
- **Code Reviews**: For feedback on your contributions

## ?? Resources

- [.NET MAUI Documentation](https://docs.microsoft.com/en-us/dotnet/maui/)
- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- [Git Best Practices](https://git-scm.com/book/en/v2)
- [Semantic Versioning](https://semver.org/)

---

Thank you for contributing to LombdaAgent MAUI! Your contributions help make this project better for everyone. ??