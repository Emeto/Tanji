using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Tanji.Helpers;

using Tangine.Habbo;

using Eavesdrop;

using FlashInspect;

using Microsoft.Win32;

using Sulakore.Habbo.Web;
using Sulakore.Communication;
using Sulakore.Protocol.Encryption;

namespace Tanji.Services.Connection
{
    public class ConnectionViewModel : ObservableObject, IReceiver, IHaltable
    {
        #region Status Constants
        private const string STANDING_BY = "Standing By...";

        private const string INTERCEPTING_GAME_DATA = "Intercepting Game Data...";
        private const string INTERCEPTING_CONNECTION = "Intercepting Connection...";
        private const string INTERCEPTING_GAME_CLIENT = "Intercepting Game Client...";

        private const string MODIFYING_GAME_CLIENT = "Modifying Game Client...";
        private const string INJECTING_GAME_CLIENT = "Injecting Game Client...";

        private const string COMPRESSING_GAME_CLIENT = "Compressing Game Client...";
        private const string DECOMPRESSING_GAME_CLIENT = "Decompressing Game Client...";

        private const string ASSEMBLING_GAME_CLIENT = "Assembling Game Client...";
        private const string DISASSEMBLING_GAME_CLIENT = "Disassembling Game Client...";
        #endregion

        private Task<bool> _loadCustomClientTask;
        private readonly OpenFileDialog _openClientDialog;
        private readonly SaveFileDialog _saveCertificateDialog;

        public bool IsConnecting
        {
            get
            {
                return (Status != STANDING_BY ||
                    ((!_loadCustomClientTask?.IsCompleted) ?? false));
            }
        }

