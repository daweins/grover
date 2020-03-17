using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

using Microsoft.Azure.Cosmos;

namespace AzureFaultInjector
{
    public class TestDefinition
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }



        [JsonProperty(PropertyName = "testDefName")]
        public string testDefName { get; set; }

        [JsonProperty(PropertyName = "actionList")]
        public List<TestDefinitionAction> actionList { get; set; }





        public PartitionKey getPartitionKey()
        {
            // TODO: Think about the partitionkey choice
            return new PartitionKey(testDefName);
        }

        override public string ToString()
        {
            return $"TestDefinition: id={id}; Name: {testDefName}; # Actions: {actionList.Count} ";

        }

    }

    public class TestDefinitionAction
    {
        [JsonProperty(PropertyName = "label")]
        public string label { get; set; }


        [JsonProperty(PropertyName = "durationMinutes")]
        public int durationMinutes { get; set; }

        [JsonProperty(PropertyName = "fiTypes")]
        public List<TestDefinitionFIType> fiTypes { get; set; }


        [JsonProperty(PropertyName = "regionFailureList")]
        public List<TestDefinitionRegionFailureDefinition> regionFailureList;

    }

    public class TestDefinitionFIType
    {
        [JsonProperty(PropertyName = "fi")]
        public string fi { get; set; }

    }


    public class TestDefinitionRegionFailureDefinition
    {
        [JsonProperty(PropertyName = "region")]
        public string region { get; set; }

    }

}
