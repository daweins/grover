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

        [JsonProperty(PropertyName = "numRepititions")]
        public int numRepititions { get; set; }

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

        public static TestDefinition getSample()
        {
            TestDefinition sampleDefinition = new TestDefinition();
            sampleDefinition.id = Guid.NewGuid().ToString();
            sampleDefinition.testDefName = "sample";
            sampleDefinition.numRepititions = 1;
            sampleDefinition.actionList = new List<TestDefinitionAction>();
            for (int i = 0; i < 10; i++)
            {
                sampleDefinition.actionList.Add(TestDefinitionAction.getSample());
            }
            return (sampleDefinition);
        }

    }

    public class TestDefinitionAction
    {
        [JsonProperty(PropertyName = "label")]
        public string label { get; set; }



        // Known Types - Resource, Region, AZ, Service
        static private string[] knownActionTypes = new string[] { "Resource", "Region", "AZ"};
        [JsonProperty(PropertyName = "actionType")]
        public string actionType { get; set; }

        [JsonProperty(PropertyName = "durationMinutes")]
        public int durationMinutes { get; set; }


        [JsonProperty(PropertyName = "numFailures")]
        public int numFailures { get; set; }

        [JsonProperty(PropertyName = "fiTypes")]
        public List<TestDefinitionFIType> fiTypes { get; set; }


        [JsonProperty(PropertyName = "regionFailureList")]
        public List<TestDefinitionRegionFailureDefinition> regionFailureList;

        [JsonProperty(PropertyName = "resourceNameList")]
        public List<TestDefinitionResourceNameDefinition> resourceNameList;


        public static TestDefinitionAction getSample()
        {
            TestDefinitionAction sampleAction = new TestDefinitionAction();
            sampleAction.durationMinutes = 5;
            sampleAction.label = "sample";
            sampleAction.numFailures = 1;

            Random rnd = new Random();
            int i = rnd.Next(knownActionTypes.Length);
            sampleAction.actionType = knownActionTypes[i];


            sampleAction.fiTypes = new List<TestDefinitionFIType>();
            sampleAction.fiTypes.Add(TestDefinitionFIType.getSampleTestDefinitionFIType());

            sampleAction.regionFailureList = new List<TestDefinitionRegionFailureDefinition>();
            sampleAction.regionFailureList.Add(TestDefinitionRegionFailureDefinition.getSampleTestDefinitionRegionFailure());

            sampleAction.resourceNameList = new List<TestDefinitionResourceNameDefinition>();
            sampleAction.resourceNameList.Add(TestDefinitionResourceNameDefinition.getSampleTestDefinitionResourceName());


            return (sampleAction);
        }

    }

    public class TestDefinitionFIType
    {
        [JsonProperty(PropertyName = "fi")]
        public string fi { get; set; }

        static public TestDefinitionFIType getSampleTestDefinitionFIType()
        {
            TestDefinitionFIType sampleFIType = new TestDefinitionFIType();
            sampleFIType.fi = "*";
            return (sampleFIType);
        }
    }


    public class TestDefinitionRegionFailureDefinition
    {
        [JsonProperty(PropertyName = "region")]
        public string region { get; set; }

        static public TestDefinitionRegionFailureDefinition getSampleTestDefinitionRegionFailure()
        {
            TestDefinitionRegionFailureDefinition sampleRegion = new TestDefinitionRegionFailureDefinition();
            sampleRegion.region = "*";
            return sampleRegion;
        }

    }

    public class TestDefinitionResourceNameDefinition
    {
        [JsonProperty(PropertyName = "resourceShortName")]
        public string resourceShortName { get; set; }

        public static TestDefinitionResourceNameDefinition getSampleTestDefinitionResourceName()
        {
            TestDefinitionResourceNameDefinition sampleResourceName = new TestDefinitionResourceNameDefinition();
            sampleResourceName.resourceShortName = ".*rigbasic.*";
            return (sampleResourceName);
        }
    }

  
}
