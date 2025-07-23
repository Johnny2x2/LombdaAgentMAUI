# LombdaAgent MAUI

A cross-platform .NET MAUI application for interacting with AI agents using the LombdaAiAgents library. This application provides a rich, native user interface for creating, managing, and chatting with AI agents across iOS, Android, macOS, and Windows platforms.

![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)
![MAUI](https://img.shields.io/badge/MAUI-Cross--Platform-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## ?? Features

- **Cross-Platform**: Runs natively on iOS, Android, macOS, and Windows
- **Agent Management**: Create, configure, and manage AI agents
- **Real-time Chat**: Interactive conversations with streaming response support
- **Enhanced Streaming**: Advanced streaming with detailed event information
- **Session Management**: Persistent conversation threads
- **Settings Configuration**: Configurable API endpoints and preferences
- **Secure Storage**: Encrypted storage for sensitive configuration data
- **Markdown Support**: Rich text rendering for agent responses
- **System Logs**: Comprehensive logging and debugging capabilities

## ?? Platform Support

| Platform | Minimum Version |
|----------|----------------|
| **iOS** | 15.0+ |
| **Android** | API 21+ (Android 5.0+) |
| **macOS** | 12.0+ (Mac Catalyst) |
| **Windows** | Windows 10 build 19041+ |

## ??? Architecture

The application follows a clean architecture pattern with clear separation of concerns:
LombdaAgentMAUI/
??? LombdaAgentMAUI/           # Main MAUI application
?   ??? Views/                 # XAML pages and views
?   ??? Services/             # Platform-specific services
?   ??? Converters/           # XAML value converters
?   ??? Controls/             # Custom controls
??? LombdaAgentMAUI.Core/     # Shared business logic
?   ??? Models/               # Data models and DTOs
?   ??? Services/             # Core services and interfaces
??? LombdaAgentMAUI.Tests/    # Comprehensive test suite
    ??? Unit/                 # Unit tests
    ??? Integration/          # Integration tests
    ??? Live/                 # Live API tests
### Key Components

- **Core Services**: Abstracted business logic for cross-platform compatibility
- **MVVM Pattern**: Clean separation between UI and business logic
- **Dependency Injection**: Centralized service registration and management
- **Secure Storage**: Platform-specific encrypted storage implementation

## ??? Getting Started

### Prerequisites

1. **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download)
2. **Visual Studio 2022** (17.8+) or **Visual Studio Code** with MAUI extensions
3. **LombdaAgent API** - A running instance of the LombdaAgent API
4. **OpenAI API Key** - Required for AI functionality

### Development Setup

1. **Clone the repository**git clone https://github.com/lombda/LombdaAgentMAUI.git
cd LombdaAgentMAUI
2. **Restore NuGet packages**dotnet restore
3. **Set up the API backend**
   - Ensure your LombdaAgent API is running (typically on `https://localhost:5001`)
   - Configure your OpenAI API key in the backend environment

4. **Build and run**# For Windows
dotnet build -f net9.0-windows10.0.19041.0
dotnet run --project LombdaAgentMAUI -f net9.0-windows10.0.19041.0

# For other platforms, use appropriate target framework
### First-Time Configuration

1. **Launch the application**
2. **Navigate to Settings tab**
3. **Configure API endpoint** (e.g., `https://localhost:5001`)
4. **Test connection** using the "Test Connection" button
5. **Create your first agent** using the "Create Agent" button
6. **Start chatting!**

## ?? Usage Guide

### Creating Agents

1. Navigate to the main chat interface
2. Click "Create Agent" button
3. Enter a name for your agent
4. Select agent type (if multiple types are available)
5. Your new agent will appear in the agent dropdown

### Chatting with Agents

1. **Select an Agent**: Choose from the dropdown list
2. **Enable/Disable Streaming**: Toggle real-time response streaming
3. **Send Messages**: Type your message and press Enter or click Send
4. **View Responses**: Responses appear with markdown formatting support
5. **Monitor Logs**: Check the system log panel for detailed information

### Advanced Features

#### Streaming Events

The application supports enhanced streaming with detailed event information:

