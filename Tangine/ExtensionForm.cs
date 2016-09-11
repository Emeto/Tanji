using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading.Tasks;

using Tangine.Habbo;

using Sulakore.Habbo;
using Sulakore.Modules;
using Sulakore.Habbo.Web;
using Sulakore.Protocol;
using Sulakore.Communication;

namespace Tangine
{
    public class ExtensionForm : ExtensionForm<HTriggers>
    { }
    public class ExtensionForm<T> : Form, ITExtension where T : HTriggers
    {
        private int _initStep;
        private readonly ITContext _context;
        private readonly IContractor _installer;
        private readonly HNode _remoteContractor;
        private readonly TaskCompletionSource<bool> _initializationSource;

        ITContext ITExtension.Context => _context;
        HTriggers ITExtension.Triggers => Triggers;
        IContractor IModule.Installer => _installer;

        [Browsable(false)]
        public virtual bool IsRemoteModule { get; }

        [Browsable(false)]
        public T Triggers { get; }

        private HGame _game;
        [Browsable(false)]
        public HGame Game
        {
            get
            {
                return (_game ??
                    _context?.Game);
            }
        }

        private HHotel? _hotel;
        [Browsable(false)]
        public HHotel Hotel
        {
            get
            {
                return (_hotel ??
                   (_installer?.Hotel ?? HHotel.Com));
            }
        }

        private HGameData _gameData;
        [Browsable(false)]
        public HGameData GameData
        {
            get
            {
                return (_gameData ??
                    _installer?.GameData);
            }
        }

        private readonly IInterceptor _interceptor;
        [Browsable(false)]
        public IInterceptor Interceptor
        {
            get
            {
                return (_interceptor ??
                    _installer?.Connection);
            }
        }

        public ExtensionForm()
        {
            _installer = Contractor.GetInstaller(GetType());
            _context = (_installer as ITContext);

            if (_installer == null && IsRemoteModule)
            {
                _remoteContractor = GetRemoteContractor();
                if (_remoteContractor != null)
                {
                    _interceptor = new ContractorProxy(_remoteContractor);
                    _initializationSource = new TaskCompletionSource<bool>();

                    Task receiveRemContDataTask =
                        ReceiveRemoteContractorDataAsync();

                    RequestRemoteContractorData();
                    _initializationSource.Task.Wait();
                    _initializationSource = null;
                }
            }
            Triggers = Activator.CreateInstance<T>();
        }

        public virtual void ModifyGame(HGame game)
        { }
        public virtual void ModifyGameData(HGameData gameData)
        { }

        public virtual void HandleOutgoing(DataInterceptedEventArgs e)
        { }
        public virtual void HandleIncoming(DataInterceptedEventArgs e)
        { }

        private void RequestRemoteContractorData()
        {
            _remoteContractor.SendMessageAsync(0); // Hotel
            _remoteContractor.SendMessageAsync(1); // Game
            _remoteContractor.SendMessageAsync(2); // GameData
            _remoteContractor.SendMessageAsync(3); // Connection Info
        }
        private async Task ReceiveRemoteContractorDataAsync()
        {
            try
            {
                HMessage packet = await _remoteContractor
                    .ReceiveMessageAsync().ConfigureAwait(false);

                if (packet == null)
                    Environment.Exit(0);

                #region Switch: packet.Header
                switch (packet.Header)
                {
                    case 0:
                    {
                        _initStep++;
                        _hotel = (HHotel)packet.ReadShort();
                        break;
                    }
                    case 1:
                    {
                        _initStep++;
                        string location = packet.ReadString();
                        if (!string.IsNullOrWhiteSpace(location))
                        {
                            _game = new HGame(location);

                            _game.Decompress();
                            _game.Disassemble();

                            if (_initializationSource == null)
                                ModifyGame(_game);
                        }
                        break;
                    }
                    case 2:
                    {
                        _initStep++;
                        _gameData = new HGameData(packet.ReadString());

                        if (_initializationSource == null)
                            ModifyGameData(_gameData);

                        break;
                    }
                    case 3:
                    {
                        _initStep++;
                        var connection = (ContractorProxy)_interceptor;

                        connection.RemoteEndPoint = HotelEndPoint.Parse(
                            packet.ReadString(), packet.ReadShort());

                        break;
                    }
                    case 4:
                    case 5:
                    {
                        var destination = (HDestination)(packet.Header - 4);

                        string stamp = packet.ReadString();
                        int step = packet.ReadInteger();
                        bool isBlocked = packet.ReadBoolean();
                        int dataLength = packet.ReadInteger();
                        byte[] data = packet.ReadBytes(dataLength);
                        var interPacket = new HMessage(data, destination);

                        var args = new DataInterceptedEventArgs(interPacket, step, null);
                        try
                        {
                            if (destination == HDestination.Client)
                            {
                                HandleIncoming(args);
                                Triggers?.HandleIncoming(args);
                            }
                            else
                            {
                                HandleOutgoing(args);
                                Triggers?.HandleOutgoing(args);
                            }
                        }
                        finally
                        {
                            await SendInterceptedDataResponseAsync(
                                stamp, args).ConfigureAwait(false);
                        }
                        break;
                    }
                }
                #endregion

                if (_initStep == 4 &&
                    _initializationSource != null)
                {
                    _initializationSource.SetResult(true);
                }
            }
            finally
            {
                Task receiveRemContDataTask =
                    ReceiveRemoteContractorDataAsync();
            }
        }

        private HNode GetRemoteContractor()
        {
            HNode remoteContractor = null;
            do
            {
                try
                {
                    remoteContractor = new HNode();
                    remoteContractor.ConnectAsync("127.0.0.1", 8055).Wait();
                }
                catch
                {
                    DialogResult result = MessageBox.Show($"Unable to establish connection with the remote contractor on port 8055.",
                        "Tangine ~ Warning!", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Warning);

                    switch (result)
                    {
                        case DialogResult.Ignore: return null;
                        case DialogResult.Abort: Environment.Exit(0); break;
                    }
                }
            }
            while (remoteContractor == null);
            return remoteContractor;
        }
        private Task SendInterceptedDataResponseAsync(string stamp, DataInterceptedEventArgs args)
        {
            var interceptedData = new HMessage((ushort)(args.Message.Destination + 6));
            interceptedData.WriteString(stamp);
            interceptedData.WriteInteger(args.Step);
            interceptedData.WriteBoolean(args.IsBlocked);
            interceptedData.WriteInteger(args.Message.Length + 4);
            interceptedData.WriteBytes(args.Message.ToBytes());

            return _remoteContractor.SendAsync(interceptedData.ToBytes());
        }
    }
}