using LombdaAgentMAUI.Services;
using System.Diagnostics;

namespace LombdaAgentMAUI;

public partial class FilePickerTestPage : ContentPage
{
    private readonly IFilePickerService _filePickerService;

    public FilePickerTestPage(IFilePickerService filePickerService)
    {
        InitializeComponent();
        _filePickerService = filePickerService;
        Debug.WriteLine("FilePickerTestPage initialized");
    }

    private async void OnPickFileClicked(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine("Pick file button clicked");
            StatusLabel.Text = "Picking file...";
            PickFileButton.IsEnabled = false;
            ProgressIndicator.IsRunning = true;
            ProgressIndicator.IsVisible = true;

            var fileAttachment = await _filePickerService.PickAndProcessSingleFileAsync(FilePickerFileType.Images);
            
            if (fileAttachment != null)
            {
                FileNameLabel.Text = fileAttachment.FileName;
                FileSizeLabel.Text = $"Type: {fileAttachment.MediaType}";
                
                // For images, show preview
                if (fileAttachment.MediaType.StartsWith("image/"))
                {
                    FilePreviewImage.Source = fileAttachment.DataUri;
                    FilePreviewImage.IsVisible = true;
                }
                else
                {
                    FilePreviewImage.IsVisible = false;
                }
                
                StatusLabel.Text = "File picked successfully!";
                Debug.WriteLine($"File picked: {fileAttachment.FileName}");
            }
            else
            {
                FileNameLabel.Text = "No file selected";
                FileSizeLabel.Text = "";
                FilePreviewImage.IsVisible = false;
                StatusLabel.Text = "File selection canceled or failed";
                Debug.WriteLine("No file was selected");
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            Debug.WriteLine($"Error picking file: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            PickFileButton.IsEnabled = true;
            ProgressIndicator.IsRunning = false;
            ProgressIndicator.IsVisible = false;
        }
    }

    private async void OnShowDialogClicked(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine("Show dialog button clicked");
            StatusLabel.Text = "Showing file dialog...";
            ShowDialogButton.IsEnabled = false;

            //[USER]: This does not work you need to use  var fileAttachment = await _filePickerService.PickAndProcessSingleFileAsync(FilePickerFileType.Images);
            var fileDialog = new FileUploadDialog(_filePickerService);
            bool result = await fileDialog.ShowDialogAsync();
            
            if (result)
            {
                StatusLabel.Text = "File selected from dialog";
                FileNameLabel.Text = fileDialog.FileAttachment?.FileName ?? "Unknown file";
                Debug.WriteLine($"File dialog returned success: {fileDialog.FileAttachment?.FileName}");
                
                if (fileDialog.FileAttachment?.MediaType.StartsWith("image/") == true)
                {
                    FilePreviewImage.Source = fileDialog.FileAttachment.DataUri;
                    FilePreviewImage.IsVisible = true;
                }
            }
            else
            {
                StatusLabel.Text = "Dialog canceled or no file selected";
                Debug.WriteLine("File dialog returned without a file");
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            Debug.WriteLine($"Error showing dialog: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            ShowDialogButton.IsEnabled = true;
        }
    }
}