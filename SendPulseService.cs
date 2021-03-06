using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Timers;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using IniParser;
using IniParser.Model;
using RestSharp;

namespace PCPerfChecker
{
    public partial class SendPulseService : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
       
        public SendPulseService()
        {
            InitializeComponent();
            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("SendPulseService"))
            {
                System.Diagnostics.EventLog.CreateEventSource("SendPulseService", "SendPulseServiceLog");
            }

            eventLog1.Source = "SendPulseService";
            eventLog1.Log = "";
        }

        public void StartService(string[] args)
        {
            this.OnStart(args);
        }

        public void StopService()
        {
            this.OnStop();
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("PC Perf Checker service is starting...");
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 60000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            Timer timer = new Timer();
            timer.Interval = 60000;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        public async void OnTimer(object sender, ElapsedEventArgs args)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(AppDomain.CurrentDomain.BaseDirectory + "Configuration.ini");

            string url = $@"{ data["Options"]["AliveUrl"] }";
            string branch = $@"{ data["Options"]["BranchCode"] }";

            RestClient client = new RestClient(url);
            var request = new RestRequest(branch, Method.Post);

            try
            {
                var alive = await client.PostAsync(request);
                eventLog1.WriteEntry("Trying to send to URL: " + client.BuildUri(request));

                if (alive.IsSuccessful)
                {
                    eventLog1.WriteEntry("Succesfully sent alive status to server. Sent to URL: " + client.BuildUri(request));
                }
                else
                {
                    throw new Exception("Failed at REST Request!. Was trying to send to URL: " + client.BuildUri(request) + ". Reason: " + alive.ErrorMessage);
                }
            }
            catch (Exception ex)    
            {
                eventLog1.WriteEntry("Failed to send alive status to server.. Reason: " + ex.Message + " URL: " + client.BuildUri(request));
            }
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("PC Perf Checker Service is stopping...\n" +
                "Start the service manually or reboot the PC to restart the service");
        }
    }

    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };
}
