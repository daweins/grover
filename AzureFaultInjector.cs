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
using System.Collections.Specialized;



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
            //            doRandomQueuePopulate(myAz, rgList, log);

            // Read the schedule list from CosmosDB
            string cosmosConn = Environment.GetEnvironmentVariable("cosmosConn");
            string cosmosDBName = Environment.GetEnvironmentVariable("cosmosDB");
            string cosmosContainerTestScheduleName = Environment.GetEnvironmentVariable("cosmosContainerMasterSchedule");

            // Get list of schedules to perform from cosmos
            using (CosmosClient cosmosClient = new CosmosClient(cosmosConn))
            {
                Database curDB = cosmosClient.GetDatabase(cosmosDBName);
                Container cosmosContainerTestSchedule = curDB.GetContainer(cosmosContainerTestScheduleName);

                QueryDefinition query = new QueryDefinition(
                    @"SELECT * 
                      FROM c
                      WHERE c.startTicks <= @filterTime and c.endTicks >= @filterTime and c.status = 'waiting' ")
                    .WithParameter("@filterTime", DateTime.UtcNow.Ticks);

                FeedIterator<TestSchedule> readyOps = cosmosContainerTestSchedule.GetItemQueryIterator<TestSchedule>(query);
                while (readyOps.HasMoreResults)
                {
                    FeedResponse<TestSchedule> response = readyOps.ReadNextAsync().Result;


                    foreach (TestSchedule curTestSchedule in response)
                    {
                        log.LogInformation($"Got Test Schedule to activate: {curTestSchedule.ToString()} ");
                    }
                }
            }

        }

        // TODO: Get a list of schedules that are running and need to stop from cosmos
        static private void doQueuePruning(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, ILogger log)
        {

        }
        // Shouldn't be called - dead code
        static private void doRandomQueuePopulate(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, ILogger log)
        { 
            double nextAction = rnd.NextDouble();
            nextAction = .85;
            if (nextAction < 0.7)
            {
                // Take a break
                return;
            }
            else if (nextAction < 0.8)
            {


                // Have each type roll the dice to generate a single error

                List<ScheduledOperation> opsToAdd = new List<ScheduledOperation>();
                foreach (Type curFI in FI.getSubTypes())
                {
                    try
                    {
                        List<ScheduledOperation> newOps = (List<ScheduledOperation>)curFI.GetMethod("getSampleSchedule").Invoke(null, new object[] { myAz, rgList, log });
                        opsToAdd.AddRange(newOps);
                    }
                    catch (Exception sampleError)
                    {
                        log.LogWarning($"Warning: trouble creating a sample schedule for {curFI.Name}. It might not have implemented the getSampleSchedule static function (note - C# doesnt support static abstracts) : {sampleError}");
                    }
                }
                log.LogInformation($"Sample Schedule: {opsToAdd.Count} items");
                ScheduledOperationHelper.addSchedule(opsToAdd, log);
            }
            else if (nextAction < 0.9)
            {

                // Trigger an AZ outage
                int azToKill = rnd.Next(3) + 1;
                log.LogInformation($"Trying to kill AZ {azToKill}");

                List<ScheduledOperation> opsToAdd = new List<ScheduledOperation>();
                foreach (Type curFI in FI.getSubTypes())
                {
                    try
                    {
                        log.LogInformation($"Trying to AZKill in type of: {curFI.FullName}");
                        List<ScheduledOperation> newOps = (List<ScheduledOperation>)curFI.GetMethod("killAZ").Invoke(null, new object[] { myAz, rgList, azToKill, log });
                        opsToAdd.AddRange(newOps);
                    }
                    catch (Exception sampleError)
                    {
                        log.LogWarning($"Warning: trouble creating an AZ Kill schedule for {curFI.Name}. It might not have implemented the killAZ static function (note - C# doesnt support static abstracts) : {sampleError}");
                    }
                }
                log.LogInformation($"AZKill Schedule: {opsToAdd.Count} items");
                ScheduledOperationHelper.addSchedule(opsToAdd, log);


            }
            else
            {
                // Trigger a regional outage
                // Find a list of interesting regions - use the RGList as a starter
                HashSet<string> regionList = new HashSet<string>();
                foreach(IResourceGroup curRG in rgList)
                {
                    regionList.Add(curRG.RegionName);
                }
                int regionIndex = rnd.Next(regionList.Count);
                string curRegion = regionList.ElementAt<string>(regionIndex);
                log.LogInformation($"RegionKill: {curRegion}");

                List<ScheduledOperation> opsToAdd = new List<ScheduledOperation>();
                foreach (Type curFI in FI.getSubTypes())
                {
                    try
                    {
                        log.LogInformation($"Trying to Kill Region in type of: {curFI.FullName}");
                        List<ScheduledOperation> newOps = (List<ScheduledOperation>)curFI.GetMethod("killRegion").Invoke(null, new object[] { myAz, rgList, curRegion, log });
                        opsToAdd.AddRange(newOps);
                    }
                    catch (Exception sampleError)
                    {
                        log.LogWarning($"Warning: trouble creating a region Kill  schedule for {curFI.Name}. It might not have implemented the regionToKill static function (note - C# doesnt support static abstracts) : {sampleError}");
                    }
                }
                log.LogInformation($"AZKill Schedule: {opsToAdd.Count} items");
                ScheduledOperationHelper.addSchedule(opsToAdd, log);


            }

        }

        static private void doQueueProcess(Microsoft.Azure.Management.Fluent.IAzure myAz, ILogger log)
        {
            string cosmosConn = Environment.GetEnvironmentVariable("cosmosConn");
            string cosmosDBName = Environment.GetEnvironmentVariable("cosmosDB");
            string cosmosContainerScheduledOperationsName = Environment.GetEnvironmentVariable("cosmosContainerScheduledOperations");

            // Get list of ops to perform from cosmos
            using (CosmosClient cosmosClient = new CosmosClient(cosmosConn))
            {
                Database curDB = cosmosClient.GetDatabase(cosmosDBName);
                Container cosmosContainerScheduledOperations = curDB.GetContainer(cosmosContainerScheduledOperationsName);
                
                QueryDefinition query = new QueryDefinition(
                    @"SELECT * 
                      FROM c
                      WHERE c.scheduleTimeTicks < @filterTime ")
                    .WithParameter("@filterTime", DateTime.UtcNow.Ticks);
                FeedIterator<ScheduledOperation> readyOps =   cosmosContainerScheduledOperations.GetItemQueryIterator<ScheduledOperation>(query);
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
                                opResult = vmFuzzer.processOp(curOp.operation, curOp.payload);

                                break;
                            case "nsg":
                                FINSG nsgFuzzer = new FINSG(log, myAz, curOp.target);
                                opResult = nsgFuzzer.processOp(curOp.operation, curOp.payload);
                                break;
                            case "web":
                                FIWeb webFuzzer = new FIWeb(log, myAz, curOp.target);
                                opResult = webFuzzer.processOp(curOp.operation, curOp.payload);
                                break;
                            case "sql":
                                FISQL sqlFuzzer = new FISQL(log, myAz, curOp.target);
                                opResult = sqlFuzzer.processOp(curOp.operation, curOp.payload);
                                break;
                            case "vmss":
                                FIVMSS vmssFuzzer = new FIVMSS(log, myAz, curOp.target);
                                opResult = vmssFuzzer.processOp(curOp.operation, curOp.payload);
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
                                ItemResponse<ScheduledOperation> deleteResult = cosmosContainerScheduledOperations.DeleteItemAsync<ScheduledOperation>(curOp.id, curOp.getPartitionKey()).Result;
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
