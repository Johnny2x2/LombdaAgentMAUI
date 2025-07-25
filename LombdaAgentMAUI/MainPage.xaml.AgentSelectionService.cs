using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LombdaAgentMAUI
{
    public partial class MainPage
    {

        private void OnAgentSelected(object? sender, EventArgs e)
        {
            var agentPicker = this.FindByName<Picker>("AgentPicker");
            if (agentPicker?.SelectedItem is string selectedAgentId)
            {
                SelectAgent(selectedAgentId);

                // Sync the list view selection
                var agentListView = this.FindByName<CollectionView>("AgentListView");
                if (agentListView != null)
                {
                    var index = _agentList.IndexOf(selectedAgentId);
                    if (index >= 0)
                    {
                        agentListView.SelectedItem = selectedAgentId;
                    }
                }
            }
        }

        // Event handler for when an agent is selected from the agent list
        private void OnAgentListSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection?.FirstOrDefault() is string selectedAgentId)
            {
                SelectAgent(selectedAgentId);

                // Sync the picker selection
                var agentPicker = this.FindByName<Picker>("AgentPicker");
                if (agentPicker != null)
                {
                    agentPicker.SelectedItem = selectedAgentId;
                }
            }
        }

        // Common method to handle agent selection from both controls
        private async void SelectAgent(string agentId)
        {
            try
            {
                // Save current session before switching if there's an active agent
                if (!string.IsNullOrEmpty(_currentAgentId))
                {
                    await SaveCurrentSessionAsync();
                }

                _currentAgentId = agentId;

                // Update the current agent label
                var currentAgentLabel = this.FindByName<Label>("CurrentAgentLabel");
                if (currentAgentLabel != null)
                {
                    currentAgentLabel.Text = "Loading agent details...";
                }

                LogSystemMessage($"Selected agent: {agentId}");

                // Load session for the selected agent
                await LoadAgentSessionAsync(agentId);

                // Save this as the last selected agent
                await _sessionManager.SaveLastSelectedAgentIdAsync(agentId);

                // Try to fetch agent details for better display
                try
                {
                    var agentDetails = await _agentApiService.GetAgentAsync(agentId);
                    if (agentDetails != null && currentAgentLabel != null)
                    {
                        currentAgentLabel.Text = $"{agentDetails.Name} (ID: {agentId})";
                        LogSystemMessage($"Loaded agent details: {agentDetails.Name}");
                    }
                    else if (currentAgentLabel != null)
                    {
                        currentAgentLabel.Text = $"Agent ID: {agentId}";
                    }
                }
                catch (Exception ex)
                {
                    LogSystemMessage($"Could not load agent details: {ex.Message}");
                    if (currentAgentLabel != null)
                    {
                        currentAgentLabel.Text = $"Agent ID: {agentId}";
                    }
                }
            }
            catch (Exception ex)
            {
                LogSystemMessage($"Error selecting agent: {ex.Message}");
            }
        }
    }
}
