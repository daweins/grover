using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Azure.Management.ResourceGraph.Models;
using Newtonsoft.Json.Linq;
using System.Linq;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Sql.Fluent;
using Microsoft.Azure.Management.Sql.Fluent.SqlFirewallRule;
using Microsoft.Azure.Management.Sql.Fluent.SqlFirewallRuleOperations;


//TODO: Desired improvements include: handling service endpoints


namespace AzureFaultInjector
{
    class FISQL : FI
    {

        public static string myTargetType = "SQL";
        public FISQL (ILogger iLog, Microsoft.Azure.Management.Fluent.IAzure iAzure, string iTarget) : base(iLog,iAzure, iTarget)
        {
            try
            {
                myResource = iAzure.SqlServers.GetById(iTarget);
            }
            catch(Exception err)
            {
                log.LogError($"Error in {myTargetType} constructor: {err.ToString()}");
            }
        }

        protected override bool turnOn(ScheduledOperation curOp)
        {
            Microsoft.Azure.Management.Sql.Fluent.ISqlServer curSQL = (Microsoft.Azure.Management.Sql.Fluent.ISqlServer)myResource;

            try
            {
                log.LogInformation($"Turning on SQL by recreating the firewall rules: {curSQL.Id}");
                //TODO: evaluate this
                // Don't need to re-flip the Geo - leave the DB where it is  

                // Recreate the firewall rules
                //TODO: do the AzureServices special case

                SQLFirewallDefinition rulesToRecreate = SQLFirewallDefinition.deserialize(curOp.payload);
                foreach(SQLFirewallRule curRule in rulesToRecreate.ruleList)
                {
                    string ruleName = curRule.name;
                    string startIP = curRule.startIP;
                    string endIP = curRule.endIP;
                    log.LogInformation($"Recreating sql firewall rule: {ruleName} {startIP} - {endIP}");
                    curSQL.FirewallRules.Define(ruleName).WithIPAddressRange(startIP, endIP).Create();
                }
                if(rulesToRecreate.allowAzureAccess)
                {
                    log.LogInformation($"Allowing Azure access");
                    curSQL.EnableAccessFromAzureServices();
                }
                log.LogInformation($"Turned on SQL: {curSQL.Id}");
                myLogHelper.logEvent(myTargetType, curTarget, "on");

                return true;
            }
            catch(Exception err)
            {
                log.LogError($"Error turning on SQL {curSubName} -> {curTarget} -> {curSQL.Name}: {err}");
                return false;
            }
        }

        protected override bool turnOff(ScheduledOperation curOp)
        {
            Microsoft.Azure.Management.Sql.Fluent.ISqlServer curSQL = (Microsoft.Azure.Management.Sql.Fluent.ISqlServer)myResource;

            try
            {

                log.LogInformation($"Turning off SQL: {curSQL.Id}");
                foreach (ISqlDatabase curDB in curSQL.Databases.List())
                {
                    log.LogInformation($"Looking at {curDB.Name}");
                    IEnumerable<IReplicationLink> replicationDefinition = curDB.ListReplicationLinks().Values;
                    if(replicationDefinition.Count() > 0)
                    {
                        IReplicationLink curReplicationDefinition = replicationDefinition.First();
                            curReplicationDefinition.ForceFailoverAllowDataLossAsync();
                            log.LogInformation($"Forcing SQL Failover to {curDB.RegionName} from {curReplicationDefinition.PartnerLocation}");
                        
                    }
                }
                //TODO: find a way to impact existing connections
                SQLFirewallDefinition rulesToPreserve = new SQLFirewallDefinition();

                log.LogInformation($"All DBs started failover, now block the current server to simulate failure in the firewall. Note - this won't impact existing connections");
                {
                    // TODO: Need to list these so we can save & rehydrate them later
                 
                    foreach (var curFirewallRule in curSQL.FirewallRules.List())
                    {
                        if (curFirewallRule.Name == "AllowAllWindowsAzureIps")
                        {
                            curSQL.RemoveAccessFromAzureServices();
                            rulesToPreserve.allowAzureAccess = true;
                        }
                        else
                        {
                            rulesToPreserve.addRule(curFirewallRule.Name, curFirewallRule.StartIPAddress, curFirewallRule.EndIPAddress);
                        }
                        curSQL.FirewallRules.Delete(curFirewallRule.Name);
                    }

                }

                log.LogInformation($"Turned off SQL: {curSQL.Id}. Creating the compensating On action");
                string onPayload = rulesToPreserve.toJSON();
                ScheduledOperation onOp = new ScheduledOperation(DateTime.Now.AddTicks(curOp.durationTicks), curOp.source,  $"Compensating On action for turning off a {myTargetType}", myTargetType, "on", curTarget, 0, onPayload);
                ScheduledOperationHelper.addSchedule(onOp, log);
                myLogHelper.logEvent(myTargetType, curTarget, "off");

                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error turning off SQL {curSubName} -> {curTarget} -> {curSQL.Name}: {err}");
                return false;
            }

        }

        /*
        static public List<ScheduledOperation> getSampleSchedule(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, ILogger log)
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

                log.LogInformation("Adding a SQL sample schedule this iteration");

                // Pick a random RG from the list
                int rgIndex = rnd.Next(rgList.Count);
                List<ISqlServer> SQLList = new List<ISqlServer>(  myAz.SqlServers.ListByResourceGroup(rgList[rgIndex].Name));
                if (SQLList.Count > 0)
                {
                    // Pick a random SQL from the RG
                    int SQLID = rnd.Next(SQLList.Count);

                    ScheduledOperation newOffOp = new ScheduledOperation(DateTime.Now, $"Sample {myTargetType} Off", myTargetType, "off", SQLList[SQLID].Id);
                    results.Add(newOffOp);
                }
                return results;
            }
        }
        */

        static public List<ScheduledOperation> killAZ(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, string source, string regionToKill, int azToKill, long startTicks, long endTicks, ILogger log)
        {
            List<ScheduledOperation> results = new List<ScheduledOperation>();
            log.LogInformation($"Sql AZKill for Zone {azToKill}: Nothing to do - SQL is zonally redundant");
            // TODO - should we flip it on & off quickly to simulate the potential time until the SLB notices the failed backend? can we flip it fast enough?

            return results;
        }


        static public List<ScheduledOperation> killRegion(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, string source, string regionToKill, long startTicks, long endTicks, ILogger log)
        {
            List<ScheduledOperation> results = new List<ScheduledOperation>();
            foreach (IResourceGroup curRG in rgList)
            {
                log.LogInformation($"SQL Region Kill for region:  {regionToKill}: checking RG: {curRG.Name}");
                List<ISqlServer> SQLList = new List<ISqlServer>(myAz.SqlServers.ListByResourceGroup(curRG.Name));
                foreach (ISqlServer curSQL in SQLList)
                {
                    if (curSQL.RegionName == regionToKill)
                    {
                        log.LogInformation($"RegionKill: Got a Region {regionToKill} match for {curSQL.Id} - scheduling for termination");
                        ScheduledOperation newOffOp = new ScheduledOperation(new DateTime(startTicks), $"{source} Region Kill {regionToKill}",$"Killing Region {regionToKill} - {myTargetType} Off", myTargetType, "off",  curSQL.Id, endTicks - startTicks);
                        results.Add(newOffOp);
                    }
                }

            }
            return results;
        }

    }
}
