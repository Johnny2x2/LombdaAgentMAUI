using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LombdaAgentMAUI
{
    public partial class MainPage
    {
        private async void OnRefreshClicked(object? sender, EventArgs e)
        {
            await LoadAgentsAsync();
            await LoadAgentTypesAsync();
        }

        private async void OnPageLoaded(object? sender, EventArgs e)
        {
            try
            {
                // Load configuration first
                await _configService.LoadSettingsAsync();

                // Find controls and set up bindings
                var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                var agentListView = this.FindByName<CollectionView>("AgentListView");

                if (chatCollectionView != null)
                    chatCollectionView.ItemsSource = _chatMessages;

                if (agentListView != null)
                    agentListView.ItemsSource = _agentList;

                await LoadAgentsAsync();
                await LoadAgentTypesAsync();

                // Try to restore the last selected agent after loading agents
                await RestoreLastSelectedAgentAsync();

                LogSystemMessage("Application started. Please select or create an agent.");

                // Add welcome message with setup instructions if no agents found
                if (_agentList.Count == 0)
                {
                    LogSystemMessage("No agents found. Please check your API configuration in Settings.");
                }
            }
            catch (Exception ex)
            {
                LogSystemMessage($"Error during page load: {ex.Message}");
            }
        }

        private async Task LoadAgentsAsync()
        {
            try
            {
                var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                if (loadingOverlay != null)
                    loadingOverlay.IsVisible = true;

                LogSystemMessage("Loading agents...");

                var agents = await _agentApiService.GetAgentsAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _agentList.Clear();
                    var agentPicker = this.FindByName<Picker>("AgentPicker");
                    if (agentPicker != null)
                    {
                        agentPicker.ItemsSource = null;
                        agentPicker.ItemsSource = agents;
                    }

                    foreach (var agent in agents)
                    {
                        _agentList.Add(agent);
                    }

                    LogSystemMessage($"Loaded {agents.Count} agents.");
                });
            }
            catch (Exception ex)
            {
                LogSystemMessage($"Error loading agents: {ex.Message}");
            }
            finally
            {
                var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                if (loadingOverlay != null)
                    loadingOverlay.IsVisible = false;
            }
        }

        private async Task LoadAgentTypesAsync()
        {
            try
            {
                LogSystemMessage("Loading agent types...");

                var agentTypes = await _agentApiService.GetAgentTypesAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _agentTypes.Clear();
                    foreach (var agentType in agentTypes)
                    {
                        _agentTypes.Add(agentType);
                    }

                    LogSystemMessage($"Loaded {agentTypes.Count} agent types: {string.Join(", ", agentTypes)}");
                });
            }
            catch (Exception ex)
            {
                LogSystemMessage($"Error loading agent types: {ex.Message}");
            }
        }
    }
}
