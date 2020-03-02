using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace AzureFaultInjector
{
    public class ScheduledOperation
    {

        [JsonProperty(PropertyName = "scheduleTime")]
        public DateTime scheduleTime { get; set; }


        [JsonProperty(PropertyName = "opType")]
        public string opType { get; set; }


        [JsonProperty(PropertyName = "target")]
        public string target { get; set; }


        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }


        [JsonProperty(PropertyName = "description")]
        public string description { get; set; }


        [JsonProperty(PropertyName = "relatedOps")]
        public List<string> relatedOps { get; set; }  




    public ScheduledOperation(DateTime iScheduleTime, string iDescription, string iOpType, string iTarget)
        {
            scheduleTime = iScheduleTime;
            opType = iOpType;
            target = iTarget;
            description = iDescription;
            id = Guid.NewGuid().ToString();
            relatedOps = new List<string>();
        }

        public PartitionKey getPartitionKey()
        {
            // TODO: Think about the partitionkey choice
            return new PartitionKey(opType);
        }

        override public string ToString()
        {
            return $"ScheduledOperation: id={id}; scheduleTime: {scheduleTime.ToString}; opType: {opType}; target: {target}; description: {description} ";

        }
        static void createLink(ScheduledOperation a, ScheduledOperation b)
        {
            a.relatedOps.Add(b.id);
            b.relatedOps.Add(a.id);

        }
    }
}
