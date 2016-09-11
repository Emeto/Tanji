using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.Generic;

using Tanji.Helpers;
using Tanji.Services;

using Sulakore.Communication;

namespace Tanji.Windows.Logger
{
    public class LoggerViewModel : ObservableObject, IReceiver, IHaltable
    {
        private Task _readQueueTask;
        private readonly LoggerView _view;
        private readonly Action _displayMessageEntry;
        private readonly object _pushToQueueLock, _messageEntryPopperLock;

        public Queue<MessageEntry> MessageEntries { get; }

        public LoggerViewModel(LoggerView view)
        {
            _view = view;
            _pushToQueueLock = new object();
            _messageEntryPopperLock = new object();
            _displayMessageEntry = DisplayMessageEntry;

            App.Haltables.Add(this);
            App.Receivers.Add(5, this);

            MessageEntries = new Queue<MessageEntry>();
        }

        private void MessageEntryPopper()
        {
            if (Monitor.TryEnter(_messageEntryPopperLock))
            {
                try
                {
                    _view.Dispatcher.Invoke(_displayMessageEntry,
                        DispatcherPriority.Background);
                }
                finally { Monitor.Exit(_messageEntryPopperLock); }
            }
        }
        private void DisplayMessageEntry()
        {
            while (MessageEntries.Count > 0)
            {
                MessageEntry entry = MessageEntries.Dequeue();
                _view.DisplayMessage(entry);
            }
        }
        private void PushToQueue(DataInterceptedEventArgs e)
        {
            lock (_pushToQueueLock)
            {
                MessageEntries.Enqueue(new MessageEntry(e));
            }
            if (_readQueueTask == null || _readQueueTask.IsCompleted)
            {
                _readQueueTask = Task.Factory.StartNew(
                    MessageEntryPopper, TaskCreationOptions.LongRunning);
            }
        }

        #region IHaltable Implementation
        public void Halt()
        {
            IsReceiving = false;
        }
        public void Restore()
        {
            IsReceiving = true;
        }
        #endregion
        #region IReceiver Implementation
        public bool IsReceiving { get; private set; }
        public void HandleOutgoing(DataInterceptedEventArgs e) => PushToQueue(e);
        public void HandleIncoming(DataInterceptedEventArgs e) => PushToQueue(e);
        #endregion
    }
}