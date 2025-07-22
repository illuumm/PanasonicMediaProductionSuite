using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Crestron.SimplSharp;

namespace PanasonicMediaProductionSuite
{
    public class Http
    {
        public ushort DebugEnable;

        private HttpClient Client;
        private Uri BaseUri;
        private readonly object SyncLock = new object();

        public event MessageReceivedDelegate OnMessageReceived;
        public event ClientInitDelegate OnClientInit;

        public void Init(string host)
        {
            lock (SyncLock)
            {
                if (Client != null)
                {
                    CloseClient();
                }

                string baseUriString = $"http://{host}:1338";

                if (Uri.TryCreate(baseUriString, UriKind.Absolute, out Uri createdUri))
                {
                    BaseUri = createdUri;

                    var handler = new HttpClientHandler
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                    };

                    Client = new HttpClient(handler)
                    {
                        BaseAddress = BaseUri,
                        Timeout = TimeSpan.FromSeconds(120)
                    };

                    OnClientInit?.Invoke(this, new ClientInitArgs { Initialized = true });
                    Debugger.Log(this, "HttpClient initialized, using base uri", $"{BaseUri.AbsoluteUri}");
                }
                else
                {
                    CrestronConsole.PrintLine($"HttpClient Init exception: failed to create base uri from: {baseUriString}");
                    throw new UriFormatException($"Failed to create base uri from {baseUriString}");
                }
            }
        }

        public void CloseClient()
        {
            lock (SyncLock)
            {
                if (Client == null)
                {
                    return;
                }

                Client.Dispose();
                Client = null;
                OnClientInit?.Invoke(this, new ClientInitArgs { Initialized = false });
                Debugger.Log(this, $"{nameof(CloseClient)}", "Client closed");
            }
        }

        public void GetUri(string uri)
        {
            try
            {
                if (Client == null)
                {
                    Debugger.Log(this, $"{nameof(GetUri)} failed", "Http not initialized");
                    return;
                }

                Task.Run(async () => await HandleGet(uri)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine($"GetUri exception message: {ex.Message}");
                CrestronConsole.PrintLine($"GetUri InnerException: {ex.InnerException}");
                CrestronConsole.PrintLine($"GetUri exception StackTrace: {ex.StackTrace}");
                CrestronConsole.PrintLine($"uri: {uri}");
            }
        }

        private async Task HandleGet(string relativeUri)
        {
            if (!Uri.TryCreate(relativeUri, UriKind.RelativeOrAbsolute, out Uri uri))
            {
                CrestronConsole.PrintLine($"Failed to create uri. Relative: {relativeUri}.");
                return;
            }

            try
            {
                Debugger.Log(this, $"{nameof(HandleGet)}", $"get uri: {uri}");
                string responseBody = await Client.GetStringAsync(uri).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(responseBody))
                {
                    Debugger.Log(this, $"{nameof(HandleGet)}", $"responseBody: {responseBody}");
                    var args = new MessageReceivedArgs { EventMessage = responseBody };

                    var handlers = OnMessageReceived;
                    if (handlers != null)
                    {
                        foreach (var handler in handlers.GetInvocationList())
                        {
                            try
                            {
                                handler.DynamicInvoke(this, args);
                            }
                            catch (Exception ex)
                            {
                                CrestronConsole.PrintLine($"Event handler exception: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                CrestronConsole.PrintLine($"Http client get request exception: {ex.Message}");
                CrestronConsole.PrintLine($"uri: {uri}");
                CloseClient();
            }
            catch (TaskCanceledException ex)
            {
                CrestronConsole.PrintLine($"Http client get request timed out: {ex.Message}");
                CrestronConsole.PrintLine($"uri: {uri}");
                CloseClient();
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine($"Http client exception: {ex.Message}");
                CrestronConsole.PrintLine($"uri: {uri}");
                CloseClient();
            }
        }
    }

    public class MessageReceivedArgs : EventArgs
    {
        public string EventMessage { get; set; }

        public MessageReceivedArgs() { }
    }

    public class ClientInitArgs : EventArgs
    {
        public bool Initialized { get; set; }

        public ClientInitArgs() { }
    }

    public delegate void MessageReceivedDelegate(object sender, MessageReceivedArgs e);
    public delegate void ClientInitDelegate(object sender, ClientInitArgs e);
}
