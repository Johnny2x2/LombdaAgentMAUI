using System.ComponentModel;

namespace LombdaAgentMAUI.Core.Models
{
    /// <summary>
    /// Request to create a new agent
    /// </summary>
    public class AgentCreationRequest
    {
        /// <summary>
        /// Name for the new agent
        /// </summary>
        public string Name { get; set; } = "Assistant";
        public string AgentType { get; set; } = "Default";
    }

    /// <summary>
    /// Response with agent details
    /// </summary>
    public class AgentResponse
    {
        /// <summary>
        /// Agent ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Agent name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// File attachment for messages (used in UI only)
    /// </summary>
    public class FileAttachment
    {
        /// <summary>
        /// The data URI in format: data:{mediaType};base64,{base64EncodedData}
        /// </summary>
        public string DataUri { get; set; } = string.Empty;

        /// <summary>
        /// File name with extension
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// MIME media type (e.g., "image/jpeg")
        /// </summary>
        public string MediaType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to send a message to an agent
    /// </summary>
    public class MessageRequest
    {
        /// <summary>
        /// Message text content
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Optional thread ID for conversation context
        /// </summary>
        public string? ThreadId { get; set; }

        /// <summary>
        /// Gets or sets the file data encoded in Base64 format in URL format.
        /// </summary>
        public string? FileBase64Data { get; set; }

        // Kept for backward compatibility with existing code
        [System.Text.Json.Serialization.JsonIgnore]
        internal List<FileAttachment>? Files { get; set; }
    }

    /// <summary>
    /// Response from agent message
    /// </summary>
    public class MessageResponse
    {
        /// <summary>
        /// Agent ID
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Thread ID for this conversation
        /// </summary>
        public string ThreadId { get; set; } = string.Empty;

        /// <summary>
        /// Response text
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Chat message for the UI with property change notifications
    /// </summary>
    public class ChatMessage : INotifyPropertyChanged
    {
        private string _text = string.Empty;
        private bool _isUser;
        private bool _isMarkdown = true; // Default to true for agent responses
        private DateTime _timestamp = DateTime.Now;
        private List<FileAttachment>? _files;

        public string Text 
        { 
            get => _text; 
            set 
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsUser 
        { 
            get => _isUser; 
            set 
            {
                if (_isUser != value)
                {
                    _isUser = value;
                    OnPropertyChanged();
                    // User messages are typically plain text, agent messages can be markdown
                    if (_isUser)
                    {
                        IsMarkdown = false;
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether the text should be rendered as markdown
        /// </summary>
        public bool IsMarkdown
        {
            get => _isMarkdown;
            set
            {
                if (_isMarkdown != value)
                {
                    _isMarkdown = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime Timestamp 
        { 
            get => _timestamp; 
            set 
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayTime));
                }
            }
        }

        /// <summary>
        /// File attachments included with this message
        /// </summary>
        public List<FileAttachment>? Files
        {
            get => _files;
            set
            {
                if (_files != value)
                {
                    _files = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasFiles));
                }
            }
        }

        /// <summary>
        /// Indicates whether this message has file attachments
        /// </summary>
        public bool HasFiles => Files != null && Files.Count > 0;

        public string DisplayTime => Timestamp.ToString("HH:mm:ss");

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a chat session for a specific agent
    /// </summary>
    public class AgentSession
    {
        /// <summary>
        /// Agent ID
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Current thread ID for the conversation
        /// </summary>
        public string? ThreadId { get; set; }

        /// <summary>
        /// Last response ID (for API continuity)
        /// </summary>
        public string? LastResponseId { get; set; }

        /// <summary>
        /// Chat messages in this session
        /// </summary>
        public List<ChatMessage> Messages { get; set; } = new();

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        public DateTime LastActivity { get; set; } = DateTime.Now;

        /// <summary>
        /// Agent name for display purposes
        /// </summary>
        public string AgentName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Container for all agent sessions
    /// </summary>
    public class SessionData
    {
        /// <summary>
        /// Dictionary of agent sessions keyed by agent ID
        /// </summary>
        public Dictionary<string, AgentSession> Sessions { get; set; } = new();

        /// <summary>
        /// Last selected agent ID
        /// </summary>
        public string? LastSelectedAgentId { get; set; }
    }
}