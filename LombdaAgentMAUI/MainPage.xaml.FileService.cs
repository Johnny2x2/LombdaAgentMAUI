using LombdaAgentMAUI.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LombdaAgentMAUI
{
    public partial class MainPage
    {
        private async void OnAttachFileClicked(object sender, EventArgs e)
        {
            try
            {
                LogSystemMessage("Opening file attachment dialog...");

                // Create and show the file upload dialog
                //var fileDialog = new FileUploadDialog(_filePickerService);
                //bool result = await fileDialog.ShowDialogAsync();

                var fileAttachment = await _filePickerService.PickAndProcessSingleFileAsync(FilePickerFileType.Images);

                if (fileAttachment != null)
                {
                    Debug.WriteLine($"File picked: {fileAttachment.FileName}");
                }

                // Check if a file was selected and processed
                if (!string.IsNullOrEmpty(fileAttachment.DataUri))
                {
                    _currentFileBase64Data = fileAttachment.DataUri;
                    _currentFileName = fileAttachment.FileName ?? "attachment";

                    // Update the UI to show the attached file indicator
                    FileAttachmentIndicator.IsVisible = true;
                    FileNameLabel.Text = _currentFileName;

                    LogSystemMessage($"File attached successfully: {_currentFileName}");

                    // Visual feedback (optional)
                    AttachFileButton.BackgroundColor = Colors.LightGreen;
                    await Task.Delay(500);
                    AttachFileButton.BackgroundColor = null;
                }
                else
                {
                    LogSystemMessage("File attachment dialog returned but no file was selected");
                }
            }
            catch (Exception ex)
            {
                LogSystemMessage($"Error attaching file: {ex.Message}");

                // Show error to user
                await DisplayAlert("File Attachment Error",
                    "There was a problem attaching the file. Please try again or select a different file.",
                    "OK");
            }
        }

        private void OnClearAttachmentClicked(object sender, EventArgs e)
        {
            ClearFileAttachment();
        }

        private void ClearFileAttachment()
        {
            _currentFileBase64Data = null;
            _currentFileName = null;
            FileAttachmentIndicator.IsVisible = false;
            LogSystemMessage("File attachment cleared");
        }
    }
}
