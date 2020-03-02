using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace AzureFaultInjector
{
    static public class ScheduledOperationHelper
    {
        static public void addSchedule(ScheduledOperation newOp, ILogger log)
        {
            List<ScheduledOperation> myList = new List<ScheduledOperation>();
            myList.Add(newOp);
            addSchedule(myList, log);
        }


        static public void addSchedule(List<ScheduledOperation> newOps, ILogger log)
        {

            string cosmosConn = Environment.GetEnvironmentVariable("cosmosConn");
            string cosmosDBName = Environment.GetEnvironmentVariable("cosmosDB");
            string cosmosScheduleContainerName = Environment.GetEnvironmentVariable("cosmosScheduleContainer");

            using (CosmosClient cosmosClient = new CosmosClient(cosmosConn))
            {
                Database curDB = cosmosClient.GetDatabase(cosmosDBName);
                Container cosmosScheduleContainer = curDB.GetContainer(cosmosScheduleContainerName);
                foreach (ScheduledOperation newOp in newOps)
                {
                    try
                    {
                        ItemResponse<ScheduledOperation> createOp = cosmosScheduleContainer.CreateItemAsync(newOp, newOp.getPartitionKey()).Result;
                        log.LogInformation($"Created Op: {createOp.ToString()}");
                    }
                    catch (Exception err)
                    {
                        log.LogError($"Error creating scheduled Op: {err.ToString()} for op: {newOp.ToString()}");
                    }
                }

            }
        }
    }
}
