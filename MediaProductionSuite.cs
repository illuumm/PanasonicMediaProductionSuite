using Crestron.SimplSharp;
using Independentsoft.Exchange;
using Independentsoft.Json.Parser;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanasonicMediaProductionSuite
{
    public class ClientStatusArgs : EventArgs
    {
        public short Open { get; set; }

        public ClientStatusArgs() { }
    }

    public class ReturnFramingStateArgs : EventArgs
    {
        public ushort AutoFramingEnabled { get; set; }
        public ushort AutoFramingStarted { get; set; }
        public ushort AutoStartAreaOn { get; set; }
    }

    public class PresetRecallArgs : EventArgs
    {
        public ushort Preset { get; set; }
    }

    public delegate void ClientStatusDelegate(object sender, ClientStatusArgs e);
    public delegate void ReturnFramingStateDelegate(object sender, ReturnFramingStateArgs e);
    public delegate void PresetRecallDelegate(object sender, PresetRecallArgs e);

    public class MediaProductionSuite
    {
        private bool ClientInitialized;
        private ushort AutoFramingEnableState, AutoFramingStartState, AutoStartAreaState;
        private ushort CameraId;
        private string Host;
        private readonly Http Client = new Http();

        public event ClientStatusDelegate OnClientStatusChanged;
        public event ReturnFramingStateDelegate OnReturnFramingState;
        public event PresetRecallDelegate OnPresetRecall;

        public MediaProductionSuite() { }

        public void EnableDebug()
        {
            Debugger.DebugEnable = true;
        }

        public void DisableDebug()
        {
            Debugger.DebugEnable = false;
        }

        public void Init(string host)
        {
            Host = host;
            Client.OnClientInit += Client_OnClientInit;
            Client.OnMessageReceived += Client_OnMessageReceived;
        }

        public void SetCameraId(ushort id)
        {
            CameraId = id;
        }

        public void OpenClient()
        {
            Client.Init(Host);
        }

        public void GetCommand(string uri)
        {
            if (!ClientInitialized)
            {
                OpenClient();
            }
            Client.GetUri(uri);
        }

        public void GetFramingState()
        {
            GetCommand($"cgi-bin/auto_framing?cmd=FramingState&id={CameraId}");
        }

        public void EnableAutoFraming()
        {
            GetCommand($"cgi-bin/auto_framing?cmd=FramingEnable&id={CameraId}&enable=on");
        }

        public void DisableAutoFraming()
        {
            GetCommand($"cgi-bin/auto_framing?cmd=FramingEnable&id={CameraId}&enable=off");
        }

        public void StartAutoFraming()
        {
            GetCommand($"cgi-bin/auto_framing?cmd=FramingStartStop&id={CameraId}&process=start");
        }

        public void StopAutoFraming()
        {
            GetCommand($"cgi-bin/auto_framing?cmd=FramingStartStop&id={CameraId}&process=stop");
        }

        public void AutoStartAreaOn(ushort x, ushort y, ushort width, ushort height)
        {
            if (x + width > 1920 || y + height > 1080)
            {
                CrestronConsole.PrintLine($"{this}, {nameof(AutoStartAreaOn)} parameters out of range. Must fit within a 1920x1080 frame");
                return;
            }

            if (x == 0 && y == 0 && width == 0 && height == 0)
            {
                GetCommand($"cgi-bin/auto_framing?cmd=AutoStartArea&id={CameraId}&mode=1");
            }
            else
            {
                GetCommand($"cgi-bin/auto_framing?cmd=AutoStartArea&id={CameraId}&mode=1&area_x={x}&area_y={y}&area_width={width}&area_height={height}");
            }
        }

        public void AutoStartAreaOff()
        {
            GetCommand($"cgi-bin/auto_framing?cmd=AutoStartArea&id={CameraId}&mode=0");
        }

        public void RecallAdvancedPreset(ushort preset)
        {
            GetCommand($"cgi-bin/auto_framing?cmd=Preset&id={CameraId}&mode=recall&preset_num={preset}");
        }

        public void CloseClient()
        {
            Client.CloseClient();
        }

        private void Client_OnClientInit(object sender, ClientInitArgs e)
        {
            ClientInitialized = e.Initialized;
            ClientStatusArgs clientStatusArgs = new ClientStatusArgs();
            clientStatusArgs.Open = e.Initialized ? (short)1 : (short)0;
            OnClientStatusChanged?.Invoke(this, clientStatusArgs);
        }

        private void Client_OnMessageReceived(object sender, MessageReceivedArgs e)
        {
            try
            {
                CommandResponse commandResponse = JsonConvert.DeserializeObject<CommandResponse>(e.EventMessage);

                if (commandResponse.Response != "ack")
                {
                    Debugger.Log(this, $"{nameof(Client_OnMessageReceived)}", $"Command: {commandResponse.Command}, Response: {commandResponse.Response}, NACKDetail: {commandResponse.NACKDetail}");
                    return;
                }

                string[] parameters = commandResponse.Parameter.Split('&');

                switch (commandResponse.Command)
                {
                    case "FramingState":
                        {
                            FramingStateRoot framingStateRoot = JsonConvert.DeserializeObject<FramingStateRoot>(e.EventMessage);
                            AutoFramingEnableState = (ushort)framingStateRoot.FramingState[0].FramingEnable;
                            AutoFramingStartState = (ushort)framingStateRoot.FramingState[0].FramingStartStop;
                            AutoStartAreaState = (ushort)framingStateRoot.FramingState[0].auto_start_area.AutoStartAreaEnable;
                            ReturnFramingResponse();
                            break;
                        }
                    case "FramingEnable": //{"Command":"FramingEnable","Parameter":"&id=2&enable=off","Response":"ack"}
                        {
                            if (commandResponse.Parameter.Contains("enable=on"))
                            {
                                AutoFramingEnableState = 1;
                            }
                            else if (commandResponse.Parameter.Contains("enable=off"))
                            {
                                AutoFramingEnableState = 0;
                            }
                            ReturnFramingResponse();
                            break;
                        }
                    case "FramingStartStop": //{"Command":"FramingStartStop","Parameter":"&id=2&process=start","Response":"ack"}
                        {
                            if (commandResponse.Parameter.Contains("process=start"))
                            {
                                AutoFramingStartState = 1;
                            }
                            else if (commandResponse.Parameter.Contains("process=stop"))
                            {
                                AutoFramingStartState = 0;
                            }
                            ReturnFramingResponse();
                            break;
                        }
                    case "AutoStartArea": //{"Command":"AutoStartArea","Parameter":"&id=2&mode=1","Response":"ack"}
                        {
                            if (commandResponse.Parameter.Contains("mode=1"))
                            {
                                AutoStartAreaState = 1;
                            }
                            else if (commandResponse.Parameter.Contains("mode=0"))
                            {
                                AutoStartAreaState = 0;
                            }
                            ReturnFramingResponse();
                            break;
                        }
                    case "Preset": //{"Command":"Preset","Parameter":"&id=2&mode=recall&preset_num=1","Response":"ack"}
                        {
                            if (parameters[2].Contains("recall") && parameters[3].Contains("preset_num="))
                            {
                                string preset = parameters[3].Remove(0, 11);
                                bool result = ushort.TryParse(preset, out ushort x);
                                
                                if (result)
                                {
                                    OnPresetRecall?.Invoke(this, new PresetRecallArgs { Preset = x });
                                }
                                else
                                {
                                    CrestronConsole.PrintLine($"{this}.{nameof(Client_OnMessageReceived)}: Failed to parse preset number from {parameters[3]}");
                                }
                            }
                            break;
                        }
                    default:
                        {
                            Debugger.Log(this, $"{nameof(Client_OnMessageReceived)}", $"Command: {commandResponse.Command}, Parameter: {commandResponse.Parameter}, Response: {commandResponse.Response}");
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine($"{this} {nameof(Client_OnMessageReceived)} error: {ex.Message}");
                CrestronConsole.PrintLine($"{this} {nameof(Client_OnMessageReceived)} error InnersException: {ex.InnerException}");
                CrestronConsole.PrintLine($"{this} {nameof(Client_OnMessageReceived)} error StackTrace: {ex.StackTrace}");
            }
        }

        private void ReturnFramingResponse()
        {
            ReturnFramingStateArgs returnFramingStateArgs = new ReturnFramingStateArgs
            {
                AutoFramingEnabled = AutoFramingEnableState,
                AutoFramingStarted = AutoFramingStartState,
                AutoStartAreaOn = AutoStartAreaState
            };
            OnReturnFramingState?.Invoke(this, returnFramingStateArgs);
        }
    }
}
