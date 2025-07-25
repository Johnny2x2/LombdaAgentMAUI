using System.Collections.ObjectModel;
using LombdaAgentMAUI.Core.Models;
using LombdaAgentMAUI.Core.Services;
using LombdaAgentMAUI.Services;

namespace LombdaAgentMAUI;

public partial class MainPage : ContentPage
{
    private readonly IAgentApiService _agentApiService;
    private readonly IConfigurationService _configService;
    private readonly ISessionManagerService _sessionManager;
    private readonly IFilePickerService _filePickerService;
    private readonly ObservableCollection<ChatMessage> _chatMessages;
    private readonly ObservableCollection<string> _agentList;
    private readonly ObservableCollection<string> _agentTypes;
    private string? _currentAgentId;
    private string? _currentThreadId;
    private string? _currentResponseId; // Track last response ID for API continuity
    private CancellationTokenSource? _streamingCancellationTokenSource;
    
    // File attachment
    private string? _currentFileBase64Data;
    private string? _currentFileName;

    public MainPage(IAgentApiService agentApiService, IConfigurationService configService, 
                   ISessionManagerService sessionManager, IFilePickerService filePickerService)
    {
        InitializeComponent();
        
        _agentApiService = agentApiService;
        _configService = configService;
        _sessionManager = sessionManager;
        _filePickerService = filePickerService;
        _chatMessages = new ObservableCollection<ChatMessage>();
        _agentList = new ObservableCollection<string>();
        _agentTypes = new ObservableCollection<string>();

        // Set up data binding after InitializeComponent
        this.Loaded += OnPageLoaded;
    }

