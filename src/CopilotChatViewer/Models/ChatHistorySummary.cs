namespace CopilotChatViewer.Models
{
    /// <summary>
    /// 履歴一覧表示用のサマリモデル
    /// </summary>
    public class ChatHistorySummary
    {
        public string FilePath { get; set; } = string.Empty;
        public string FirstMessage { get; set; } = string.Empty;
        public DateTime? Timestamp { get; set; }

        // 表示用：改行をスペースに変換
        public string FirstMessageDisplay =>
            (FirstMessage ?? string.Empty)
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");

        // 日本時間で表示（yyyy/MM/dd HH:mm:ss）
        public string TimestampDisplay => Timestamp.HasValue
            ? Timestamp.Value.ToUniversalTime().AddHours(9).ToString("yyyy/MM/dd HH:mm:ss")
            : "";
    }
}
