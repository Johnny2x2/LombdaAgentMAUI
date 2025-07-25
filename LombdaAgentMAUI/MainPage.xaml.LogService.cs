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
        private void LogSystemMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";

            System.Diagnostics.Debug.WriteLine($"[T{Environment.CurrentManagedThreadId}] {logMessage}");

            if (MainThread.IsMainThread)
            {
                UpdateLogUI(logMessage);
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => UpdateLogUI(logMessage));
            }
        }

        private void UpdateLogUI(string logMessage)
        {
            try
            {
                var systemLogLabel = this.FindByName<Label>("SystemLogLabel");
                if (systemLogLabel != null)
                {
                    if (systemLogLabel.Text.Length > 50000)
                    {
                        var lines = systemLogLabel.Text.Split(Environment.NewLine).Skip(100).ToList();
                        systemLogLabel.Text = string.Join(Environment.NewLine, lines) + Environment.NewLine + "[...truncated...]" + Environment.NewLine;
                    }

                    systemLogLabel.Text += logMessage + Environment.NewLine;

                    var logScrollView = this.FindByName<ScrollView>("LogScrollView");
                    if (logScrollView != null)
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    await logScrollView.ScrollToAsync(0, systemLogLabel.Height, false);
                                });
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error scrolling log: {ex.Message}");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating log UI: {ex.Message}");
            }
        }
    }
}