    private async void OnCreateAgentClicked(object? sender, EventArgs e)
    {
        try
        {
            // First, check if we have agent types loaded
            if (_agentTypes.Count == 0)
            {
                LogSystemMessage("No agent types available. Refreshing agent types...");
                await LoadAgentTypesAsync();
                
                if (_agentTypes.Count == 0)
                {
                    await DisplayAlert("Error", "No agent types available. Please check your API connection.", "OK");
                    return;
                }
            }

            // First, show agent type selection
            string selectedAgentType = "Default";
            if (_agentTypes.Count > 1)
            {
                var typeOptions = _agentTypes.ToArray();
                selectedAgentType = await DisplayActionSheet("Select Agent Type", "Cancel", null, typeOptions);
                
                if (selectedAgentType == "Cancel" || string.IsNullOrEmpty(selectedAgentType))
                    return;
            }
            else if (_agentTypes.Count == 1)
            {
                selectedAgentType = _agentTypes.First();
            }

            // Then, get the agent name
            var name = await DisplayPromptAsync("Create Agent", $"Enter name for {selectedAgentType} agent:", "Create", "Cancel", "Assistant");
            if (string.IsNullOrWhiteSpace(name))
                return;

            var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
            if (loadingOverlay != null)
                loadingOverlay.IsVisible = true;

            LogSystemMessage($"Creating agent '{name}' of type '{selectedAgentType}'...");

            var response = await _agentApiService.CreateAgentAsync(name, selectedAgentType);
            if (response != null)
            {
                LogSystemMessage($"Created agent: {response.Name} (ID: {response.Id}, Type: {selectedAgentType})");
                await LoadAgentsAsync();
            }
            else
            {
                LogSystemMessage($"Failed to create agent. Check if agent type '{selectedAgentType}' is valid.");
            }
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Error creating agent: {ex.Message}");
        }
        finally
        {
            var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
            if (loadingOverlay != null)
                loadingOverlay.IsVisible = false;
        }
    }

    
    private async void OnSendClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentAgentId))
        {
            await DisplayAlert("Error", "Please select an agent first.", "OK");
            return;
        }

        var messageEditor = this.FindByName<Editor>("MessageEditor");
        var message = messageEditor?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            await DisplayAlert("Error", "Please enter a message.", "OK");
            return;
        }

        try
        {
            var userMessage = new ChatMessage
            {
                Text = message,
                IsUser = true,
                Timestamp = DateTime.Now
            };
            _chatMessages.Add(userMessage);
            
            // Save session after adding user message
            await SaveCurrentSessionAsync();
            
            if (messageEditor != null)
                messageEditor.Text = string.Empty;

            var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
            if (chatCollectionView != null && _chatMessages.Count > 0)
            {
                chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
            }

            var sendButton = this.FindByName<Button>("SendButton");
            if (sendButton != null)
            {
                sendButton.IsEnabled = false;
                sendButton.Text = "Sending...";
            }

            var streamingCheckBox = this.FindByName<CheckBox>("StreamingCheckBox");
            bool hasAttachment = !string.IsNullOrEmpty(_currentFileBase64Data);
            
            if (hasAttachment)
            {
                LogSystemMessage($"Sending message with file attachment: {_currentFileName}");
            }
            
            if (streamingCheckBox?.IsChecked == true)
            {
                if (hasAttachment)
                {
                    await SendStreamingMessageWithFile(message, _currentFileBase64Data);
                }
                else
                {
                    await SendStreamingMessage(message);
                }
            }
            else
            {
                if (hasAttachment)
                {
                    await SendRegularMessageWithFile(message, _currentFileBase64Data);
                }
                else
                {
                    await SendRegularMessage(message);
                }
            }
            
            // Clear the file attachment after sending
            ClearFileAttachment();
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Error sending message: {ex.Message}");
            await DisplayAlert("Error", $"Failed to send message: {ex.Message}", "OK");
        }
        finally
        {
            var sendButton = this.FindByName<Button>("SendButton");
            if (sendButton != null)
            {
                sendButton.IsEnabled = true;
                sendButton.Text = "Send";
            }
        }
    }
    
    private async Task SendRegularMessageWithFile(string message, string? fileBase64Data)
    {
        LogSystemMessage($"Sending message with file to agent {_currentAgentId}...");

        var response = await _agentApiService.SendMessageWithFileAsync(_currentAgentId!, message, fileBase64Data!, _currentThreadId);
        if (response != null)
        {
            LogSystemMessage($"Response received - ThreadId: {response.ThreadId}");
            LogSystemMessage($"Response text length: {response.Text?.Length ?? 0}");

            _currentThreadId = response.ThreadId;

            var agentMessage = new ChatMessage
            {
                Text = response.Text ?? "[No response text received]",
                IsUser = false,
                Timestamp = DateTime.Now
            };

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _chatMessages.Add(agentMessage);
                LogSystemMessage($"Added agent message to chat. Total messages: {_chatMessages.Count}");
                
                var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                if (chatCollectionView != null && _chatMessages.Count > 0)
                {
                    chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
                }
            });

            // Save session after receiving response
            await SaveCurrentSessionAsync();

            LogSystemMessage("Response processing completed.");
        }
        else
        {
            LogSystemMessage("Failed to get response from agent - response was null.");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var errorMessage = new ChatMessage
                {
                    Text = "❌ Failed to get response from agent. Please try again.",
                    IsUser = false,
                    Timestamp = DateTime.Now
                };
                _chatMessages.Add(errorMessage);
            });
            
            // Save session even with error message
            await SaveCurrentSessionAsync();
        }
    }

    private async Task SendRegularMessage(string message)
    {
        LogSystemMessage($"Sending message to agent {_currentAgentId}...");

        var response = await _agentApiService.SendMessageAsync(_currentAgentId!, message, _currentThreadId);
        if (response != null)
        {
            LogSystemMessage($"Response received - ThreadId: {response.ThreadId}");
            LogSystemMessage($"Response text length: {response.Text?.Length ?? 0}");

            _currentThreadId = response.ThreadId;
            // Note: Regular messages don't typically return response IDs like streaming API

            var agentMessage = new ChatMessage
            {
                Text = response.Text ?? "[No response text received]",
                IsUser = false,
                Timestamp = DateTime.Now
            };

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _chatMessages.Add(agentMessage);
                LogSystemMessage($"Added agent message to chat. Total messages: {_chatMessages.Count}");
                
                var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                if (chatCollectionView != null && _chatMessages.Count > 0)
                {
                    chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
                }
            });

            // Save session after receiving response
            await SaveCurrentSessionAsync();

            LogSystemMessage("Response processing completed.");
        }
        else
        {
            LogSystemMessage("Failed to get response from agent - response was null.");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var errorMessage = new ChatMessage
                {
                    Text = "❌ Failed to get response from agent. Please try again.",
                    IsUser = false,
                    Timestamp = DateTime.Now
                };
                _chatMessages.Add(errorMessage);
            });
            
            // Save session even with error message
            await SaveCurrentSessionAsync();
        }
    }
    

   
}