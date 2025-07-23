using LombdaAgentMAUI.Core.Models;
using System.Text;

namespace LombdaAgentMAUI.Core.Utilities
{
    /// <summary>
    /// Utility class for handling file attachments
    /// </summary>
    public static class FileAttachmentUtility
    {
        /// <summary>
        /// Convert a file to a FileAttachment object with base64-encoded data URI
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>FileAttachment object</returns>
        public static async Task<FileAttachment> CreateFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            var fileName = Path.GetFileName(filePath);
            var mediaType = GetMimeTypeFromFileName(fileName);
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            
            return CreateFromBytes(fileBytes, fileName, mediaType);
        }

        /// <summary>
        /// Convert a file to a base64-encoded data URI string for API
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>Base64 data URI string</returns>
        public static async Task<string> CreateBase64DataUriFromFileAsync(string filePath)
        {
            var attachment = await CreateFromFileAsync(filePath);
            return attachment.DataUri;
        }

        /// <summary>
        /// Convert a stream to a FileAttachment object with base64-encoded data URI
        /// </summary>
        /// <param name="stream">File stream</param>
        /// <param name="fileName">File name with extension</param>
        /// <returns>FileAttachment object</returns>
        public static async Task<FileAttachment> CreateFromStreamAsync(Stream stream, string fileName)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var mediaType = GetMimeTypeFromFileName(fileName);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();
            
            return CreateFromBytes(fileBytes, fileName, mediaType);
        }

        /// <summary>
        /// Convert a stream to a base64-encoded data URI string for API
        /// </summary>
        /// <param name="stream">File stream</param>
        /// <param name="fileName">File name with extension</param>
        /// <returns>Base64 data URI string</returns>
        public static async Task<string> CreateBase64DataUriFromStreamAsync(Stream stream, string fileName)
        {
            var attachment = await CreateFromStreamAsync(stream, fileName);
            return attachment.DataUri;
        }

        /// <summary>
        /// Create a FileAttachment object from byte array with base64-encoded data URI
        /// </summary>
        /// <param name="fileBytes">File bytes</param>
        /// <param name="fileName">File name with extension</param>
        /// <param name="mediaType">MIME media type</param>
        /// <returns>FileAttachment object</returns>
        public static FileAttachment CreateFromBytes(byte[] fileBytes, string fileName, string mediaType)
        {
            if (fileBytes == null || fileBytes.Length == 0)
                throw new ArgumentException("File data cannot be empty", nameof(fileBytes));
            
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));
            
            if (string.IsNullOrEmpty(mediaType))
                mediaType = "application/octet-stream";

            var base64EncodedData = Convert.ToBase64String(fileBytes);
            var dataUri = $"data:{mediaType};base64,{base64EncodedData}";

            return new FileAttachment
            {
                DataUri = dataUri,
                FileName = fileName,
                MediaType = mediaType
            };
        }

        /// <summary>
        /// Create a base64-encoded data URI string from byte array for API
        /// </summary>
        /// <param name="fileBytes">File bytes</param>
        /// <param name="fileName">File name with extension</param>
        /// <param name="mediaType">MIME media type</param>
        /// <returns>Base64 data URI string</returns>
        public static string CreateBase64DataUriFromBytes(byte[] fileBytes, string fileName, string mediaType)
        {
            var attachment = CreateFromBytes(fileBytes, fileName, mediaType);
            return attachment.DataUri;
        }

        /// <summary>
        /// Get the MIME type based on file extension
        /// </summary>
        /// <param name="fileName">File name with extension</param>
        /// <returns>MIME type string</returns>
        public static string GetMimeTypeFromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".html" or ".htm" => "text/html",
                ".css" => "text/css",
                ".js" => "text/javascript",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".zip" => "application/zip",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".csv" => "text/csv",
                ".md" => "text/markdown",
                _ => "application/octet-stream" // Default MIME type
            };
        }
        
        /// <summary>
        /// Convert a list of FileAttachment objects to a single FileBase64Data string
        /// </summary>
        /// <param name="attachments">List of FileAttachment objects</param>
        /// <returns>Single FileBase64Data string for the API</returns>
        public static string? ConvertAttachmentsToFileBase64Data(List<FileAttachment>? attachments)
        {
            if (attachments == null || attachments.Count == 0)
                return null;
                
            // If there's only one attachment, use its DataUri directly
            if (attachments.Count == 1)
                return attachments[0].DataUri;
                
            // For multiple attachments, we could implement a format that combines them,
            // but for now, let's just use the first one as the API seems to expect a single file
            System.Diagnostics.Debug.WriteLine($"Warning: Multiple file attachments detected but API only supports one. Using first attachment.");
            return attachments[0].DataUri;
        }
    }
}