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
        private async Task SendStreamingMessageWithFile(string message, string? fileBase64Data)
        {
            LogSystemMessage($"🚀 Starting streaming message with file to agent {_currentAgentId}...");

            var agentMessage = new ChatMessage
            {
                Text = "🤖 Initializing...",
                IsUser = false,
                Timestamp = DateTime.Now
            };

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _chatMessages.Add(agentMessage);
                LogSystemMessage("✅ Added placeholder message to chat");
            });

            _streamingCancellationTokenSource?.Cancel();
            _streamingCancellationTokenSource = new CancellationTokenSource();
            _streamingCancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(5));

            var streamedContent = "";
            var hasReceivedContent = false;
            var eventCount = 0;
            var updateCount = 0;
            var startTime = DateTime.Now;

            var messageLock = new object();
            var hasReceivedFirstDelta = false;

            // Run the streaming operation on a background task to prevent UI blocking
            try
            {
                LogSystemMessage("🔄 Starting streaming request with file...");

                // Use Task.Run to execute streaming on background thread
                await Task.Run(async () =>
                {
                    try
                    {
                        var resultThreadId = await _agentApiService.SendMessageStreamWithFileAsync(
                            _currentAgentId!,
                            message,
                            fileBase64Data!,
                            _currentThreadId,
                            // Text callback - runs on background thread
                            (streamedText) =>
                            {
                                lock (messageLock)
                                {
                                    hasReceivedContent = true;
                                    streamedContent += streamedText;
                                    updateCount++;
                                    hasReceivedFirstDelta = true;
                                }

                                // Only log every 10th chunk to reduce log spam
                                if (updateCount % 10 == 0)
                                {
                                    LogSystemMessage($"📥 Received {updateCount} text chunks (total: {streamedContent.Length} chars)");
                                }

                                // Capture content for UI update
                                string currentContent;
                                lock (messageLock)
                                {
                                    currentContent = streamedContent;
                                }

                                Dispatcher.DispatchAsync(async () =>
                                {
                                    try
                                    {
                                        if (_chatMessages.Contains(agentMessage))
                                        {
                                            agentMessage.Text = currentContent;

                                            // CRITICAL: Yield control to let the GUI framework work
                                            await Task.Yield();
                                        }
                                        else
                                        {
                                            LogSystemMessage("⚠️ Agent message no longer in collection");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogSystemMessage($"❌ Error updating UI: {ex.Message}");
                                    }
                                });
                            },
                            // Event callback - runs on background thread
                            (eventData) =>
                            {
                                eventCount++;
                                var elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;

                                // Track response ID from streaming events
                                if (eventData.EventType == "created" && !string.IsNullOrEmpty(eventData.ResponseId))
                                {
                                    _currentResponseId = eventData.ResponseId;
                                    LogSystemMessage($"📝 Stream created (ID: {eventData.ResponseId})");
                                }

                                // Only log important events to reduce clutter
                                switch (eventData.EventType)
                                {
                                    case "connected":
                                        LogSystemMessage("✅ Connected to streaming endpoint");
                                        break;

                                    case "created":
                                        // Already logged above when tracking response ID
                                        break;

                                    case "complete":
                                        LogSystemMessage($"🏁 Response complete (Thread: {eventData.ThreadId})");
                                        break;

                                    case "error":
                                    case "stream_error":
                                        LogSystemMessage($"❌ Error: {eventData.Error}");
                                        break;

                                    case "reasoning":
                                        LogSystemMessage($"🧠 Reasoning step received");
                                        break;

                                    // Skip logging delta events as they're too verbose
                                    case "delta":
                                        break;

                                    default:
                                        LogSystemMessage($"ℹ️ Event: {eventData.EventType}");
                                        break;
                                }

                                // Queue UI update without blocking the streaming thread
                                Dispatcher.DispatchAsync(async () =>
                                {
                                    try
                                    {
                                        if (!_chatMessages.Contains(agentMessage))
                                        {
                                            return;
                                        }

                                        bool shouldUpdateFromEvent;
                                        lock (messageLock)
                                        {
                                            shouldUpdateFromEvent = !hasReceivedFirstDelta;
                                        }

                                        switch (eventData.EventType)
                                        {
                                            case "connected":
                                                if (shouldUpdateFromEvent)
                                                {
                                                    agentMessage.Text = "🔗 Connected, waiting for response...";
                                                }
                                                break;

                                            case "created":
                                                if (shouldUpdateFromEvent)
                                                {
                                                    agentMessage.Text = "⚡ Processing your request with file...";
                                                }
                                                break;

                                            case "complete":
                                                if (!string.IsNullOrEmpty(eventData.ThreadId))
                                                {
                                                    _currentThreadId = eventData.ThreadId;
                                                }
                                                break;

                                            case "error":
                                            case "stream_error":
                                                agentMessage.Text = $"❌ Error: {eventData.Error}";
                                                break;
                                        }

                                        // CRITICAL: Yield control to let the GUI framework work
                                        await Task.Yield();
                                    }
                                    catch (Exception ex)
                                    {
                                        LogSystemMessage($"❌ Error in event handler: {ex.Message}");
                                    }
                                });
                            },
                            _streamingCancellationTokenSource.Token
                        );

                        LogSystemMessage("✅ Streaming request completed");

                        // Final updates on UI thread
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            string finalContent;
                            lock (messageLock)
                            {
                                finalContent = streamedContent;
                            }

                            if (!string.IsNullOrEmpty(finalContent))
                            {
                                if (_chatMessages.Contains(agentMessage))
                                {
                                    agentMessage.Text = finalContent;
                                    LogSystemMessage($"✅ Response complete ({finalContent.Length} characters)");
                                }
                            }

                            var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                            if (chatCollectionView != null && _chatMessages.Count > 0)
                            {
                                chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
                            }
                        });

                        if (!string.IsNullOrEmpty(resultThreadId))
                        {
                            _currentThreadId = resultThreadId;
                            LogSystemMessage($"✅ Thread ID: {resultThreadId}");
                        }

                        var totalTime = (DateTime.Now - startTime).TotalSeconds;
                        LogSystemMessage($"📊 Streaming completed in {totalTime:0.1}s");

                        if (!hasReceivedContent)
                        {
                            LogSystemMessage("⚠️ No streaming content received - check API connection");
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                if (_chatMessages.Contains(agentMessage))
                                {
                                    agentMessage.Text = "❌ No response received. Please try again.";
                                }
                            });
                        }

                        // Save session after streaming completes
                        await SaveCurrentSessionAsync();
                    }
                    catch (Exception streamEx)
                    {
                        LogSystemMessage($"❌ Error during streaming: {streamEx.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                LogSystemMessage($"❌ Error in streaming logic: {ex.Message}");
            }
            finally
            {
                // Final cancellation and cleanup
                _streamingCancellationTokenSource?.Cancel();
                _streamingCancellationTokenSource = null;

                LogSystemMessage("🔚 Streaming process ended");
            }
        }
        private async Task SendStreamingMessage(string message)
        {
            LogSystemMessage($"🚀 Starting streaming message to agent {_currentAgentId}...");

            var agentMessage = new ChatMessage
            {
                Text = "🤖 Initializing...",
                IsUser = false,
                Timestamp = DateTime.Now
            };

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _chatMessages.Add(agentMessage);
                LogSystemMessage("✅ Added placeholder message to chat");
            });

            _streamingCancellationTokenSource?.Cancel();
            _streamingCancellationTokenSource = new CancellationTokenSource();
            _streamingCancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(5));

            var streamedContent = "";
            var hasReceivedContent = false;
            var eventCount = 0;
            var updateCount = 0;
            var startTime = DateTime.Now;

            var messageLock = new object();
            var hasReceivedFirstDelta = false;

            // Run the streaming operation on a background task to prevent UI blocking
            try
            {
                LogSystemMessage("🔄 Starting streaming request...");

                // Use Task.Run to execute streaming on background thread
                await Task.Run(async () =>
                {
                    try
                    {
                        var resultThreadId = await _agentApiService.SendMessageStreamWithEventsAsync(
                            _currentAgentId!,
                            message,
                            _currentThreadId,
                            // Text callback - runs on background thread
                            (streamedText) =>
                            {
                                lock (messageLock)
                                {
                                    hasReceivedContent = true;
                                    streamedContent += streamedText;
                                    updateCount++;
                                    hasReceivedFirstDelta = true;
                                }

                                // Only log every 10th chunk to reduce log spam
                                if (updateCount % 10 == 0)
                                {
                                    LogSystemMessage($"📥 Received {updateCount} text chunks (total: {streamedContent.Length} chars)");
                                }

                                // Capture content for UI update
                                string currentContent;
                                lock (messageLock)
                                {
                                    currentContent = streamedContent;
                                }

                                Dispatcher.DispatchAsync(async () =>
                                {
                                    try
                                    {
                                        if (_chatMessages.Contains(agentMessage))
                                        {
                                            agentMessage.Text = currentContent;

                                            // CRITICAL: Yield control to let the GUI framework work
                                            await Task.Yield();
                                        }
                                        else
                                        {
                                            LogSystemMessage("⚠️ Agent message no longer in collection");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogSystemMessage($"❌ Error updating UI: {ex.Message}");
                                    }
                                });
                            },
                            // Event callback - runs on background thread
                            (eventData) =>
                            {
                                eventCount++;
                                var elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;

                                // Track response ID from streaming events
                                if (eventData.EventType == "created" && !string.IsNullOrEmpty(eventData.ResponseId))
                                {
                                    _currentResponseId = eventData.ResponseId;
                                    LogSystemMessage($"📝 Stream created (ID: {eventData.ResponseId})");
                                }

                                // Only log important events to reduce clutter
                                switch (eventData.EventType)
                                {
                                    case "connected":
                                        LogSystemMessage("✅ Connected to streaming endpoint");
                                        break;

                                    case "created":
                                        // Already logged above when tracking response ID
                                        break;

                                    case "complete":
                                        LogSystemMessage($"🏁 Response complete (Thread: {eventData.ThreadId})");
                                        break;

                                    case "error":
                                    case "stream_error":
                                        LogSystemMessage($"❌ Error: {eventData.Error}");
                                        break;

                                    case "reasoning":
                                        LogSystemMessage($"🧠 Reasoning step received");
                                        break;

                                    // Skip logging delta events as they're too verbose
                                    case "delta":
                                        break;

                                    default:
                                        LogSystemMessage($"ℹ️ Event: {eventData.EventType}");
                                        break;
                                }

                                // Queue UI update without blocking the streaming thread
                                Dispatcher.DispatchAsync(async () =>
                                {
                                    try
                                    {
                                        if (!_chatMessages.Contains(agentMessage))
                                        {
                                            return;
                                        }

                                        bool shouldUpdateFromEvent;
                                        lock (messageLock)
                                        {
                                            shouldUpdateFromEvent = !hasReceivedFirstDelta;
                                        }

                                        switch (eventData.EventType)
                                        {
                                            case "connected":
                                                if (shouldUpdateFromEvent)
                                                {
                                                    agentMessage.Text = "🔗 Connected, waiting for response...";
                                                }
                                                break;

                                            case "created":
                                                if (shouldUpdateFromEvent)
                                                {
                                                    agentMessage.Text = "⚡ Processing your request...";
                                                }
                                                break;

                                            case "complete":
                                                if (!string.IsNullOrEmpty(eventData.ThreadId))
                                                {
                                                    _currentThreadId = eventData.ThreadId;
                                                }
                                                break;

                                            case "error":
                                            case "stream_error":
                                                agentMessage.Text = $"❌ Error: {eventData.Error}";
                                                break;
                                        }

                                        // CRITICAL: Yield control to let the GUI framework work
                                        await Task.Yield();
                                    }
                                    catch (Exception ex)
                                    {
                                        LogSystemMessage($"❌ Error in event handler: {ex.Message}");
                                    }
                                });
                            },
                            _streamingCancellationTokenSource.Token
                        );

                        LogSystemMessage("✅ Streaming request completed");

                        // Final updates on UI thread
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            string finalContent;
                            lock (messageLock)
                            {
                                finalContent = streamedContent;
                            }

                            if (!string.IsNullOrEmpty(finalContent))
                            {
                                if (_chatMessages.Contains(agentMessage))
                                {
                                    agentMessage.Text = finalContent;
                                    LogSystemMessage($"✅ Response complete ({finalContent.Length} characters)");
                                }
                            }

                            var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                            if (chatCollectionView != null && _chatMessages.Count > 0)
                            {
                                chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
                            }
                        });

                        if (!string.IsNullOrEmpty(resultThreadId))
                        {
                            _currentThreadId = resultThreadId;
                            LogSystemMessage($"✅ Thread ID: {resultThreadId}");
                        }

                        var totalTime = (DateTime.Now - startTime).TotalSeconds;
                        LogSystemMessage($"📊 Streaming completed in {totalTime:0.1}s");

                        if (!hasReceivedContent)
                        {
                            LogSystemMessage("⚠️ No streaming content received - check API connection");
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                if (_chatMessages.Contains(agentMessage))
                                {
                                    agentMessage.Text = "❌ No response received. Please try again.";
                                }
                            });
                        }

                        // Save session after streaming completes
                        await SaveCurrentSessionAsync();
                    }
                    catch (Exception streamEx)
                    {
                        LogSystemMessage($"❌ Error during streaming: {streamEx.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                LogSystemMessage($"❌ Error in streaming logic: {ex.Message}");
            }
            finally
            {
                // Final cancellation and cleanup
                _streamingCancellationTokenSource?.Cancel();
                _streamingCancellationTokenSource = null;

                LogSystemMessage("🔚 Streaming process ended");
            }
        }
    }
}
