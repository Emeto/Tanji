using Tanji.Helpers;

using Sulakore.Protocol;
using Sulakore.Communication;

namespace Tanji.Windows.Logger
{
    public class MessageEntry : ObservableObject
    {
        public string Title { get; }
        public HMessage Message { get; }

        public MessageEntry(DataInterceptedEventArgs args)
        {
            bool isOutgoing =
                (args.Message.Destination == HDestination.Server);

            Message = args.Message;
            Title = (isOutgoing ? "Outgoing" : "Incoming");
        }
    }
}