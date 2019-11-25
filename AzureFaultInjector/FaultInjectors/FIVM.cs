using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Threading;
using Kusto.Ingest;
using System.IO;
using Microsoft.Azure.Management.Network.Fluent;

namespace AzureFaultInjector
{
    public static class FIVM
    {
        static Random rnd = new Random();
        [FunctionName("FIVM")]
        public static void Run([TimerTrigger("0 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Fault Injector - VM function executed at: {DateTime.Now}");
            string subListStr = Environment.GetEnvironmentVariable("targetSubscriptionIDList");
            string rgListStr = Environment.GetEnvironmentVariable("targetRGList");
            string clientId = Environment.GetEnvironmentVariable("clientId");
            string tenantId = Environment.GetEnvironmentVariable("tenantId");
            string clientPwd = Environment.GetEnvironmentVariable("clientPassword");
            string ingestConn = Environment.GetEnvironmentVariable("ingestConn");
            log.LogInformation($"Params: SubscriptionIDList: {subListStr}; RGList: {rgListStr}");

            ServicePrincipalLoginInformation spi = new ServicePrincipalLoginInformation
            {
                ClientId = clientId,
                ClientSecret = clientPwd

            };
            AzureCredentials myAzCreds = new AzureCredentials(spi, tenantId, AzureEnvironment.AzureGlobalCloud);
            var myAz = Azure.Configure().Authenticate(myAzCreds).WithSubscription(subListStr);

            var ingestClient = KustoIngestFactory.CreateQueuedIngestClient(ingestConn);
            var ingestProps = new KustoIngestionProperties("annandale", "faultInjections");
            ingestProps.Format = Kusto.Data.Common.DataSourceFormat.csv;
            using (var memStream = new MemoryStream())
            {
                using (var sr = new StreamWriter(memStream))
                {
                    bool hasWrites = false;
                    foreach (string curRGName in rgListStr.Split(","))
                    {
                        log.LogInformation($"Iterating over RG {curRGName}");

                        var vmList = myAz.VirtualMachines.ListByResourceGroup(curRGName);
                        foreach (var curVM in vmList)
                        {
                            int nextRnd = rnd.Next(100);
                            log.LogInformation($"Got VM: {curVM.Name}. Will reboot if {nextRnd} is <= 20");
                            if (nextRnd <= 20)
                            {
                                hasWrites = true;
                                log.LogInformation($"Turning off VM for 3 minutes: {curVM.Name}");
                                sr.WriteLine($"{DateTime.UtcNow.ToString()}, {subListStr},{curRGName},{curVM.Name},{"VMFaultInjection"},{1}");
                                curVM.PowerOff();
                                Thread.Sleep(180000);
                                log.LogInformation($"Turning on VM: {curVM.Name}");
                                sr.WriteLine($"{DateTime.UtcNow.ToString()}, {subListStr},{curRGName},{curVM.Name},{"VMFaultInjection"},{0}");
                                curVM.StartAsync();
                                Thread.Sleep(60000);
                            }
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

        }
    }
}
