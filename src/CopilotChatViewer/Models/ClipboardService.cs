using System.Windows;

namespace CopilotChatViewer.Models
{
    public interface IClipboardService
    {
        void SetText(string text);
    }

    public class ClipboardService : IClipboardService
    {
        public void SetText(string text)
        {
            var dataObj = new DataObject();
            dataObj.SetData(DataFormats.Text, text);
            Clipboard.SetDataObject(dataObj, true);
        }
    }
}
