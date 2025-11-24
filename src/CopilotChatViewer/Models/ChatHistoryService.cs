using CopilotChatParser;

namespace CopilotChatViewer.Models
{
    public interface IChatHistoryService : IDisposable
    {
        Task<IReadOnlyList<ChatHistorySummary>> GetHistoriesAsync();
        Task<IReadOnlyList<ChatHistorySummary>> SearchHistoriesAsync(string keyword);
        Task<IReadOnlyList<ChatMessageViewModel>> GetMessagesAsync(string filePath, int count);
        Task<IReadOnlyList<ChatMessageViewModel>> LoadNextMessagesAsync(int count);
    }

    public class ChatHistoryService : IChatHistoryService
    {
        private JsonHistoryParser? currentParser;
        private bool disposedValue;
        private readonly SemaphoreSlim _parserLock = new(1, 1);

        // パーサーからメッセージを読み取り、パース失敗時はエラー用メッセージを返す
        private static async Task<ChatMessage?> TryReadMessageAsync(JsonHistoryParser parser)
        {
            try
            {
                return await parser.ReadAsync();
            }
            catch (Exception ex)
            {
                // パース失敗時はエラー用メッセージを返す
                return new ChatMessage
                {
                    Type = MessageType.Error,
                    Content = $"履歴が読み込めませんでした：{ex.GetType()},{ex.Message}",
                    Timestamp = null
                };
            }
        }

        public async Task<IReadOnlyList<ChatHistorySummary>> GetHistoriesAsync()
        {
            var tempList = new List<ChatHistorySummary>();
            foreach (var file in ChatHistoryLocator.EnumerateHistoryFiles())
            {
                using var parser = JsonHistoryParser.Create(file);
                var message = await TryReadMessageAsync(parser);
                if (message == null) continue;
                var summary = new ChatHistorySummary
                {
                    FilePath = file,
                    FirstMessage = message.Content,
                    Timestamp = message.Timestamp
                };
                tempList.Add(summary);
            }
            return [.. tempList.OrderByDescending(x => x.Timestamp)];
        }

        public async Task<IReadOnlyList<ChatHistorySummary>> SearchHistoriesAsync(string keyword)
        {
            var filePaths = ChatHistoryLocator.EnumerateHistoryFiles().ToList();
            var tempList = new List<ChatHistorySummary>();
            await Task.Run(() =>
            {
                Parallel.ForEach(filePaths, async filePath =>
                {
                    using var parser = JsonHistoryParser.Create(filePath);
                    ChatMessage? msg;
                    bool found = false;
                    do
                    {
                        msg = TryReadMessageAsync(parser).Result;
                        if (msg != null && msg.Type != null && !string.IsNullOrEmpty(msg.Content) && msg.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    } while (msg != null);
                    if (found)
                    {
                        using var parser2 = JsonHistoryParser.Create(filePath);
                        var message = await TryReadMessageAsync(parser2);
                        if (message == null) return;
                        var summary = new ChatHistorySummary
                        {
                            FilePath = filePath,
                            FirstMessage = message.Content,
                            Timestamp = message.Timestamp
                        };
                        tempList.Add(summary);
                    }
                });
            });
            return [.. tempList.OrderByDescending(x => x.Timestamp)];
        }

        public async Task<IReadOnlyList<ChatMessageViewModel>> GetMessagesAsync(string filePath, int count)
        {
            await _parserLock.WaitAsync();
            try
            {
                currentParser?.Dispose();
                currentParser = JsonHistoryParser.Create(filePath);
                var result = new List<ChatMessageViewModel>();
                for (int i = 0; i < count; i++)
                {
                    var msg = await TryReadMessageAsync(currentParser);
                    if (msg == null) break;
                    result.Add(new ChatMessageViewModel(msg));
                }
                return result;
            }
            finally
            {
                _parserLock.Release();
            }
        }

        public async Task<IReadOnlyList<ChatMessageViewModel>> LoadNextMessagesAsync(int count)
        {
            await _parserLock.WaitAsync();
            try
            {
                if (currentParser == null) return [];
                var result = new List<ChatMessageViewModel>();
                for (int i = 0; i < count; i++)
                {
                    var msg = await TryReadMessageAsync(currentParser);
                    if (msg == null) break;
                    result.Add(new ChatMessageViewModel(msg));
                }
                return result;
            }
            finally
            {
                _parserLock.Release();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            _parserLock.Wait();
            try
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        currentParser?.Dispose();
                        currentParser = null;
                    }
                    disposedValue = true;
                }
            }
            finally
            {
                _parserLock.Release();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
