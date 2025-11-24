using Newtonsoft.Json;

namespace CopilotChatParser;

/// <summary>
/// JSON形式のCopilotチャット履歴を読み込むパーサー
/// </summary>
public sealed class JsonHistoryParser : IDisposable
{
    private readonly StreamReader _streamReader;
    private readonly JsonTextReader _jsonTextReader;
    private readonly Queue<ChatMessage> _messageQueue = new();
    private bool _isFinished = false;
    private bool _isInRequestsArray = false;
    private readonly SemaphoreSlim _readLock = new(1, 1);

    public static JsonHistoryParser Create(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("ファイルパスを指定してください。", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"ファイルが見つかりません: {filePath}");
        }

        return new JsonHistoryParser(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public static JsonHistoryParser Create(Stream stream)
    {
        return new JsonHistoryParser(stream);
    }

    private JsonHistoryParser(Stream stream)
    {
        _streamReader = new StreamReader(stream);
        _jsonTextReader = new JsonTextReader(_streamReader);
    }

    /// <summary>
    /// テスト用のBaseStreamアクセサ
    /// </summary>
    internal Stream BaseStream => _streamReader.BaseStream;

    public async Task<ChatMessage?> ReadAsync()
    {
        await _readLock.WaitAsync();
        try
        {
            // キューに残っているメッセージがあれば返す
            if (_messageQueue.Count > 0)
            {
                return _messageQueue.Dequeue();
            }
            if (_isFinished)
            {
                return null;
            }
            while (await _jsonTextReader.ReadAsync())
            {
                if (_jsonTextReader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = _jsonTextReader.Value?.ToString();
                    if (propertyName == "requests" && !_isInRequestsArray)
                    {
                        if (await _jsonTextReader.ReadAsync() && _jsonTextReader.TokenType == JsonToken.StartArray)
                        {
                            _isInRequestsArray = true;
                        }
                    }
                }
                else if (_isInRequestsArray)
                {
                    if (_jsonTextReader.TokenType == JsonToken.StartObject)
                    {
                        await ProcessSingleRequestObjectAsync();

                        // メッセージがキューに追加されていれば返す
                        if (_messageQueue.Count > 0)
                        {
                            return _messageQueue.Dequeue();
                        }
                    }
                    else if (_jsonTextReader.TokenType == JsonToken.EndArray)
                    {
                        _isInRequestsArray = false;
                        _isFinished = true;
                        break;
                    }
                }
            }
            _isFinished = true;
            return null;
        }
        finally
        {
            _readLock.Release();
        }
    }

    private async Task ProcessSingleRequestObjectAsync()
    {
        string? userMessage = null;
        var aiResponses = new List<string>();
        long? timestampValue = null;
        int depth = 1; // StartObjectを既に読んでいるので深度1から開始

        while (depth > 0 && await _jsonTextReader.ReadAsync())
        {
            if (_jsonTextReader.TokenType == JsonToken.PropertyName)
            {
                var propertyName = _jsonTextReader.Value?.ToString();
                if (propertyName == "message")
                {
                    // messageオブジェクト内のtext抽出
                    if (await _jsonTextReader.ReadAsync() && _jsonTextReader.TokenType == JsonToken.StartObject)
                    {
                        int msgDepth = 1;
                        while (msgDepth > 0 && await _jsonTextReader.ReadAsync())
                        {
                            if (_jsonTextReader.TokenType == JsonToken.PropertyName && _jsonTextReader.Value?.ToString() == "text")
                            {
                                if (await _jsonTextReader.ReadAsync() && _jsonTextReader.TokenType == JsonToken.String)
                                {
                                    userMessage = _jsonTextReader.Value?.ToString();
                                }
                            }
                            else if (_jsonTextReader.TokenType == JsonToken.StartObject)
                            {
                                msgDepth++;
                            }
                            else if (_jsonTextReader.TokenType == JsonToken.EndObject)
                            {
                                msgDepth--;
                            }
                        }
                    }
                }
                else if (propertyName == "response")
                {
                    // response配列のvalue抽出
                    if (await _jsonTextReader.ReadAsync() && _jsonTextReader.TokenType == JsonToken.StartArray)
                    {
                        while (await _jsonTextReader.ReadAsync() && _jsonTextReader.TokenType != JsonToken.EndArray)
                        {
                            if (_jsonTextReader.TokenType == JsonToken.StartObject)
                            {
                                int respDepth = 1;
                                string? responseValue = null;
                                while (respDepth > 0 && await _jsonTextReader.ReadAsync())
                                {
                                    if (_jsonTextReader.TokenType == JsonToken.PropertyName && _jsonTextReader.Value?.ToString() == "value")
                                    {
                                        if (await _jsonTextReader.ReadAsync() && _jsonTextReader.TokenType == JsonToken.String)
                                        {
                                            responseValue = _jsonTextReader.Value?.ToString();
                                        }
                                    }
                                    else if (_jsonTextReader.TokenType == JsonToken.StartObject)
                                    {
                                        respDepth++;
                                    }
                                    else if (_jsonTextReader.TokenType == JsonToken.EndObject)
                                    {
                                        respDepth--;
                                    }
                                    else
                                    {
                                        await _jsonTextReader.SkipAsync();
                                    }
                                }
                                if (!string.IsNullOrEmpty(responseValue))
                                {
                                    var trimmedValue = responseValue.AsSpan().Trim();
                                    if (trimmedValue.Trim('`').Length > 0)
                                    {
                                        aiResponses.Add(trimmedValue.ToString());
                                    }
                                }
                            }
                            else
                            {
                                await _jsonTextReader.SkipAsync();
                            }
                        }
                    }
                }
                else if (propertyName == "timestamp")
                {
                    if (await _jsonTextReader.ReadAsync() && _jsonTextReader.TokenType == JsonToken.Integer)
                    {
                        timestampValue = Convert.ToInt64(_jsonTextReader.Value);
                    }
                }
                else
                {
                    await _jsonTextReader.SkipAsync();
                }
            }
            else if (_jsonTextReader.TokenType == JsonToken.StartObject)
            {
                depth++;
            }
            else if (_jsonTextReader.TokenType == JsonToken.EndObject)
            {
                depth--;
            }
        }

        DateTime? timestamp = timestampValue.HasValue
            ? DateTime.UnixEpoch.AddMilliseconds(timestampValue.Value)
            : null;

        if (!string.IsNullOrEmpty(userMessage))
        {
            _messageQueue.Enqueue(new ChatMessage
            {
                Type = MessageType.User,
                Content = userMessage,
                Timestamp = timestamp
            });
        }
        foreach (var response in aiResponses)
        {
            _messageQueue.Enqueue(new ChatMessage
            {
                Type = MessageType.Copilot,
                Content = response,
                Timestamp = timestamp
            });
        }
    }

    public async IAsyncEnumerable<ChatMessage> ReadAllAsync()
    {
        ChatMessage? message;
        while ((message = await ReadAsync()) != null)
        {
            yield return message;
        }
    }

    #region IDisposable

    private bool disposedValue;

    private void Dispose(bool disposing)
    {
        _readLock.Wait();
        try
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    using (_jsonTextReader) { }
                    using (_streamReader) { }
                }
                disposedValue = true;
            }
        }
        finally
        {
            _readLock.Release();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
