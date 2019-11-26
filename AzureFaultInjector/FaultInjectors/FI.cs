using System;
using System.Collections.Generic;
using System.Text;
using Kusto.Ingest;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace AzureFaultInjector
{
    public abstract class FI
    {

        public static Random rnd = new Random();


        protected string curSubName;
        protected string curRGName;
        protected Microsoft.Azure.Management.Fluent.IAzure myAzure;
        protected ILogger log;
        protected IKustoQueuedIngestClient ingestClient;
        protected KustoIngestionProperties ingestProps;


        // Must be defined by subclass
        // TODO: Enforce this
        protected string myTargetType = "Unknown";
        protected IEnumerable<Microsoft.Azure.Management.ResourceManager.Fluent.Core.IResource> myResourceCollection = null;


        abstract protected bool turnOn(Microsoft.Azure.Management.ResourceManager.Fluent.Core.IResource curResource);
        abstract protected bool turnOff(Microsoft.Azure.Management.ResourceManager.Fluent.Core.IResource curResource);



        protected FI(ILogger iLog, Microsoft.Azure.Management.Fluent.IAzure iAzure, string iRGName, string kustoConn, string kustoDBName, string kustoTableName)
        {
            curRGName = iRGName;
            log = iLog;
            curSubName = iAzure.SubscriptionId;

            ingestClient = KustoIngestFactory.CreateQueuedIngestClient(kustoConn);
            ingestProps = new KustoIngestionProperties(kustoDBName, kustoTableName);
            ingestProps.Format = Kusto.Data.Common.DataSourceFormat.csv;


        }


        public async Task<bool> Fuzz(int pct)
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



    }



}
