using LombdaAgentMAUI.Core.Models;
using LombdaAgentMAUI.Core.Utilities;
using System.Diagnostics;

namespace LombdaAgentMAUI.Services
{
    /// <summary>
    /// Service for picking and processing files in MAUI app
    /// </summary>
    public interface IFilePickerService
    {
        Task<List<FileAttachment>> PickAndProcessFilesAsync(FilePickerFileType? fileTypes = null, bool allowMultiple = true);
        Task<FileAttachment?> PickAndProcessSingleFileAsync(FilePickerFileType? fileTypes = null);
        Task<string?> PickAndProcessSingleFileToBase64DataAsync(FilePickerFileType? fileTypes = null);
    }
    
    public class FilePickerService : IFilePickerService
    {
        public FilePickerService()
        {
            Debug.WriteLine("FilePickerService constructor called");
        }

        /// <summary>
        /// Pick and process multiple files into FileAttachment objects
        /// </summary>
        public async Task<List<FileAttachment>> PickAndProcessFilesAsync(FilePickerFileType? fileTypes = null, bool allowMultiple = true)
        {
            Debug.WriteLine($"PickAndProcessFilesAsync called, allowMultiple={allowMultiple}");
            try
            {
                var options = new PickOptions
                {
                    FileTypes = fileTypes ?? PickerFileTypes.All,
                    PickerTitle = "Select Files to Attach"
                };

                Debug.WriteLine("Creating picker options");
                FileResult[]? result = null;
                
                Debug.WriteLine("About to show file picker");
                if (allowMultiple)
                {
                    Debug.WriteLine("Calling PickMultipleAsync");
                    try
                    {
                        var files = await FilePicker.Default.PickMultipleAsync(options);
                        if (files != null)
                        {
                            int fileCount = files.Count();
                            Debug.WriteLine("PickMultipleAsync returned: " + fileCount + " files");
                            if (fileCount > 0)
                            {
                                result = files.ToArray();
                                Debug.WriteLine($"Files converted to array, length: {result.Length}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("PickMultipleAsync returned null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in PickMultipleAsync: {ex.Message}");
                        Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                }
                else
                {
                    Debug.WriteLine("Calling PickAsync");
                    try
                    {
                        var file = await FilePicker.Default.PickAsync(options);
                        if (file != null) 
                        {
                            Debug.WriteLine("PickAsync returned: " + file.FileName);
                            result = new[] { file };
                            Debug.WriteLine("Single file added to array");
                        }
                        else
                        {
                            Debug.WriteLine("PickAsync returned null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in PickAsync: {ex.Message}");
                        Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                }

                // If no files were selected, return empty list
                if (result == null || result.Length == 0)
                {
                    Debug.WriteLine("No files selected, returning empty list");
                    return new List<FileAttachment>();
                }

                Debug.WriteLine($"Processing {result.Length} files");
                var attachments = new List<FileAttachment>();
                
                foreach (var file in result)
                {
                    try
                    {
                        Debug.WriteLine($"Processing file: {file.FileName}");
                        Debug.WriteLine("Opening read stream");
                        using var stream = await file.OpenReadAsync();
                        Debug.WriteLine($"Stream opened, creating attachment for {file.FileName}");
                        var attachment = await FileAttachmentUtility.CreateFromStreamAsync(stream, file.FileName);
                        Debug.WriteLine($"Attachment created for {file.FileName}, media type: {attachment.MediaType}");
                        
                        attachments.Add(attachment);
                        Debug.WriteLine($"Added attachment to list, count: {attachments.Count}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing file {file.FileName}: {ex.Message}");
                        Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                        // Continue with other files even if one fails
                    }
                }

                Debug.WriteLine($"PickAndProcessFilesAsync completed, returning {attachments.Count} attachments");
                return attachments;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PickAndProcessFilesAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<FileAttachment>();
            }
        }

        /// <summary>
        /// Pick and process a single file into a FileAttachment object
        /// </summary>
        public async Task<FileAttachment?> PickAndProcessSingleFileAsync(FilePickerFileType? fileTypes = null)
        {
            Debug.WriteLine("PickAndProcessSingleFileAsync called");
            try
            {
                Debug.WriteLine("Creating pick options");
                var options = new PickOptions
                {
                    FileTypes = fileTypes ?? PickerFileTypes.All,
                    PickerTitle = "Select File to Attach"
                };

                Debug.WriteLine("About to call FilePicker.PickAsync");
                FileResult? result = null;
                try
                {
                    result = await FilePicker.Default.PickAsync(options);
                    if (result != null)
                    {
                        Debug.WriteLine("PickAsync returned: " + result.FileName);
                    }
                    else
                    {
                        Debug.WriteLine("PickAsync returned null");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception in FilePicker.PickAsync: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                
                if (result == null)
                {
                    Debug.WriteLine("No file selected, returning null");
                    return null;
                }

                try
                {
                    Debug.WriteLine($"Opening read stream for {result.FileName}");
                    using var stream = await result.OpenReadAsync();
                    Debug.WriteLine("Stream opened successfully");
                    
                    Debug.WriteLine("Creating file attachment");
                    var attachment = await FileAttachmentUtility.CreateFromStreamAsync(stream, result.FileName);
                    Debug.WriteLine($"Attachment created: {attachment.FileName}, media type: {attachment.MediaType}");
                    
                    return attachment;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing file {result.FileName}: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PickAndProcessSingleFileAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// Pick a single file and convert it directly to a base64 data URI for API
        /// </summary>
        public async Task<string?> PickAndProcessSingleFileToBase64DataAsync(FilePickerFileType? fileTypes = null)
        {
            Debug.WriteLine("PickAndProcessSingleFileToBase64DataAsync called");
            try
            {
                Debug.WriteLine("Calling PickAndProcessSingleFileAsync");
                var attachment = await PickAndProcessSingleFileAsync(fileTypes);
                
                if (attachment != null)
                {
                    int dataLength = attachment.DataUri?.Length ?? 0;
                    Debug.WriteLine("Attachment received, returning DataUri with length: " + dataLength);
                    return attachment.DataUri;
                }
                else
                {
                    Debug.WriteLine("No attachment received, returning null");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PickAndProcessSingleFileToBase64DataAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }
    }
    
    /// <summary>
    /// Common file type collections for picker
    /// </summary>
    public static class PickerFileTypes
    {
        public static FilePickerFileType All => new FilePickerFileType(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "public.item" } },
                { DevicePlatform.Android, new[] { "*/*" } },
                { DevicePlatform.WinUI, new[] { "*" } },
                { DevicePlatform.macOS, new[] { "*" } }
            });
            
        public static FilePickerFileType Images => new FilePickerFileType(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "public.image" } },
                { DevicePlatform.Android, new[] { "image/*" } },
                { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" } },
                { DevicePlatform.macOS, new[] { "jpg", "jpeg", "png", "gif", "bmp" } }
            });
            
        public static FilePickerFileType Documents => new FilePickerFileType(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "public.text", "public.document" } },
                { DevicePlatform.Android, new[] { "text/*", "application/pdf", "application/msword" } },
                { DevicePlatform.WinUI, new[] { ".txt", ".pdf", ".doc", ".docx", ".rtf", ".md" } },
                { DevicePlatform.macOS, new[] { "txt", "pdf", "doc", "docx", "rtf", "md" } }
            });
    }
}