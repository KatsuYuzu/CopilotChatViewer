using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace CopilotChatViewer.Views
{
    public class ScrollToTopBehavior : Behavior<ListView>
    {
        public static readonly DependencyProperty TargetValueProperty =
            DependencyProperty.Register(
                nameof(TargetValue),
                typeof(object),
                typeof(ScrollToTopBehavior),
                new PropertyMetadata(null, OnTargetValueChanged));

        public object? TargetValue
        {
            get => GetValue(TargetValueProperty);
            set => SetValue(TargetValueProperty, value);
        }

        private static void OnTargetValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollToTopBehavior behavior && behavior.AssociatedObject != null)
            {
                var scrollViewer = FindScrollViewer(behavior.AssociatedObject);
                scrollViewer?.ScrollToVerticalOffset(0);
            }
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject d)
        {
            if (d is ScrollViewer sv) return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);
                var result = FindScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