- **Connection Events**: Real-time connection status
- **Delta Events**: Character-by-character response streaming
- **Completion Events**: Response finalization with metadata
- **Error Events**: Detailed error information and recovery

#### Session Management

- **Thread Persistence**: Conversations maintain context across app restarts
- **Session Recovery**: Automatic recovery of interrupted conversations
- **Multi-Agent Support**: Switch between different agents seamlessly

## ?? Configuration

### API Settings

Configure the following settings in the Settings tab:

| Setting | Description | Default |
|---------|-------------|---------|
| **API Base URL** | LombdaAgent API endpoint | `https://localhost:5001` |
| **Streaming Enabled** | Enable real-time streaming | `true` |
| **Auto-scroll** | Auto-scroll chat during streaming | `true` |

### Secure Storage

The application uses platform-specific secure storage for:
- API endpoint configuration
- User preferences
- Session data
- Authentication tokens (if applicable)

## ?? Testing

The project includes a comprehensive test suite:
# Run all tests
dotnet test

# Run only unit tests (fast)
dotnet test --filter "Category!=Network"

# Run integration tests with live API
dotnet test --filter "Category=Network"
### Test Categories

- **Unit Tests**: Fast, isolated component tests
- **Integration Tests**: Multi-component interaction tests
- **Network Tests**: Live API integration tests
- **Slow Tests**: AI processing and long-running tests

## ?? API Integration

### Supported Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/v1/agents` | GET | List all agents |
| `/v1/agents` | POST | Create new agent |
| `/v1/agents/{id}` | GET | Get agent details |
| `/v1/agents/{id}/messages` | POST | Send message (synchronous) |
| `/v1/agents/{id}/messages/stream` | POST | Send message (streaming) |

### Response Formats

#### Standard Message Response{
  "agentId": "agent-123",
  "threadId": "thread-456",
  "text": "Response from the agent"
}
#### Streaming Eventsevent: connected
data: {}

event: delta
data: {"text": "Hello", "sequenceId": 1}

event: complete
data: {"threadId": "thread-456", "text": "Complete response"}
## ?? Troubleshooting

### Common Issues

**Connection Problems**
- Verify API URL is correct and accessible
- Check if the API server is running
- Ensure network connectivity

**No Agents Available**
- Create agents through the app interface
- Verify API connectivity
- Check server logs for errors

**Streaming Issues**
- Test with streaming disabled first
- Check network stability
- Verify API endpoint supports streaming

**Platform-Specific Issues**
- Ensure target platform requirements are met
- Check platform-specific permissions
- Review platform deployment guides

### Debug Logging

Enable detailed logging in the system log panel:
- API request/response details
- Streaming event sequence
- Error messages and stack traces
- Performance metrics

## ?? Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow C# coding conventions
- Maintain test coverage above 80%
- Update documentation for new features
- Test on multiple platforms before submitting

## ?? Dependencies

### Core Dependencies

- **LombdaAiAgents** (1.5.0) - AI agent integration library
- **Microsoft.Extensions.Http** (9.0.0) - HTTP client factory
- **Microsoft.Extensions.DependencyInjection** (9.0.0) - Dependency injection
- **System.Text.Json** (9.0.0) - JSON serialization

### Platform Dependencies

- **.NET MAUI** - Cross-platform UI framework
- **Platform-specific SDKs** - iOS, Android, Windows, macOS

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ????? Support

- **Issues**: [GitHub Issues](https://github.com/lombda/LombdaAgentMAUI/issues)
- **Discussions**: [GitHub Discussions](https://github.com/lombda/LombdaAgentMAUI/discussions)
- **Wiki**: [Project Wiki](https://github.com/lombda/LombdaAgentMAUI/wiki)

## ??? Roadmap

- [ ] Voice input/output support
- [ ] Image and file attachment handling
- [ ] Custom agent templates
- [ ] Plugin system for extensions
- [ ] Advanced conversation export
- [ ] Multi-language support
- [ ] Dark/light theme support
- [ ] Offline mode capabilities

---

Built with ?? using .NET MAUI and the LombdaAiAgents library.