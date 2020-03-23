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

        [JsonProperty(PropertyName = "scheduleTimeReadOnly")]
        public string scheduleTimeReadOnly { get {
                return new DateTime(scheduleTimeTicks).ToString();
            }
            set
            {
            }

        }


        [JsonProperty(PropertyName = "operation")]
        public string operation { get; set; }

        [JsonProperty(PropertyName = "source")]
        public string source { get; set; }


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


        [JsonProperty(PropertyName = "durationTicks")]
        public long durationTicks { get; set; }


        public DateTime getStart()
        {
            return (new DateTime(scheduleTimeTicks));
        }


        public ScheduledOperation(DateTime iScheduleTime, string iSource, string iDescription, string iTargetType, string iOperation,  string iTarget, long iDurationTicks, string iPayload = "")
        {
            scheduleTimeTicks = iScheduleTime.Ticks;
            targetType = iTargetType;
            operation = iOperation;
            source = iSource;
            target = iTarget;
            description = iDescription;
            payload = iPayload;
            id = Guid.NewGuid().ToString();
            relatedOps = new List<string>();
            durationTicks = iDurationTicks;
        }

        public PartitionKey getPartitionKey()
        {
            // TODO: Think about the partitionkey choice
            return new PartitionKey(targetType);
        }

        override public string ToString()
        {
            return $"ScheduledOperation: id={id}; scheduleTime: { getStart()}; targetType: { targetType};opType: {operation}; target: {target}; description: {description} ";

        }
        static void createLink(ScheduledOperation a, ScheduledOperation b)
        {
            a.relatedOps.Add(b.id);
            b.relatedOps.Add(a.id);

        }
    }
}
