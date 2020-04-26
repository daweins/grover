using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

using Microsoft.Azure.Cosmos;

namespace AzureFaultInjector
{
    public class TestSchedule
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }


        [JsonProperty(PropertyName = "startTicks")]
        public long startTicks { get; set; }

     
        [JsonProperty(PropertyName = "endTicks")]
        public long endTicks { get; set; }


        [JsonProperty(PropertyName = "startTimeReadOnly")]
        public string startTimeReadOnly
        {
            get
            {
                return new DateTime(startTicks).ToString();
            }
            set
            {
            }

        }

        [JsonProperty(PropertyName = "endTimeReadOnly")]
        public string endTimeReadOnly
        {
            get
            {
                return new DateTime(endTicks).ToString();
            }
            set
            {
            }

        }

        [JsonProperty(PropertyName = "status")]
        public string status { get; set; }


        [JsonProperty(PropertyName = "testDef")]
        public string testDef { get; set; }




        public DateTime getStart()
        {
            return (new DateTime(startTicks));
        }
        public DateTime getEnd()
        {
            return (new DateTime(endTicks));
        }


        public PartitionKey getPartitionKey()
        {
            // TODO: Think about the partitionkey choice
            return new PartitionKey(status);
        }

        override public string ToString()
        {
            return $"TestSchedule: id={id}; scheduleTime: { new DateTime(startTicks).ToString()} - { new DateTime(startTicks).ToString()} ;  ";

        }

        public static TestSchedule getSample()
        {
            TestSchedule sampleSchedule = new TestSchedule();
            sampleSchedule.id = Guid.NewGuid().ToString();
            sampleSchedule.startTicks = DateTime.Now.AddMinutes(1).Ticks;
            sampleSchedule.endTicks = DateTime.Now.AddMinutes(60).Ticks;
            sampleSchedule.status = "waiting";
            sampleSchedule.testDef = "sample";
            return (sampleSchedule);
        }
    }
}
