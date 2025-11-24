using System.IO;

namespace CopilotChatViewer.Models
{
    /// <summary>
    /// 履歴フォルダからCopilotチャット履歴JSONファイルを列挙するヘルパー
    /// </summary>
    public static class ChatHistoryLocator
    {
        public static IEnumerable<string> EnumerateHistoryFiles()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var workspaceStorage = Path.Combine(appData, "Code", "User", "workspaceStorage");
            var globalStorage = Path.Combine(appData, "Code", "User", "globalStorage", "emptyWindowChatSessions");

            // workspaceStorage: {workspaceId}\chatSessions\{sessionId}.json
            if (Directory.Exists(workspaceStorage))
            {
                foreach (var wsDir in Directory.GetDirectories(workspaceStorage))
                {
                    var chatSessionsDir = Path.Combine(wsDir, "chatSessions");
                    if (Directory.Exists(chatSessionsDir))
                    {
                        foreach (var file in Directory.GetFiles(chatSessionsDir, "*.json", SearchOption.TopDirectoryOnly))
                        {
                            yield return file;
                        }
                    }
                }
            }

            // globalStorage: {sessionId}.json
            if (Directory.Exists(globalStorage))
            {
                foreach (var file in Directory.GetFiles(globalStorage, "*.json", SearchOption.TopDirectoryOnly))
                {
                    yield return file;
                }
            }
        }
    }
}
