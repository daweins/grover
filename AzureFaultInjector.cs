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


        [FunctionName("AzureFaultInjector")]
        public static void Run([TimerTrigger("0 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"Fault Injector - VM function started at: {DateTime.Now}");
                string subId = Environment.GetEnvironmentVariable("targetSubscription");
                string rgListStr = Environment.GetEnvironmentVariable("targetRGList");
                string clientId = Environment.GetEnvironmentVariable("clientId");
                string tenantId = Environment.GetEnvironmentVariable("tenantId");
                string clientPwd = Environment.GetEnvironmentVariable("clientPassword");
                int vmFuzzPct = Convert.ToInt32(Environment.GetEnvironmentVariable("vmFuzzPct"));
                int nsgFuzzPct = Convert.ToInt32(Environment.GetEnvironmentVariable("nsgFuzzPct"));
                string rgFilterTag = Environment.GetEnvironmentVariable("rgFilterTag");
                

                ServicePrincipalLoginInformation spi = new ServicePrincipalLoginInformation
                {
                    ClientId = clientId,
                    ClientSecret = clientPwd

                };
                AzureCredentials myAzCreds = new AzureCredentials(spi, tenantId, AzureEnvironment.AzureGlobalCloud);
                Microsoft.Azure.Management.Fluent.IAzure myAz = Azure.Configure().Authenticate(myAzCreds).WithSubscription(subId);


                // Centralize the filtering of target RGs for fault injection
                List<IResourceGroup> rgList = new List<IResourceGroup>(myAz.ResourceGroups.ListByTag(rgFilterTag, "true"));
                log.LogInformation($"Finding ResourceGroups in subscription {subId} with Tag {rgFilterTag} with a value of true");
                    
                doQueuePopulate(myAz, rgList, log);
                doQueueProcess(myAz, log);

                

                log.LogInformation($"Fault Injector finished at: {DateTime.Now}");

            }
            catch (Exception err)
            {
                log.LogError(err.ToString());
            }
        }

        static private void doQueuePopulate(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, ILogger log)
        {
            string cosmosConn = Environment.GetEnvironmentVariable("cosmosConn");
            string cosmosDBName = Environment.GetEnvironmentVariable("cosmosDB");
            string cosmosScheduleContainerName = Environment.GetEnvironmentVariable("cosmosScheduleContainer");

            //TODO: test if we need to add max object appSetting to allow > 100 items in the query
            // Moving back to a RG List ResourceGraphClient resourceGraphClient = new ResourceGraphClient(myAzCreds);

            // TODO: Move some of this into the FI objects
            List<ScheduledOperation> opsToAdd = new List<ScheduledOperation>();
            foreach(Type curFI in FI.getSubTypes())
            {
                try
                {
                    List<ScheduledOperation> newOps = (List<ScheduledOperation>)curFI.GetMethod("getSampleSchedule").Invoke(null, new object[] { myAz, rgList ,log });
                    opsToAdd.AddRange(newOps);
                }
                catch(Exception sampleError)
                {
                    log.LogWarning($"Warning: trouble creating a sample schedule for {curFI.Name}. It might not have implemented the getSampleSchedule static function (note - C# doesnt support static abstracts) : {sampleError}");
                }
            }
            log.LogInformation($"Sample Schedule: {opsToAdd.Count} items");
            ScheduledOperationHelper.addSchedule(opsToAdd,log);


        }

        static private void doQueueProcess(Microsoft.Azure.Management.Fluent.IAzure myAz, ILogger log)
        {
            string cosmosConn = Environment.GetEnvironmentVariable("cosmosConn");
            string cosmosDBName = Environment.GetEnvironmentVariable("cosmosDB");
            string cosmosScheduleContainerName = Environment.GetEnvironmentVariable("cosmosScheduleContainer");

            // Get list of ops to perform from cosmos
            using (CosmosClient cosmosClient = new CosmosClient(cosmosConn))
            {
                Database curDB = cosmosClient.GetDatabase(cosmosDBName);
                Container cosmosScheduleContainer = curDB.GetContainer(cosmosScheduleContainerName);
                
                QueryDefinition query = new QueryDefinition(
                    @"SELECT * 
                      FROM c
                      WHERE c.scheduleTimeTicks < @filterTime ")
                    .WithParameter("@filterTime", DateTime.UtcNow.Ticks);
                FeedIterator<ScheduledOperation> readyOps =   cosmosScheduleContainer.GetItemQueryIterator<ScheduledOperation>(query);
                while (readyOps.HasMoreResults)
                {
                    FeedResponse<ScheduledOperation> response = readyOps.ReadNextAsync().Result;
                    foreach (ScheduledOperation curOp in response)
                    {
                        bool opResult = false;
                        log.LogInformation($"Processing op: {curOp}");
                        switch (curOp.targetType.ToLower())
                        {
                            case "vm":
                                FIVM vmFuzzer = new FIVM(log, myAz, curOp.target);
                                opResult = vmFuzzer.processOp(curOp.operation);

                                break;
                            case "nsg":
                                FINSG nsgFuzzer = new FINSG(log, myAz, curOp.target);
                                opResult = nsgFuzzer.processOp(curOp.operation);
                                break;

                            default:
                                log.LogError("Got an op we don't know how to handle!");
                                break;
                        }
                        if (opResult)
                        {
                            // Delete it so we don't redo it
                            try
                            {
                                ItemResponse<ScheduledOperation> deleteResult = cosmosScheduleContainer.DeleteItemAsync<ScheduledOperation>(curOp.id, curOp.getPartitionKey()).Result;
                                log.LogInformation($"Completed op {curOp} - deleted from Cosmosb");
                            }
                            catch (Exception deleteOpErr)
                            {
                                log.LogError($"Error deleting an op from Cosmos: {deleteOpErr}");
                            }
                        }
                    }

                }


            }


            // process due ops




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

        }
        }
}
