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
            MountDrive();

            long iterations = 0;
            Setup();

            while (true)
            {
                try
                {

                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, ex.Message, ++iterations);
                }
            }
        }

        private void MountDrive()
        {
            try
            {
                var psi = new ProcessStartInfo(@"net.exe")
                {
                    Arguments =
                        @"use Z: \\t7minecraftstate.file.core.windows.net\state /u:AZURE\t7minecraftstate rzcZ2yF0VjKv21iBlepch5lBBKB2y7Dn8GupduvJuI2ZsjyaKZOjdya3T304EN8bqf8nkEi1QIoh/bTALJhvmw==",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, ex.Message);
            }
        }

        private void Setup()
        {
            var telemetryconfiguration = new TelemetryConfiguration();
            var perfCollectorModule = new PerformanceCollectorModule();
            perfCollectorModule.Counters.Add(new PerformanceCounterCollectionRequest(@"\Memory\Committed Bytes", "used memory"));
            perfCollectorModule.Counters.Add(new PerformanceCounterCollectionRequest(@"\Cpu\% Processor Time", "used cpu"));
            perfCollectorModule.Initialize(telemetryconfiguration);
            telemetry = new TelemetryClient(telemetryconfiguration);
            telemetry.Context.InstrumentationKey = "4e8a09ff-e8eb-470d-8633-71414e89a701";
            telemetry.TrackEvent("This node started");
        }

        private static TelemetryClient telemetry = new TelemetryClient();
        private static DockerClient client = new DockerClientConfiguration(new Uri("tcp://localhost:2375")).CreateClient();

        static async Task MainAsync(string[] args)
        { // create an instance of the socket. In this case i've used the .Net 4.5 object defined in the project
            INetworkSocket socket = new RconSocket();
            IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
                new ContainersListParameters()
                {
                    Limit = 10,
                });
            var containerMetric = "";
            foreach (var container in containers)
            {
                containerMetric += string.Format(" & image: {0} state: {1};", container.Image, container.State);
            }

            // create the RconMessenger instance and inject the socket
            RconMessenger messenger = new RconMessenger(socket);

            // initiate the connection with the remote server
            bool isConnected = await messenger.ConnectAsync("localhost", 25575);
            await messenger.AuthenticateAsync("cheesesteakjimmys");

            // if we fall here, we're good to go! from this point on the connection is authenticated and you can send commands
            // to the server
            var response = await messenger.ExecuteCommandAsync("/list");

            var data = response.Substring(10, 5).Trim().Split('/');
            var _maxPlayers = int.Parse(data[1]);
            var _players = int.Parse(data[0]);

            Console.WriteLine(response.Substring(10, 5).Trim());

            telemetry.TrackMetric(new MetricTelemetry("Nr of players", _players));
            telemetry.TrackMetric(new MetricTelemetry("Nr of maximum players", _maxPlayers));
            telemetry.TrackEvent("Container is " + containerMetric);
        }
    }
}
