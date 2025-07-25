using LombdaAgentMAUI.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LombdaAgentMAUI
{
    public partial class MainPage
    {

        private void OnClearClicked(object? sender, EventArgs e)
        {
            _chatMessages.Clear();
            _currentThreadId = null;
            _currentResponseId = null;
            ClearFileAttachment();
            LogSystemMessage("Chat cleared. New conversation will start with next message.");

            // Save the cleared session
            if (!string.IsNullOrEmpty(_currentAgentId))
            {
                Task.Run(async () => await SaveCurrentSessionAsync());
            }
        }
        private async Task SaveCurrentSessionAsync()
        {
            if (string.IsNullOrEmpty(_currentAgentId))
                return;

            try
            {
                var session = new AgentSession
                {
                    AgentId = _currentAgentId,
                    ThreadId = _currentThreadId,
                    LastResponseId = _currentResponseId,
                    Messages = _chatMessages.ToList(),
                    LastActivity = DateTime.Now
                };

                // Try to get agent name for better session display
                try
                {
                    var agentDetails = await _agentApiService.GetAgentAsync(_currentAgentId);
                    if (agentDetails != null)
                    {
                        session.AgentName = agentDetails.Name;
                    }
                }
                catch
                {
                    // If we can't get agent details, just use the ID
                    session.AgentName = _currentAgentId;
                }

                await _sessionManager.SaveSessionAsync(session);

                // Update Clear Session button visibility
                var clearSessionButton = this.FindByName<Button>("ClearSessionButton");
                if (clearSessionButton != null)
                {
                    clearSessionButton.IsVisible = _chatMessages.Count > 0;
                }

                LogSystemMessage($"Saved session for agent {_currentAgentId} ({_chatMessages.Count} messages)");
            }
            catch (Exception ex)
            {
                LogSystemMessage($"Error saving session: {ex.Message}");
            }
        }

        /// <summary>
        /// Load the chat session for the specified agent
        /// </summary>
        private async Task LoadAgentSessionAsync(string agentId)
        {
            try
            {
                var session = await _sessionManager.GetSessionAsync(agentId);
                var clearSessionButton = this.FindByName<Button>("ClearSessionButton");

                // Clear current messages
                _chatMessages.Clear();

                if (session != null && session.Messages.Count > 0)
                {
                    // Restore session data
                    _currentThreadId = session.ThreadId;
                    _currentResponseId = session.LastResponseId;

                    // Restore chat messages
                    foreach (var message in session.Messages)
                    {
                        _chatMessages.Add(message);
                    }

                    // Show clear session button since there's history
                    if (clearSessionButton != null)
                    {
                        clearSessionButton.IsVisible = true;
                    }

                    LogSystemMessage($"Restored session for agent {agentId}: {session.Messages.Count} messages, ThreadId: {session.ThreadId}");

                    // Scroll to the bottom to show the latest message
                    if (_chatMessages.Count > 0)
                    {
                        var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                        if (chatCollectionView != null)
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await Task.Delay(100); // Small delay to ensure UI is updated
                                chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: false);
                            });
                        }
                    }
                }
                else
                {
                    // No existing session, start fresh
                    _currentThreadId = null;
                    _currentResponseId = null;

                    // Hide clear session button since there's no history
                    if (clearSessionButton != null)
                    {
                        clearSessionButton.IsVisible = false;
                    }

                    LogSystemMessage($"Starting new session for agent {agentId}");
                }
            }
            catch (Exception ex)
            {
                LogSystemMessage($"Error loading session for agent {agentId}: {ex.Message}");
                // On error, start with a clean session
                _chatMessages.Clear();
                _currentThreadId = null;
                _currentResponseId = null;

                // Hide clear session button on error
                var clearSessionButton = this.FindByName<Button>("ClearSessionButton");
                if (clearSessionButton != null)
                {
                    clearSessionButton.IsVisible = false;
                }
            }
        }

        /// <summary>
        /// Restore the last selected agent and its session
        /// </summary>
        private async Task RestoreLastSelectedAgentAsync()
        {
            try
            {
                var lastAgentId = await _sessionManager.GetLastSelectedAgentIdAsync();
                if (!string.IsNullOrEmpty(lastAgentId) && _agentList.Contains(lastAgentId))
                {
                    LogSystemMessage($"Restoring last selected agent: {lastAgentId}");

                    // Update UI controls to show the selected agent
                    var agentPicker = this.FindByName<Picker>("AgentPicker");
                    var agentListView = this.FindByName<CollectionView>("AgentListView");

                    if (agentPicker != null)
                    {
                        agentPicker.SelectedItem = lastAgentId;
                    }

                    if (agentListView != null)
                    {
                        agentListView.SelectedItem = lastAgentId;
                    }

                    // Load the agent session without triggering the selection events
                    await LoadAgentSessionAsync(lastAgentId);
                    _currentAgentId = lastAgentId;

                    // Update agent label
                    var currentAgentLabel = this.FindByName<Label>("CurrentAgentLabel");
                    if (currentAgentLabel != null)
                    {
                        try
                        {
                            var agentDetails = await _agentApiService.GetAgentAsync(lastAgentId);
                            if (agentDetails != null)
                            {
                                currentAgentLabel.Text = $"{agentDetails.Name} (ID: {lastAgentId})";
                            }
                            else
                            {
                                currentAgentLabel.Text = $"Agent ID: {lastAgentId}";
                            }
                        }
                        catch
                        {
                            currentAgentLabel.Text = $"Agent ID: {lastAgentId}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogSystemMessage($"Error restoring last selected agent: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the page is appearing - good place to save session
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        /// <summary>
        /// Called when the page is disappearing - save current session
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Save current session when page is disappearing
            if (!string.IsNullOrEmpty(_currentAgentId))
            {
                Task.Run(async () => await SaveCurrentSessionAsync());
            }
        }


        private async void OnClearSessionClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentAgentId))
                return;

            try
            {
                var result = await DisplayAlert("Clear Session",
                    $"This will permanently delete the chat history for this agent. Are you sure?",
                    "Yes, Clear", "Cancel");

                if (result)
                {
                    // Clear the session from storage
                    await _sessionManager.ClearSessionAsync(_currentAgentId);

                    // Clear current chat display
                    _chatMessages.Clear();
                    _currentThreadId = null;
                    _currentResponseId = null;
                    ClearFileAttachment();

                    LogSystemMessage($"Session cleared for agent {_currentAgentId}");
                }
            }
            catch (Exception ex)
            {
                LogSystemMessage($"Error clearing session: {ex.Message}");
            }
        }
    }
}
