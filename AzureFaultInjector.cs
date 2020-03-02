using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Threading;
using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Azure.Management.ResourceGraph.Models;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using System.Linq;
using Microsoft.Azure.Cosmos;

namespace AzureFaultInjector
{
    public static class AzureFaultInjector
    {
        static Random rnd = new Random();


        [FunctionName("AzureFaultInjector_PopulateErrors")]
        public static void Run([TimerTrigger("0 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"Fault Injector - VM function started at: {DateTime.Now}");
                string subListStr   = Environment.GetEnvironmentVariable("targetSubscriptionIDList");
                string rgListStr    = Environment.GetEnvironmentVariable("targetRGList");
                string clientId     = Environment.GetEnvironmentVariable("clientId");
                string tenantId     = Environment.GetEnvironmentVariable("tenantId");
                string clientPwd    = Environment.GetEnvironmentVariable("clientPassword");
                string ingestConn   = Environment.GetEnvironmentVariable("ingestConn");
                int vmFuzzPct       = Convert.ToInt32(Environment.GetEnvironmentVariable("vmFuzzPct"));
                int nsgFuzzPct      = Convert.ToInt32(Environment.GetEnvironmentVariable("nsgFuzzPct"));
                string cosmosConn = Environment.GetEnvironmentVariable("cosmosConn");
                string cosmosDBName = Environment.GetEnvironmentVariable("cosmosDB");
                string cosmosScheduleContainerName = Environment.GetEnvironmentVariable("cosmosScheduleContainer");

                log.LogInformation($"Params: SubscriptionIDList: {subListStr}; RGList: {rgListStr}");

                // Build the sub list
                List<string> subList = new List<string>(subListStr.Split(","));
                log.LogInformation($"Found {subList.Count} subscriptions to target");
                ServicePrincipalLoginInformation spi = new ServicePrincipalLoginInformation
                {
                    ClientId = clientId,
                    ClientSecret = clientPwd

                };
                AzureCredentials myAzCreds = new AzureCredentials(spi, tenantId, AzureEnvironment.AzureGlobalCloud);
                Microsoft.Azure.Management.Fluent.IAzure myAz = Azure.Configure().Authenticate(myAzCreds).WithSubscription(subListStr);

//TODO: test if we need to add max object appSetting to allow > 100 items in the query
                ResourceGraphClient resourceGraphClient = new ResourceGraphClient(myAzCreds);

                // TODO: Move some of this into the FI objects
                // Get VMs
                string vmListQuery = @"Resources 
                | where tags.allowFaultInjection=~'true'
                | project id
                ";

                QueryResponse vmListResponse = resourceGraphClient.Resources(new QueryRequest(subList, vmListQuery));
                log.LogInformation($"Got VMs: {vmListResponse.Count}");
                JObject vmJObj = JObject.Parse(vmListResponse.Data.ToString());
                IList<string> vmIDList = vmJObj.SelectTokens("$.rows[*][0]").Select(s => (string)s).ToList();

                using(CosmosClient cosmosClient = new CosmosClient(cosmosConn))
                {
                    Database curDB = cosmosClient.GetDatabase(cosmosDBName);
                    Container cosmosScheduleContainer = curDB.GetContainer(cosmosScheduleContainerName);

                    // Sample create a VM schedule off/on
                    ScheduledOperation newOffOp = new ScheduledOperation(DateTime.Now, "Sample VM Off", "off", vmIDList[0]);
                    ScheduledOperation newOnOp  = new ScheduledOperation(DateTime.Now.AddMinutes(5), "Sample VM On" , "on", vmIDList[0]);

                    // Only create the Off if the On is scheduled successfully
                    ItemResponse<ScheduledOperation> onCreate = cosmosScheduleContainer.CreateItemAsync(newOnOp, newOnOp.getPartitionKey()).Result;
                    log.LogInformation($"Created Off schedule: {onCreate.StatusCode} : {newOnOp.ToString()}");
                    if ((int) onCreate.StatusCode >= 200 && (int)onCreate.StatusCode < 300)
                    {
                        ItemResponse<ScheduledOperation> offCreate = cosmosScheduleContainer.CreateItemAsync(newOffOp, newOffOp.getPartitionKey()).Result;
                        log.LogInformation($"Created Off schedule: {offCreate.StatusCode} : {newOffOp.ToString()}");

                    }
                    else
                    {
                        log.LogError($"Skipping creating the off schedule, as the on schedule failed: {onCreate.StatusCode}");
                    }


                }
                /*
                foreach (string curRGName in rgListStr.Split(","))
                {
                    log.LogInformation($"Iterating over RG {curRGName}");
                    FIVM vmFuzzer = new FIVM(log, myAz, curRGName, ingestConn, "annandale", "faultInjections");
                    vmFuzzer.Fuzz(1);

                    FINSG nsgFuzzer = new FINSG(log, myAz, curRGName, ingestConn, "annandale", "faultInjections");
                    nsgFuzzer.Fuzz(25);
                }
                */

                log.LogInformation($"Fault Injector - Error Populatorfunction finished at: {DateTime.Now}");

            }
            catch(Exception err)
            {
                log.LogError(err.ToString());
            }
        }
    }
}
