using CopilotChatParser;

namespace CopilotChatViewer.Models
{
    public class ChatMessageViewModel(ChatMessage model)
    {
        public ChatMessage Model { get; } = model;
        public MessageType? Type => Model.Type;
        public string Content => Model.Content;
        public DateTime? Timestamp => Model.Timestamp;
        public bool IsUser => Model.Type == MessageType.User;
        public bool IsCopilot => Model.Type == MessageType.Copilot;
        public bool IsCopilotOrError => IsCopilot || Model.Type == MessageType.Error;
        public string Icon => IsUser ? "🧑‍💻" : IsCopilot ? "🤖" : "❌";
    }
}
