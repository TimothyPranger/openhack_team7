using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.Diagnostics.EventFlow.ServiceFabric;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using RconSharp;
using RconSharp.Net45;

namespace MonitoringEventFlowService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class MonitoringEventFlowService : StatelessService
    {
        public MonitoringEventFlowService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.
            //MountDrive();

            long iterations = 0;
            //Setup();
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        //await MainAsync();
                        Thread.Sleep(1000);

                        //string sSource;
                        //string sLog;
                        //string sEvent;

                        //sSource = "Timothy";
                        //sLog = "Application";
                        //sEvent = "Ik run :')";

                        //if (!EventLog.SourceExists(sSource))
                        //    EventLog.CreateEventSource(sSource, sLog);

                        //EventLog.WriteEntry(sSource, sEvent);
                        //EventLog.WriteEntry(sSource, sEvent,
                        //    EventLogEntryType.Warning, 234);
                    }
                    catch (Exception e)
                    {
                        string sSource;
                        string sLog;
                        string sEvent;

                        sSource = "Timothy";
                        sLog = "Application";
                        sEvent = Bubble(e);

                        if (!EventLog.SourceExists(sSource))
                            EventLog.CreateEventSource(sSource, sLog);

                        EventLog.WriteEntry(sSource, sEvent);
                        EventLog.WriteEntry(sSource, sEvent,
                            EventLogEntryType.Error, 234);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToEventLog(Bubble(ex));
                ServiceEventSource.Current.ServiceMessage(this.Context, ex.Message, ++iterations);
            }
        }

        private static void WriteToEventLog(string message)
        {
            string sSource;
            string sLog;
            string sEvent;

            sSource = "Timothy";
            sLog = "Application";
            sEvent = message;

            if (!EventLog.SourceExists(sSource))
                EventLog.CreateEventSource(sSource, sLog);

            EventLog.WriteEntry(sSource, sEvent);
            EventLog.WriteEntry(sSource, sEvent,
                EventLogEntryType.Error, 234);
        }

        private static string Bubble(Exception ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }
            return string.Format("ex: {0}, stack:{1};  {2}", ex.Message, ex.StackTrace, Bubble(ex.InnerException));
        }

        private void MountDrive()
        {
            try
            {
                //Process.Start("cmd.exe",
                //    @"/C net use Z: \\t7minecraftstate.file.core.windows.net\state /u:AZURE\t7minecraftstate rzcZ2yF0VjKv21iBlepch5lBBKB2y7Dn8GupduvJuI2ZsjyaKZOjdya3T304EN8bqf8nkEi1QIoh/bTALJhvmw==");

            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, ex.Message);
            }
        }

        private void Setup()
        {
            try
            {
                TelemetryConfiguration telemetryconfiguration = new TelemetryConfiguration();
                telemetryconfiguration.InstrumentationKey = InstrumentationKey;

                PerformanceCollectorModule perfCollectorModule = new PerformanceCollectorModule();
                perfCollectorModule.Counters.Add(new PerformanceCounterCollectionRequest(@"\Memory\Committed Bytes", "used memory"));
                perfCollectorModule.Counters.Add(new PerformanceCounterCollectionRequest(@"\Cpu\% Processor Time", "used cpu"));
                perfCollectorModule.Initialize(telemetryconfiguration);
                telemetry = new TelemetryClient(telemetryconfiguration);

                //telemetry = new TelemetryClient();

                telemetry.Context.InstrumentationKey = InstrumentationKey;
                telemetry.TrackEvent("This node started");
            }
            catch (Exception e)
            {
                string sSource;
                string sLog;
                string sEvent;

                sSource = "Timothy";
                sLog = "Application";
                sEvent = Bubble(e);

                if (!EventLog.SourceExists(sSource))
                    EventLog.CreateEventSource(sSource, sLog);

                EventLog.WriteEntry(sSource, sEvent);
                EventLog.WriteEntry(sSource, sEvent,
                    EventLogEntryType.Error, 234);
            }
        }

        private static TelemetryClient telemetry = new TelemetryClient();
        private static DockerClient client = new DockerClientConfiguration(new Uri("tcp://localhost:2375")).CreateClient();
        private const string InstrumentationKey = "4e8a09ff-e8eb-470d-8633-71414e89a701";

        static async Task MainAsync()
        { // create an instance of the socket. In this case i've used the .Net 4.5 object defined in the project
            INetworkSocket socket = new RconSocket();
            IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
                new ContainersListParameters()
                {
                    Limit = 10,
                });
            string containerMetric = "";
            string ipContainer = string.Empty;
            foreach (ContainerListResponse f in containers)
            {
                if (f.State.ToLowerInvariant() != "running") continue;
                foreach (KeyValuePair<string, EndpointSettings> item in f.NetworkSettings.Networks)
                {
                    ipContainer = item.Value.IPAddress;
                }
                containerMetric += string.Format(" & image: {0} state: {1};", f.Image, f.State);
            }

            // create the RconMessenger instance and inject the socket
            RconMessenger messenger = new RconMessenger(socket);

            // initiate the connection with the remote server
            bool isConnected = await messenger.ConnectAsync(ipContainer, 25575);
            await messenger.AuthenticateAsync("cheesesteakjimmys");

            // if we fall here, we're good to go! from this point on the connection is authenticated and you can send commands
            // to the server
            string response = await messenger.ExecuteCommandAsync("/list");

            string[] data = response.Substring(10, 5).Trim().Split('/');
            int _maxPlayers = int.Parse(data[1]);
            int _players = int.Parse(data[0]);

            Console.WriteLine(response.Substring(10, 5).Trim());

            telemetry.TrackMetric(new MetricTelemetry("Nr of players", _players));
            telemetry.TrackMetric(new MetricTelemetry("Nr of maximum players", _maxPlayers));
            telemetry.TrackEvent("Container is " + containerMetric);
        }
    }
}
