using System.Threading.Tasks;

using Sulakore.Protocol;
using Sulakore.Communication;
using System;
using System.Net;

namespace Tangine
{
    internal class ContractorProxy : IInterceptor
    {
        private readonly HNode _remoteContractor;

        public IPEndPoint Proxy
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }
        public bool IsUsingProxy
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }
        public HotelEndPoint RemoteEndPoint { get; set; }

        public ContractorProxy(HNode remoteContractor)
        {
            _remoteContractor = remoteContractor;
        }

        public Task<int> SendToServerAsync(byte[] data)
        {
            return _remoteContractor.SendMessageAsync(5, data.Length, data);
        }
        public Task<int> SendToServerAsync(HMessage message)
        {
            throw new NotImplementedException();
        }
        public Task<int> SendToServerAsync(ushort header, params object[] values)
        {
            return SendToServerAsync(HMessage.Construct(header, values));
        }

        public Task<int> SendToClientAsync(byte[] data)
        {
            return _remoteContractor.SendMessageAsync(4, data.Length, data);
        }
        public Task<int> SendToClientAsync(HMessage message)
        {
            throw new NotImplementedException();
        }
        public Task<int> SendToClientAsync(ushort header, params object[] values)
        {
            return SendToClientAsync(HMessage.Construct(header, values));
        }
    }
}