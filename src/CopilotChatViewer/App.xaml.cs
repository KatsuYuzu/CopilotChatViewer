using System.Windows;

namespace CopilotChatViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

#pragma warning disable WPF0001
            // WPF for .NET 9 の新機能 - Windows 10, 11 の Fluent テーマ
            // https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/whats-new/net90?view=netdesktop-9.0
            Application.Current.ThemeMode = ThemeMode.System;
#pragma warning restore WPF0001
        }

    }
}
