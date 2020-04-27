using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Azure.Management.ResourceGraph.Models;
using System.Linq;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Threading.Tasks;




namespace AzureFaultInjector
{
    class FINSG : FI
    {

        public static string myTargetType = "NSG";

        static string myNewRuleNameIn = "FuzzerBlockingRuleIn";
        static string myNewRuleNameOut = "FuzzerBlockingRuleOut";
        static int myNewRulePriority = 100;


        public FINSG (ILogger iLog, Microsoft.Azure.Management.Fluent.IAzure iAzure, string iTarget) : base(iLog,iAzure, iTarget)
        {
            try
            {
                myResource = iAzure.NetworkSecurityGroups.GetById(iTarget);
            }
            catch(Exception err)
            {
                log.LogError($"Error in {myTargetType} constructor: {err.ToString()}");
            }
}


        protected override bool turnOn(ScheduledOperation curOp)
        {
            Microsoft.Azure.Management.Network.Fluent.INetworkSecurityGroup curNSG = (Microsoft.Azure.Management.Network.Fluent.INetworkSecurityGroup)myResource;

            try
            {
                log.LogInformation($"Removing new highest priority blocking rule to {myTargetType} {curNSG.Name}: In & Out");


                curNSG.Update().WithoutRule(myNewRuleNameIn).Apply();
                curNSG.Update().WithoutRule(myNewRuleNameOut).Apply();
                myLogHelper.logEvent(myTargetType, curTarget, "on");

                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error unblocking {myTargetType} {curSubName} -> {curTarget} -> {curNSG.Name}: {err}");
                return false;
            }

        }

        protected override bool turnOff(ScheduledOperation curOp)
        {
            Microsoft.Azure.Management.Network.Fluent.INetworkSecurityGroup curNSG = (Microsoft.Azure.Management.Network.Fluent.INetworkSecurityGroup)myResource;

            try
            {
                // Add a blocking rule
                log.LogInformation($"Adding new highest priority blocking rule to {myTargetType} {curNSG.Name}: Inbound");
                curNSG.Update().DefineRule(myNewRuleNameIn)
                    .DenyInbound()
                    .FromAnyAddress()
                    .FromAnyPort()
                    .ToAnyAddress()
                    .ToAnyPort()
                    .WithAnyProtocol()
                    .WithDescription("Temporary stop everything rule from Azure Fuzzer to simulate a cut network")
                    .WithPriority(100)
                    .Attach()
                    .Apply();
                log.LogInformation($"Adding new highest priority blocking rule to {myTargetType} {curNSG.Name}: Outbound");
                curNSG.Update().DefineRule(myNewRuleNameOut)
                    .DenyOutbound()
                    .FromAnyAddress()
                    .FromAnyPort()
                    .ToAnyAddress()
                    .ToAnyPort()
                    .WithAnyProtocol()
                    .WithDescription("Temporary stop everything rule from Azure Fuzzer to simulate a cut network")
                    .WithPriority(100)
                    .Attach()
                    .Apply();
                log.LogInformation($"Turned off NSG: {curNSG.Id}. Creating the compensating On action");
                ScheduledOperation onOp = new ScheduledOperation(DateTime.Now.AddTicks(curOp.durationTicks), curOp.source,  $"Compensating On action for turning off a {myTargetType}", myTargetType, "on", curTarget,0);
                ScheduledOperationHelper.addSchedule(onOp, log);
                myLogHelper.logEvent(myTargetType, curTarget, "off");

                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error blocking NSG {curSubName} -> {curTarget} -> {curNSG.Name}: {err}");
                return false;
            }
        }

        /*
        static public  List<ScheduledOperation> getSampleSchedule(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, ILogger log)
        {

            List<ScheduledOperation> results = new List<ScheduledOperation>();
            // Should I fault?
            if (rnd.NextDouble() > 0.3)
            {
                log.LogInformation("Not adding anything to the sample schedule this iteration");
                return results;
            }
            else
            {

                log.LogInformation($"Adding a {myTargetType} sample schedule this iteration");

                // Pick a random RG from the list
                int rgIndex = rnd.Next(rgList.Count);
                List<INetworkSecurityGroup> NSGList = new List<INetworkSecurityGroup>(myAz.NetworkSecurityGroups.ListByResourceGroup(rgList[rgIndex].Name));
                if (NSGList.Count > 0)
                {
                    // Pick a random NSG from the RG
                    int NSGID = rnd.Next(NSGList.Count);

                    ScheduledOperation newOffOp = new ScheduledOperation(DateTime.Now, $"Sample {myTargetType} Off", myTargetType, "off", NSGList[NSGID].Id);
                    results.Add(newOffOp);
                }
                return results;
            }
        }
        */

        static public List<ScheduledOperation> killAZ(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, string source, string regionToKill, int azToKill, ILogger log)
        {
            // Network doesn't AZ - return nothing
            log.LogInformation($"{myTargetType}: Killing AZ - return nothing as {myTargetType} doesn't have AZ");
            List <ScheduledOperation> results = new List<ScheduledOperation>();
            return results;
        }

        static public List<ScheduledOperation> killRegion(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, string source, string regionToKill, long startTicks, long endTicks, ILogger log)
        {
            List<ScheduledOperation> results = new List<ScheduledOperation>();
            foreach (IResourceGroup curRG in rgList)
            {
                log.LogInformation($"{myTargetType} Region Kill for region:  {regionToKill}: checking RG: {curRG.Name}");
                List<INetworkSecurityGroup> nsgList = new List<INetworkSecurityGroup>(myAz.NetworkSecurityGroups.ListByResourceGroup(curRG.Name));
                foreach (INetworkSecurityGroup curNSG in nsgList)
                {
                    if (curNSG.RegionName == regionToKill)
                    {
                        log.LogInformation($"Region Kill: Got a Region {regionToKill} match for {curNSG.Id} - scheduling for termination");
                        ScheduledOperation newOffOp = new ScheduledOperation(new DateTime(startTicks), $"{source} Region Kill {regionToKill}", $"Killing Region {regionToKill} - {myTargetType} Off", myTargetType, "off", curNSG.Id, endTicks - startTicks);
                        results.Add(newOffOp);
                    }
                }

            }
            return results;
        }

        static public List<ScheduledOperation> killResource(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, string source, string resourceToKill, long startTicks, long endTicks, ILogger log)
        {
            List<ScheduledOperation> results = new List<ScheduledOperation>();
      log.LogInformation($"TargetType: {myTargetType}:Resource Kill : not implemented");
                  
            return results;
        }

    }
}
