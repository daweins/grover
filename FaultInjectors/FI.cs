using System;
using System.Collections.Generic;
using System.Text;
using Kusto.Ingest;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Azure.Management.ResourceGraph.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Threading.Tasks;


namespace AzureFaultInjector
{
    public abstract class FI
    {

        public static Random rnd = new Random();


        protected string curSubName;
        protected string curTarget;
        protected Microsoft.Azure.Management.Fluent.IAzure myAzure;
        protected ILogger log;
        protected IKustoQueuedIngestClient ingestClient;
        protected KustoIngestionProperties ingestProps;


        // Must be defined by subclass
        // TODO: Enforce this
        protected string myTargetType = "Unknown";
        protected Microsoft.Azure.Management.ResourceManager.Fluent.Core.IResource myResource = null;


        abstract protected bool turnOn();
        abstract protected bool turnOff(int numMinutes);

        // This should be overridden by most implementations. C# doesn't have abstract statics, or I'd use that. 
        static public List<ScheduledOperation> getSampleSchedule(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, ILogger log)
        {
            return new List<ScheduledOperation>();
        }
        // This should be overridden by most implementations. C# doesn't have abstract statics, or I'd use that. 
        static public List<ScheduledOperation> killAZ(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, int azToKill, ILogger log)
        {
            return new List<ScheduledOperation>();
        }
        static public List<ScheduledOperation> killRegion(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, string regionToKill, ILogger log)
        {
            return new List<ScheduledOperation>();
        }

            protected FI(ILogger iLog, Microsoft.Azure.Management.Fluent.IAzure iAzure, string iTarget)
        {
            curTarget = iTarget;
            log = iLog;
            curSubName = iAzure.SubscriptionId;

            string ingestConn   = Environment.GetEnvironmentVariable("ingestConn");
            string ingestDB     = Environment.GetEnvironmentVariable("ingestDB");
            string ingestTable  = Environment.GetEnvironmentVariable("ingestTable");

            ingestClient = KustoIngestFactory.CreateQueuedIngestClient(ingestConn);
            ingestProps = new KustoIngestionProperties(ingestDB, ingestTable);
            ingestProps.Format = Kusto.Data.Common.DataSourceFormat.csv;
        }

        // Used to allow for easy "plugin" extension
        static public IEnumerable<Type> getSubTypes()
        {            
                return Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(FI).IsAssignableFrom(t));
        }


        public bool processOp(string operation)
        {
            switch(operation)
            {
                case "on":
                    this.turnOn();
                    return true;
                case "off":
                    this.turnOff(5);
                    return true;
                default:
                    log.LogError($"Unknown op: {operation} for {this.ToString()}");
                    break;
            }
            return false;
        }

       /* public async Task<bool> Fuzz(int pct)
        {
            try
            {
                log.LogInformation($"{myTargetType} Fuzzer run against {curSubName} -> {curRGName} start");

                using (var memStream = new MemoryStream())
                {
                    using (var sr = new StreamWriter(memStream))
                    {
                        bool hasWrites = false;

                        
                        foreach (Microsoft.Azure.Management.ResourceManager.Fluent.Core.IResource curResource in myResourceCollection)
                        {
                            int nextRnd = rnd.Next(100);
                            log.LogInformation($"Got {myTargetType}: {curResource.Name}. Will reboot if {nextRnd} is <= {pct}");
                            if (nextRnd <= pct)
                            {
                                hasWrites = true;
                                log.LogInformation($"Turning off {myTargetType} for 3 minutes: {curResource.Name}");
                                sr.WriteLine($"{DateTime.UtcNow.ToString()}, {curSubName},{curRGName},{curResource.Name},{myTargetType+"FaultInjection"},{1}");
                                bool result = turnOff(curResource);
                                log.LogInformation($"Turn Off Result: {result}");
                                // Sleep for a minimum of 3 minutes - an implementor can wait longer if they choose to add
                                log.LogInformation($"Sleeping...");
                                Thread.Sleep(TimeSpan.FromMinutes(3));
                                log.LogInformation($"Turning on {myTargetType}: {curResource.Name}");
                                sr.WriteLine($"{DateTime.UtcNow.ToString()}, {curSubName},{curRGName},{curResource.Name},{myTargetType + "FaultInjection"},{0}");
                                result = turnOn(curResource);
                                log.LogInformation($"Turn On Result: {result}");
                            }
                        }


                        if (hasWrites)
                        {
                            sr.Flush();
                            memStream.Seek(0, SeekOrigin.Begin);
                            ingestClient.IngestFromStream(memStream, ingestProps);
                        }
                    }
                }
                log.LogInformation($"VM Fuzzer run against {curSubName} -> {curRGName} complete");
                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error in VM Fuzzer: {err}");
                return false;
            }
        }
        */


    }



}
