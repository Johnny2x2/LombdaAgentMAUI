using LombdaAgentMAUI.Core.Models;
using LombdaAgentMAUI.Core.Utilities;
using LombdaAgentMAUI.Services;
using System.Diagnostics;

namespace LombdaAgentMAUI;

public partial class FileUploadDialog : ContentPage
{
    private readonly IFilePickerService _filePickerService;
    private FileAttachment? _currentFileAttachment;
    private string? _base64Data;
    private TaskCompletionSource<bool>? _dialogTcs;

    public string? FileBase64Data => _base64Data;
    public FileAttachment? FileAttachment => _currentFileAttachment;
    public bool HasFile => _currentFileAttachment != null;

    public FileUploadDialog(IFilePickerService filePickerService)
    {
        Debug.WriteLine("==== FileUploadDialog constructor start ====");
        try
        {
            InitializeComponent();
            _filePickerService = filePickerService ?? throw new ArgumentNullException(nameof(filePickerService));
            
            // Ensure button texts are correct
            CopyBase64Button.Text = "Use File";
            SelectFileButton.Text = "Browse for File";
            
            Debug.WriteLine($"FilePickerService instance: {_filePickerService.GetType().FullName}");
            Debug.WriteLine("FileUploadDialog initialized successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in FileUploadDialog constructor: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        Debug.WriteLine("==== FileUploadDialog constructor end ====");
    }

    protected override void OnAppearing()
    {
        Debug.WriteLine("FileUploadDialog.OnAppearing called");
        base.OnAppearing();
        
        // Ensure UI is reset when dialog appears
        try
        {
            ResetUI();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
        }
    }

    // Override back button behavior
    protected override bool OnBackButtonPressed()
    {
        Debug.WriteLine("Back button pressed in FileUploadDialog");
        // Close dialog with false result - use Task.Run to avoid deadlock
        Task.Run(async () => await CloseDialog(false));
        return true; // We've handled the back button
    }

    private async void OnSelectFileClicked(object sender, EventArgs e)
    {
        Debug.WriteLine("==== OnSelectFileClicked start ====");
        try
        {
            Debug.WriteLine("File selection started");
            StatusLabel.Text = "Selecting file...";
            LoadingOverlay.IsVisible = true;

            // Pick a file
            Debug.WriteLine($"FilePickerService instance before call: {_filePickerService.GetType().FullName}");
            _currentFileAttachment = await _filePickerService.PickAndProcessSingleFileAsync();
            
            if (_currentFileAttachment == null)
            {
                Debug.WriteLine("No file was selected or processing failed");
                StatusLabel.Text = "No file selected or file selection was canceled";
                LoadingOverlay.IsVisible = false;
                return;
            }

            Debug.WriteLine($"File selected: {_currentFileAttachment.FileName}, type: {_currentFileAttachment.MediaType}");

            // Update UI with file details
            FileNameLabel.Text = _currentFileAttachment.FileName;
            MediaTypeLabel.Text = _currentFileAttachment.MediaType;
            
            // Extract base64 data
            _base64Data = _currentFileAttachment.DataUri;

            // Show a preview of base64 data (truncated)
            if (!string.IsNullOrEmpty(_base64Data))
            {
                Debug.WriteLine($"Base64 data length: {_base64Data.Length}");
                var previewLength = Math.Min(_base64Data.Length, 500);
                Base64PreviewLabel.Text = _base64Data.Substring(0, previewLength) + 
                    (_base64Data.Length > previewLength ? "..." : "");
                Base64LengthLabel.Text = $"Total length: {_base64Data.Length} characters";
                
                // Calculate file size
                int commaIndex = _base64Data.IndexOf(',');
                if (commaIndex > 0 && commaIndex < _base64Data.Length - 1)
                {
                    string base64Content = _base64Data.Substring(commaIndex + 1);
                    int fileSize = (int)Math.Ceiling(base64Content.Length * 3 / 4.0); // Approximate size in bytes
                    
                    // Format file size
                    string formattedSize;
                    if (fileSize < 1024)
                        formattedSize = $"{fileSize} bytes";
                    else if (fileSize < 1024 * 1024)
                        formattedSize = $"{fileSize / 1024.0:F1} KB";
                    else
                        formattedSize = $"{fileSize / (1024.0 * 1024):F1} MB";
                    
                    FileSizeLabel.Text = formattedSize;
                    Debug.WriteLine($"File size: {formattedSize}");
                }
                else
                {
                    Debug.WriteLine("Warning: Base64 data URI format may be incorrect");
                    FileSizeLabel.Text = "Unknown size";
                }
            }
            else
            {
                Debug.WriteLine("Warning: Base64 data is null or empty");
                Base64PreviewLabel.Text = "No data available";
                Base64LengthLabel.Text = "";
            }

            // For images, show preview
            PreviewContainer.IsVisible = false;
            if (_currentFileAttachment.MediaType.StartsWith("image/"))
            {
                try
                {
                    Debug.WriteLine("Attempting to show image preview");
                    PreviewImage.Source = _currentFileAttachment.DataUri;
                    PreviewContainer.IsVisible = true;
                    Debug.WriteLine("Image preview loaded successfully");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading image preview: {ex.Message}");
                }
            }

            StatusLabel.Text = "File ready! Click 'Use File' to use it";
            CopyBase64Button.Text = "Use File";
            CopyBase64Button.IsEnabled = true;
            Debug.WriteLine("File selection and processing completed successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in OnSelectFileClicked: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            StatusLabel.Text = $"Error selecting file: {ex.Message}";
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
            Debug.WriteLine("==== OnSelectFileClicked end ====");
        }
    }

    private async void OnCopyBase64Clicked(object sender, EventArgs e)
    {
        Debug.WriteLine("==== OnCopyBase64Clicked start ====");
        try
        {
            if (string.IsNullOrEmpty(_base64Data))
            {
                StatusLabel.Text = "No data to copy";
                Debug.WriteLine("OnCopyBase64Clicked called but no data available");
                return;
            }

            if (CopyBase64Button.Text == "Use File" || CopyBase64Button.Text == "OK")
            {
                // Accept the file and close the dialog
                Debug.WriteLine("Use File button clicked, closing dialog with success");
                await CloseDialog(true);
            }
            else
            {
                // Copy to clipboard functionality
                Debug.WriteLine("Copying base64 data to clipboard");
                await Clipboard.SetTextAsync(_base64Data);
                StatusLabel.Text = "Base64 data copied to clipboard";
                
                // Visual feedback
                string originalText = CopyBase64Button.Text;
                CopyBase64Button.Text = "Copied!";
                await Task.Delay(1500);
                CopyBase64Button.Text = originalText;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in OnCopyBase64Clicked: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            StatusLabel.Text = $"Error: {ex.Message}";
        }
        Debug.WriteLine("==== OnCopyBase64Clicked end ====");
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        Debug.WriteLine("Cancel button clicked");
        await CloseDialog(false);
    }

    public async Task<bool> ShowDialogAsync()
    {
        Debug.WriteLine("==== ShowDialogAsync start ====");
        ResetUI();
        _dialogTcs = new TaskCompletionSource<bool>();
        
        try
        {
            // Show the dialog
            Debug.WriteLine("Pushing modal dialog");
            await Navigation.PushModalAsync(this, true);
            
            // Wait for result
            Debug.WriteLine("Awaiting dialog result");
            var result = await _dialogTcs.Task;
            Debug.WriteLine($"Dialog result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in ShowDialogAsync: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
        finally
        {
            Debug.WriteLine("==== ShowDialogAsync end ====");
        }
    }
    
    private async Task CloseDialog(bool success = false)
    {
        Debug.WriteLine("==== CloseDialog start ====");
        try
        {
            var hasFileResult = success && HasFile;
            Debug.WriteLine($"CloseDialog called with success={success}, hasFile={HasFile}, final result={hasFileResult}");
            
            // Set the result before popping the page
            var tcs = _dialogTcs;
            if (tcs != null && !tcs.Task.IsCompleted)
            {
                tcs.TrySetResult(hasFileResult);
                Debug.WriteLine("Dialog result set successfully");
            }
            else
            {
                Debug.WriteLine($"Warning: TaskCompletionSource is null or already completed: {tcs?.Task.IsCompleted}");
            }
            
            // Close the dialog
            Debug.WriteLine("Popping modal dialog");
            await Navigation.PopModalAsync(true);
            Debug.WriteLine("Modal dialog popped successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error closing dialog: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Ensure we always set the result
            var tcs = _dialogTcs;
            if (tcs != null && !tcs.Task.IsCompleted)
            {
                tcs.TrySetResult(false);
                Debug.WriteLine("Set result to false due to exception");
            }
        }
        finally
        {
            Debug.WriteLine("==== CloseDialog end ====");
        }
    }

    protected override void OnDisappearing()
    {
        Debug.WriteLine("FileUploadDialog.OnDisappearing called");
        base.OnDisappearing();
        
        // If the dialog is dismissed by other means, make sure we set the result
        var tcs = _dialogTcs;
        if (tcs != null && !tcs.Task.IsCompleted)
        {
            Debug.WriteLine("Dialog disappearing without result set, setting false result");
            tcs.TrySetResult(false);
        }
    }

    private void ResetUI()
    {
        Debug.WriteLine("ResetUI called");
        _currentFileAttachment = null;
        _base64Data = null;
        FileNameLabel.Text = "No file selected";
        MediaTypeLabel.Text = "-";
        FileSizeLabel.Text = "-";
        PreviewContainer.IsVisible = false;
        PreviewImage.Source = null;  // Fixed: Use PreviewImage instead of Base64PreviewImage
        Base64PreviewLabel.Text = "No data";
        Base64LengthLabel.Text = "";
        StatusLabel.Text = "Select a file to upload";
        CopyBase64Button.Text = "Use File";
        CopyBase64Button.IsEnabled = false;
    }
}