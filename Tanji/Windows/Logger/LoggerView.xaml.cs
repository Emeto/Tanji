using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Documents;

using Sulakore.Protocol;

namespace Tanji.Windows.Logger
{
    public partial class LoggerView : Window
    {
        private readonly LoggerViewModel _vm;
        private readonly SolidColorBrush _outHighlight, _inHighlight;

        public LoggerView()
        {
            _vm = new LoggerViewModel(this);
            _inHighlight = new SolidColorBrush(Color.FromRgb(178, 34, 34));
            _outHighlight = new SolidColorBrush(Color.FromRgb(0, 102, 204));

            InitializeComponent();
        }

        public void DisplayMessage(MessageEntry entry)
        {
            HMessage message = entry.Message;
            TextPointer end = LoggerTxt.Document.ContentEnd;
            bool isOutgoing = (message.Destination == HDestination.Server);

            var textRange = new TextRange(end, end);
            textRange.Text = $"{entry.Title}({message.Header}, {message.Length}) <---> {message}\r\n---------------\r\n";

            Brush foreground = (isOutgoing ? _outHighlight : _inHighlight);
            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, foreground);

            LoggerTxt.ScrollToEnd();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;

            base.OnClosing(e);
        }
    }
}