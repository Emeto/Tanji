﻿using Sulakore.Communication;

namespace Tanji.Services
{
    public interface IReceiver
    {
        bool IsReceiving { get; }

        void HandleOutgoing(DataInterceptedEventArgs e);
        void HandleIncoming(DataInterceptedEventArgs e);
    }
}