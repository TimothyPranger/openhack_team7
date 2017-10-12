using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.ServiceFabric;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Services.Runtime;

namespace MonitoringEventFlowService
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // The ServiceManifest.XML file defines one or more service type names.
                // Registering a service maps a service type name to a .NET type.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.
                // **** Instantiate log collection via EventFlow

                System.Diagnostics.Trace.TraceWarning("EventFlow is working!");

                ServiceRuntime.RegisterServiceAsync("MonitoringEventFlowServiceType",
                    context => new MonitoringEventFlowService(context)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id,
                    typeof(MonitoringEventFlowService).Name);

                // Prevents this host process from terminating so services keep running.
                Thread.Sleep(Timeout.Infinite);
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

                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        private static string Bubble (Exception ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }
            return string.Format("ex: {0}, stack:{1};  {2}", ex.Message, ex.StackTrace, Bubble(ex.InnerException));
        }
    }
}