        private string _status = STANDING_BY;
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                RaiseOnPropertyChanged();
                RaiseOnPropertyChanged(nameof(IsConnecting));
            }
        }

        public ushort _proxyPort = 8282;
        public ushort ProxyPort
        {
            get { return _proxyPort; }
            set
            {
                _proxyPort = value;
                RaiseOnPropertyChanged();
            }
        }

        private string _customClientPath = string.Empty;
        public string CustomClientPath
        {
            get { return _customClientPath; }
            set
            {
                _customClientPath = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isAutomaticServerExtraction = true;
        public bool IsAutomaticServerExtraction
        {
            get { return _isAutomaticServerExtraction; }
            set
            {
                _isAutomaticServerExtraction = value;
                RaiseOnPropertyChanged();
            }
        }

        private HotelEndPoint _hotelServer = null;
        public HotelEndPoint HotelServer
        {
            get { return _hotelServer; }
            set
            {
                _hotelServer = value;
                RaiseOnPropertyChanged();
            }
        }

        public Command CancelCommand { get; }
        public Command ConnectCommand { get; }
        public AsyncCommand BrowseAsyncCommand { get; }

        public Command ExportRootCertificateCommand { get; }
        public Command DestroySignedCertificatesCommand { get; }

        public ConnectionViewModel()
        {
            App.Haltables.Add(this);
            App.Receivers.Add(4, this);

            _saveCertificateDialog = new SaveFileDialog();
            _saveCertificateDialog.DefaultExt = "cer";
            _saveCertificateDialog.FileName = "Eavesdrop Authority";
            _saveCertificateDialog.Title = "Tanji - Export Root Certificate";
            _saveCertificateDialog.Filter = "X.509 Certificate (*.cer, *.crt)|*.cer;*.crt";

            _openClientDialog = new OpenFileDialog();
            _openClientDialog.Title = "Tanji - Select Custom Client";
            _openClientDialog.Filter = "Shockwave Flash File (*.swf)|*.swf";

            ConnectCommand = new Command(CanConnect, Connect);
            CancelCommand = new Command(CanConnect, Cancel, true);
            BrowseAsyncCommand = new AsyncCommand(CanConnect, BrowseAsync);

            ExportRootCertificateCommand = new Command(AlwaysTrue, ExportRootCertificate);
            DestroySignedCertificatesCommand = new Command(CanConnect, DestroySignedCertificates);
        }

        private void InjectGameClient(object sender, RequestInterceptedEventArgs e)
        {
            if (e.Request.RequestUri.Query.EndsWith("-Tanji"))
            {
                Eavesdropper.RequestIntercepted -= InjectGameClient;
                if (App.Game == null) // Check if this requested resource exists locally.
                {
                    Uri remoteUrl = e.Request.RequestUri;

                    string clientPath = Path.GetFullPath(
                        $"Modified Clients/{remoteUrl.Host}/{remoteUrl.LocalPath}");

                    if (!File.Exists(clientPath) || !TryLoadGameClientAsync(clientPath).Result)
                    {
                        Status = INTERCEPTING_GAME_CLIENT;
                        Eavesdropper.ResponseIntercepted += InterceptGameClient;
                        return;
                    }
                }
                e.Request = WebRequest.Create(new Uri(App.Game.Location));

                TerminateProxy();
                InterceptConnection();
            }
        }
        private void InterceptGameData(object sender, ResponseInterceptedEventArgs e)
        {
            if (Status != INTERCEPTING_GAME_DATA) return;
            if (!e.Response.ContentType.Contains("text/html")) return;

            string body = Encoding.UTF8.GetString(e.Payload);
            if (!body.Contains("info.host") && !body.Contains("info.port")) return;

            // Response contains possible game data, stop intercepting for more.
            Eavesdropper.ResponseIntercepted -= InterceptGameData;

            App.GameData.Update(body);
            if (IsAutomaticServerExtraction &&
                !TryExtractHotelServer(App.GameData))
            {
                Cancel(sender);
                return;
            }

            Task<bool> tryLoadGameClientTask = null;
            if (App.GameData.ContainsVariable("flash.client.url"))
            {
                string flashClientUrl = App.GameData["flash.client.url"]
                    .Replace("http:", string.Empty)
                    .Replace("https:", string.Empty);

                MatchCollection matches = Regex.Matches(
                    body, "//.*?\\.swf", RegexOptions.Multiline);

                foreach (Match swfURLMatch in matches)
                {
                    string url = swfURLMatch.Value;
                    if (url.Contains(flashClientUrl))
                    {
                        body = body.Replace(url,
                            $"{url}?{DateTime.Now.Ticks}-Tanji");

                        var localPath = Path.GetFullPath("Modified Clients" + url);
                        if (App.Game?.Location != localPath)
                        {
                            tryLoadGameClientTask =
                                TryLoadGameClientAsync(localPath);
                        }
                        break;
                    }
                }
            }
            else
            {
                body = body.Replace(".swf",
                    $".swf?{DateTime.Now.Ticks}-Tanji");
            }

            body = body.Replace(App.GameData.InfoHost, "127.0.0.1");
            e.Payload = Encoding.UTF8.GetBytes(body);

            // True, if game was already loaded, or succeeded in loading from cache.
            // If this is null, that means the flash client url was not parsed properly, attempt to check if the next .swf request exists locally.
            if (tryLoadGameClientTask?.Result ?? true)
            {
                Status = INJECTING_GAME_CLIENT;
                Eavesdropper.RequestIntercepted += InjectGameClient;
            }
            else
            {
                Status = INTERCEPTING_GAME_CLIENT;
                Eavesdropper.ResponseIntercepted += InterceptGameClient;
            }
        }
        private void InterceptGameClient(object sender, ResponseInterceptedEventArgs e)
        {
            if (!e.Response.ResponseUri.Query.EndsWith("-Tanji")) return;
            if (e.Response.ContentType != "application/x-shockwave-flash") return;
            Eavesdropper.ResponseIntercepted -= InterceptGameClient;

            Uri remoteUrl = e.Response.ResponseUri;
            string clientPath = Path.GetFullPath(
                $"Modified Clients/{remoteUrl.Host}/{remoteUrl.LocalPath}");

            string clientDirectory = Path.GetDirectoryName(clientPath);
            Directory.CreateDirectory(clientDirectory);

            TryLoadGameClientAsync(e.Payload).Wait();
            App.Game.Location = clientPath;

            Status = MODIFYING_GAME_CLIENT;
            App.Game.BypassDomainChecks();
            App.Game.InjectKeySharer(true);

            Status = ASSEMBLING_GAME_CLIENT;
            App.Game.Assemble();

#if DEBUG
            e.Payload = App.Game.ToArray();
            Task.Factory.StartNew(SaveAndCompress);
#else
            e.Payload = SaveAndCompress();
#endif

            TerminateProxy();
            InterceptConnection();
        }

        public void TerminateProxy()
        {
            Eavesdropper.Terminate();
            Eavesdropper.RequestIntercepted -= InjectGameClient;
            Eavesdropper.ResponseIntercepted -= InterceptGameData;
            Eavesdropper.ResponseIntercepted -= InterceptGameClient;
        }
        public bool TryExtractHotelServer(HGameData gameData)
        {
            ushort port = 0;
            HotelEndPoint endpoint = null;
            string[] ports = App.GameData.InfoPort.Split(',');
            if (ports.Length > 0 && ushort.TryParse(ports[0], out port))
            {
                if (HotelEndPoint.TryParse(
                    App.GameData.InfoHost, port, out endpoint))
                {
                    HotelServer = endpoint;
                }
            }
            return (HotelServer != null);
        }

        public Task<bool> TryLoadGameClientAsync(string path)
        {
            return TryLoadGameClientAsync(path, null);
        }
        public Task<bool> TryLoadGameClientAsync(byte[] data)
        {
            return TryLoadGameClientAsync(null, data);
        }
        private async Task<bool> TryLoadGameClientAsync(string path, byte[] data)
        {
            HGame game = null;
            if (!string.IsNullOrWhiteSpace(path))
            {
                path = Path.GetFullPath(path);
                if (!File.Exists(path))
                {
                    return false;
                }
                game = new HGame(path);
            }
            else if (data != null)
            {
                game = new HGame(data);
            }

            try
            {
                if (game.IsCompressed)
                {
                    Status = DECOMPRESSING_GAME_CLIENT;
                    await Task.Factory.StartNew(game.Decompress)
                        .ConfigureAwait(false);
                }

                if (!game.IsCompressed)
                {
                    Status = DISASSEMBLING_GAME_CLIENT;
                    await Task.Factory.StartNew(game.Disassemble)
                        .ConfigureAwait(false);
                }
                else return false;

                App.Game?.Dispose();
                App.Game = game;
                return true;
            }
            catch { return false; }
            finally
            {
                if (App.Game != game)
                    game.Dispose();
            }
        }

        private byte[] SaveAndCompress()
        {
            App.Game.Compression = CompressionType.ZLIB;
            byte[] compressed = App.Game.Compress();

            File.WriteAllBytes(App.Game.Location, compressed);
            return compressed;
        }
        private void InterceptConnection()
        {
            Status = INTERCEPTING_CONNECTION;

            Task connectTask =
                App.Interceptor.InterceptAsync(HotelServer);
        }

        private bool CanConnect(object obj)
        {
            return !IsConnecting;
        }

        private void Cancel(object obj)
        {
            TerminateProxy();
            App.Interceptor.Disconnect();

            if (IsAutomaticServerExtraction)
                HotelServer = null;

            Status = STANDING_BY;
        }
        private void Connect(object obj)
        {
            if (App.Interceptor.IsConnected)
            {
                if (MessageBox.Show("Are you sure you want to disconnect from the current session?\r\nDon't worry, all of your current options/settings will still be intact.",
                         "Tanji ~ Alert!",
                         MessageBoxButton.YesNo,
                         MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                {
                    App.Interceptor.Disconnect();
                }
            }

            if (!IsAutomaticServerExtraction && HotelServer == null)
            {
                MessageBoxResult result = MessageBox.Show(
                    "The endpoint(Host:Port) of the target hotel is required to proceed. Would you like to automatically attempt to extract the endpoint instead?",
                    "Tanji - Alert!", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);

                if (result == MessageBoxResult.Yes)
                {
                    IsAutomaticServerExtraction = true;
                }
                else return;
            }

            if (Eavesdropper.Certifier.CreateTrustedRootCertificate())
            {
                Eavesdropper.ResponseIntercepted += InterceptGameData;
                Eavesdropper.Initiate(ProxyPort);
                Status = INTERCEPTING_GAME_DATA;
            }
        }
        private async Task BrowseAsync(object obj)
        {
            _openClientDialog.FileName = string.Empty;
            if (_openClientDialog.ShowDialog() ?? false)
            {
                CustomClientPath = _openClientDialog.FileName;
                _loadCustomClientTask = TryLoadGameClientAsync(CustomClientPath);

                bool clientLoaded = await _loadCustomClientTask;
                Status = STANDING_BY;

                if (!clientLoaded)
                {
                    CustomClientPath = string.Empty;
                    MessageBox.Show("Unable to load the custom game client.",
                        "Tanji - Alert!", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            }
        }

        private void ExportRootCertificate(object obj)
        {
            if (Eavesdropper.Certifier.CreateTrustedRootCertificate() &&
                (_saveCertificateDialog.ShowDialog() ?? false))
            {
                Eavesdropper.Certifier
                    .ExportTrustedRootCertificate(_saveCertificateDialog.FileName);
            }
        }
        private void DestroySignedCertificates(object obj)
        {
            Eavesdropper.Certifier.DestroySignedCertificates();
        }

        #region IHaltable Implementation
        public void Halt()
        {
            Cancel(null);
        }
        public void Restore()
        {
            IsReceiving = true;
            Status = STANDING_BY;
        }
        #endregion
        #region IReceiver Implementation
        public bool IsReceiving { get; private set; }
        public void HandleOutgoing(DataInterceptedEventArgs e)
        {
            if (e.Message.Header == 4001)
            {
                string sharedKeyHex = e.Message.ReadString();
                if (sharedKeyHex.Length % 2 != 0)
                {
                    sharedKeyHex = ("0" + sharedKeyHex);
                }

                byte[] sharedKey = Enumerable.Range(0, sharedKeyHex.Length / 2)
                    .Select(x => Convert.ToByte(sharedKeyHex.Substring(x * 2, 2), 16))
                    .ToArray();

                App.Interceptor.Remote.Encrypter = new RC4(sharedKey);
                App.Interceptor.Remote.IsEncrypting = true;

                IsReceiving = false;
                e.IsBlocked = true;
            }
            else if (e.Step > 10)
            {
                IsReceiving = false;
            }
        }
        public void HandleIncoming(DataInterceptedEventArgs e)
        { }
        #endregion
    }
}