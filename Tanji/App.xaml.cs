using System;
using System.Windows;
using System.Collections.Generic;

using Tanji.Services;

using Tangine.Habbo;

using Sulakore.Protocol;
using Sulakore.Habbo.Web;
using Sulakore.Communication;

namespace Tanji
{
    public partial class App : Application
    {
        private static readonly object _dataLock;

        public static List<IHaltable> Haltables { get; }
        public static SortedList<int, IReceiver> Receivers { get; }

        public static HGame Game { get; set; }

        public static HGameData GameData { get; }
        public static HInterceptor Interceptor { get; }

        static App()
        {
            _dataLock = new object();

            Haltables = new List<IHaltable>();
            Receivers = new SortedList<int, IReceiver>();

            GameData = new HGameData();
            Interceptor = new HInterceptor();

            Interceptor.Connected += Connected;
            Interceptor.Disconnected += Disconnected;
            Interceptor.DataOutgoing += HandleData;
            Interceptor.DataIncoming += HandleData;
        }

        private static void Connected(object sender, EventArgs e)
        {
            Current.Dispatcher.Invoke(Restore);
        }
        private static void Disconnected(object sender, EventArgs e)
        {
            Current.Dispatcher.Invoke(Halt);
        }

        private static void Halt()
        {
            Haltables.ForEach(h => h.Halt());
        }
        private static void Restore()
        {
            Haltables.ForEach(h => h.Restore());
        }

        private static void HandleData(object sender, DataInterceptedEventArgs e)
        {
            if (Receivers.Count == 0) return;
            bool isOutgoing = (e.Message.Destination == HDestination.Server);

            foreach (IReceiver receiver in Receivers.Values)
            {
                if (!receiver.IsReceiving) continue;
                if (isOutgoing)
                {
                    receiver.HandleOutgoing(e);
                }
                else receiver.HandleIncoming(e);
            }
        }
    }
}