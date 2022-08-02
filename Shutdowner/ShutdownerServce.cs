using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using IniParser;
using IniParser.Model;

namespace Shutdowner
{
    partial class ShutdownerServce : ServiceBase
    {
        class ShutdownerArguments
        {
            public int Port = 9999;
            public string Passkey = "shutdowner passkey";
            public int MaxDelaySeconds = 5;
            public int MaxAheadSeconds = 5;

            public static ShutdownerArguments FromIni(string path)
            {
                var args = new ShutdownerArguments();
                if (!File.Exists(path))
                {
                    return args;
                }

                try
                {
                    var parser = new FileIniDataParser();
                    var data = parser.ReadFile(path)["Shutdowner"];
                    KeyData kd;

                    kd = data.GetKeyData("port");
                    if (kd != null) int.TryParse(kd.Value, out args.Port);

                    kd = data.GetKeyData("passkey");
                    if (kd != null) args.Passkey = kd.Value;

                    kd = data.GetKeyData("maxDelaySeconds");
                    if (kd != null) int.TryParse(kd.Value, out args.MaxDelaySeconds);

                    kd = data.GetKeyData("maxAheadSeconds");
                    if (kd != null) int.TryParse(kd.Value, out args.MaxAheadSeconds);
                }
                catch (Exception)
                {

                }

                return args;
            }
        }

        private ShutdownerArguments Arguments;

        private UdpListener Listener;
        private MessageReceiver Processor;

        public ShutdownerServce()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var executablePath = Assembly.GetEntryAssembly().Location;
            this.Arguments =
                ShutdownerArguments.FromIni(Path.Combine(Path.GetDirectoryName(executablePath) ?? ".\\", "config.ini"));

            this.Processor = new MessageReceiver(this.Arguments.Passkey, this.Arguments.MaxDelaySeconds, this.Arguments.MaxAheadSeconds);
            this.Processor.MessageAccepted += this.AcceptShutdown;

            this.Listener = new UdpListener(this.Arguments.Port);
            this.Listener.PacketReceived += this.Processor.ProcessPacket;
            this.Listener.StartListening();
        }

        protected override void OnStop()
        {
            this.Listener?.Dispose();
            this.Processor?.Dispose();
        }

        public void AcceptShutdown()
        {
            ShutdownInterops.PowerUtilities.ExitWindows(ShutdownInterops.ExitWindows.PowerOff,
                ShutdownInterops.ShutdownReason.MajorOther | ShutdownInterops.ShutdownReason.MinorOther | ShutdownInterops.ShutdownReason.FlagPlanned,
                true);
        }
    }
}
