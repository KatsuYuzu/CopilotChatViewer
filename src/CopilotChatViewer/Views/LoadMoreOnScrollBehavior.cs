using System.Windows.Controls;
using CopilotChatViewer.Models;
using Microsoft.Xaml.Behaviors;

namespace CopilotChatViewer.Views
{
    public class LoadMoreOnScrollBehavior : Behavior<ListView>
    {
        private bool isLoading = false;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.RemoveHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));
        }

        private async void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            const double threshold = 10; // 終端から指定px手前で発火
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - threshold)
            {
                if (!isLoading && AssociatedObject.DataContext is MainWindowViewModel vm)
                {
                    try
                    {
                        isLoading = true;
                        await vm.LoadNextMessagesAsync(10);
                    }
                    finally
                    {
                        isLoading = false;
                    }
                }
            }
        }
    }
}
