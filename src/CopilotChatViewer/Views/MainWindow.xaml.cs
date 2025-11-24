using System.Windows;
using CopilotChatViewer.Models;

namespace CopilotChatViewer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel(
                new ChatHistoryService(),
                new ClipboardService()
            );
        }
    }
}
