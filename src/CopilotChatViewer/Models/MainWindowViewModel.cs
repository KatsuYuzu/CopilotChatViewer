using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CopilotChatViewer.Models
{
    public enum MainUiState
    {
        Normal,
        Searching,
        CopyCompleted
    }

    public partial class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<ChatHistorySummary> Histories { get; } = [];
        public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];

        [ObservableProperty]
        public partial ChatHistorySummary? SelectedHistory { get; set; }

        [ObservableProperty]
        public partial bool IsLoadingHistories { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CopyButtonIcon))]
        [NotifyPropertyChangedFor(nameof(SearchButtonIcon))]
        [NotifyPropertyChangedFor(nameof(SearchButtonToolTip))]
        public partial MainUiState UiState { get; set; } = MainUiState.Normal;

        // getter専用プロパティを追加
        public string CopyButtonIcon => UiState == MainUiState.CopyCompleted ? "✔️" : "📋";
        public string SearchButtonIcon => UiState == MainUiState.Searching ? "❌" : "🔍";
        public string SearchButtonToolTip => UiState == MainUiState.Searching ? "クリア" : "検索";

        [ObservableProperty]
        public partial string Keyword { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool HasSearched { get; set; } = false;

        private readonly IChatHistoryService _historyService;
        private readonly IClipboardService _clipboardService;
        private string? currentFilePath;
        private bool isLoadingMessages = false;

        public MainWindowViewModel()
            : this(new ChatHistoryService(), new ClipboardService())
        {
        }

        public MainWindowViewModel(IChatHistoryService historyService, IClipboardService clipboardService)
        {
            _historyService = historyService;
            _clipboardService = clipboardService;
            _ = LoadHistoriesAsync();
        }

        partial void OnSelectedHistoryChanged(ChatHistorySummary? value)
        {
            _ = LoadSessionAsync(value);
        }

        private async Task LoadHistoriesAsync()
        {
            IsLoadingHistories = true;
            Histories.Clear();
            var list = await _historyService.GetHistoriesAsync();
            foreach (var item in list)
            {
                Histories.Add(item);
            }
            IsLoadingHistories = false;
        }

        private async Task LoadSessionAsync(ChatHistorySummary? history)
        {
            Messages.Clear();
            currentFilePath = null;
            if (history == null) return;
            currentFilePath = history.FilePath;
            var msgs = await _historyService.GetMessagesAsync(currentFilePath, 10); // 先頭から10件取得
            foreach (var m in msgs)
            {
                Messages.Add(m);
            }
        }

        public async Task LoadNextMessagesAsync(int count)
        {
            if (currentFilePath == null || isLoadingMessages) return;
            try
            {
                isLoadingMessages = true;
                var msgs = await _historyService.LoadNextMessagesAsync(count);
                foreach (var m in msgs)
                {
                    Messages.Add(m);
                }
            }
            finally
            {
                isLoadingMessages = false;
            }
        }

        [RelayCommand]
        private async Task CopyMessagesToClipboardAsync()
        {
            await LoadNextMessagesAsync(int.MaxValue);
            var html = BuildMessagesHtml(Messages);
            _clipboardService.SetText(html);
            SetCopyCompleted();
        }

        private void SetCopyCompleted()
        {
            UiState = MainUiState.CopyCompleted;
            Task.Delay(2000).ContinueWith(_ => UiState = MainUiState.Normal);
        }

        private static string BuildMessagesHtml(IEnumerable<ChatMessageViewModel> messages)
        {
            var sb = new StringBuilder();
            sb.AppendLine("""
            <div><style>
            .chat-container { display: flex; flex-direction: column; gap: 8px; }
            .msg-row { display: flex; }
            .msg-bubble { padding: 12px 16px; border-radius: 16px; font-family: 'Segoe UI', sans-serif; font-size: 15px; box-shadow: 0 2px 8px #0001; word-break: break-word; }
            .user { border-radius: 16px; background: #e6f3ff; margin-left: auto; }
            .copilot { border-radius: 16px; background: #fffbe6; margin-right: auto; }
            .icon { font-size: 20px; vertical-align: middle; margin-right: 8px; }
            .msg-row.user { justify-content: flex-end; }
            .msg-row.copilot { justify-content: flex-start; }
            </style>
            <div class="chat-container">
            """);
            foreach (var m in messages)
            {
                var roleClass = m.IsUser ? "user" : "copilot";
                var rowClass = $"msg-row {roleClass}";
                sb.AppendLine($"""
                <div class="{rowClass}"><div class="msg-bubble {roleClass}">
                    <span class="icon">{System.Net.WebUtility.HtmlEncode(m.Icon)}</span>{System.Net.WebUtility.HtmlEncode(m.Content).Replace("\n", "<br/>")}
                </div></div>
                """);
            }
            sb.Append("</div></div>");
            return sb.ToString();
        }

        [RelayCommand]
        private async Task SearchOrClearAsync()
        {
            if (!HasSearched)
            {
                await SearchFilesAsync();
            }
            else
            {
                ClearSearch();
            }
        }

        [RelayCommand]
        private async Task SearchOnEnterKeyAsync(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SearchFilesAsync();
            }
        }

        private async Task SearchFilesAsync()
        {
            IsLoadingHistories = true;
            HasSearched = true;
            UiState = MainUiState.Searching;
            Histories.Clear();
            var list = await _historyService.SearchHistoriesAsync(Keyword);
            foreach (var item in list)
            {
                Histories.Add(item);
            }
            IsLoadingHistories = false;
        }

        private void ClearSearch()
        {
            HasSearched = false;
            UiState = MainUiState.Normal;
            Histories.Clear();
            Keyword = string.Empty;
            _ = LoadHistoriesAsync();
        }
    }
}
