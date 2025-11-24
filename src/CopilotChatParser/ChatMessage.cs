namespace CopilotChatParser;

/// <summary>
/// チャットメッセージを表現するモデル
/// </summary>
public record ChatMessage
{
    /// <summary>
    /// メッセージの種類
    /// </summary>
    public required MessageType? Type { get; init; }

    /// <summary>
    /// メッセージ内容
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// メッセージのタイムスタンプ（日時）
    /// </summary>
    public required DateTime? Timestamp { get; init; }
}

/// <summary>
/// メッセージの種類
/// </summary>
public enum MessageType
{
    /// <summary>
    /// ユーザーからのメッセージ
    /// </summary>
    User,

    /// <summary>
    /// Copilotからのメッセージ
    /// </summary>
    Copilot,

    /// <summary>
    /// 読み込みエラー
    /// </summary>
    Error
}
