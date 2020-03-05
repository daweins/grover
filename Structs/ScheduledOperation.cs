using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace AzureFaultInjector
{
    public class ScheduledOperation
    {

        [JsonProperty(PropertyName = "scheduleTimeTicks")]
        public long scheduleTimeTicks { get; set; }


        [JsonProperty(PropertyName = "operation")]
        public string operation { get; set; }

        [JsonProperty(PropertyName = "targetType")]
        public string targetType { get; set; }

        [JsonProperty(PropertyName = "target")]
        public string target { get; set; }


        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }


        [JsonProperty(PropertyName = "description")]
        public string description { get; set; }


        [JsonProperty(PropertyName = "payload")]
        public string payload { get; set; }


        [JsonProperty(PropertyName = "relatedOps")]
        public List<string> relatedOps { get; set; }  




    public ScheduledOperation(DateTime iScheduleTime, string iDescription, string iTargetType, string iOperation,  string iTarget, string iPayload = "")
        {
            scheduleTimeTicks = iScheduleTime.Ticks;
            targetType = iTargetType;
            operation = iOperation;
            target = iTarget;
            description = iDescription;
            payload = iPayload;
            id = Guid.NewGuid().ToString();
            relatedOps = new List<string>();
        }

        public PartitionKey getPartitionKey()
        {
            // TODO: Think about the partitionkey choice
            return new PartitionKey(targetType);
        }

        override public string ToString()
        {
            return $"ScheduledOperation: id={id}; scheduleTime: { new DateTime(scheduleTimeTicks).ToString()}; targetType: { targetType};opType: {operation}; target: {target}; description: {description} ";

        }
        static void createLink(ScheduledOperation a, ScheduledOperation b)
        {
            a.relatedOps.Add(b.id);
            b.relatedOps.Add(a.id);

        }
    }
}
