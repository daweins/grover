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

namespace AzureFaultInjector
{
    class FIVM : FI
    {

        static string myTargetType = "VM";

        public FIVM (ILogger iLog, Microsoft.Azure.Management.Fluent.IAzure iAzure, string iTarget) : base(iLog,iAzure, iTarget)
        {
            try
            {
                myResource = iAzure.VirtualMachines.GetById(iTarget);
            }
            catch(Exception err)
            {
                log.LogError($"Error in {myTargetType} constructor: {err.ToString()}");
            }
}

        protected override bool turnOn()
        {
            Microsoft.Azure.Management.Compute.Fluent.IVirtualMachine curVM = (Microsoft.Azure.Management.Compute.Fluent.IVirtualMachine)myResource;

            try
            {
                log.LogInformation($"Turning on {myTargetType}: {curVM.Id}");
                curVM.StartAsync();
                log.LogInformation($"Turning on {myTargetType} (async): {curVM.Id}");

                return true;
            }
            catch(Exception err)
            {
                log.LogError($"Error turning on {myTargetType} {curSubName} -> {curTarget} -> {curVM.Name}: {err}");
                return false;
            }
        }

        protected override bool turnOff(int numMinutes = 5)
        {
            Microsoft.Azure.Management.Compute.Fluent.IVirtualMachine curVM = (Microsoft.Azure.Management.Compute.Fluent.IVirtualMachine)myResource;

            try
            {
                if(curVM.PowerState == PowerState.Running)
                {
                    log.LogInformation($"Turning off VM: {curVM.Id}");
                    curVM.PowerOffAsync();   // We don't really care if this fails - worst case we turn it on when it's already on
                    log.LogInformation($"Turning off VM (async): {curVM.Id}. Creating the compensating On action");
                    ScheduledOperation onOp = new ScheduledOperation(DateTime.Now.AddMinutes(numMinutes), $"Compensating On action for turning off a {myTargetType}", myTargetType, "on", curTarget);
                    ScheduledOperationHelper.addSchedule(onOp, log);

                }
                else
                {
                    log.LogInformation($"Turning off {myTargetType} {curVM.Id}, but it was already not running");
                }
                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error turning on {myTargetType} {curSubName} -> {curTarget} -> {curVM.Name}: {err}");
                return false;
            }

        }


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

                log.LogInformation($"Adding a {myTargetType} sample schedule this iteration");

                // Pick a random RG from the list
                int rgIndex = rnd.Next(rgList.Count);
                List<IVirtualMachine> vmList = new List<IVirtualMachine>(  myAz.VirtualMachines.ListByResourceGroup(rgList[rgIndex].Name));
                if (vmList.Count > 0 )
                {
                    // Pick a random VM from the RG
                    int vmID = rnd.Next(vmList.Count);

                    ScheduledOperation newOffOp = new ScheduledOperation(DateTime.Now, $"Sample {myTargetType} Off", myTargetType, "off", vmList[vmID].Id);
                    results.Add(newOffOp);
                }
                return results;
            }
        }

        static public List<ScheduledOperation> killAZ(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, int azToKill, ILogger log)
        {
            List<ScheduledOperation> results = new List<ScheduledOperation>();
            foreach(IResourceGroup curRG in rgList)
            {
                log.LogInformation($"{myTargetType} AZKill for Zone {azToKill}: checking RG: {curRG.Name}");
                List<IVirtualMachine> vmList = new List<IVirtualMachine>(myAz.VirtualMachines.ListByResourceGroup(curRG.Name));
                foreach(IVirtualMachine curVM in vmList)
                {
                    // This is interesting that a VM object can be in multiple AZs... let's just roll with it
                    foreach(var curZone in curVM.AvailabilityZones)
                    {
                        if(curZone.Value.ToString() == azToKill.ToString())
                        {
                            log.LogInformation($"AZKill: Got a Zone {azToKill} match for {curVM.Id} - scheduling for termination");
                            
                            ScheduledOperation newOffOp = new ScheduledOperation(DateTime.Now, $"Killing AZ {azToKill} - {myTargetType} Off", myTargetType, "off", curVM.Id);
                            results.Add(newOffOp);
                        }
                    }
                }

            }
            return results;
        }


        static public List<ScheduledOperation> killRegion(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, string regionToKill, ILogger log)
        {
            List<ScheduledOperation> results = new List<ScheduledOperation>();
            foreach (IResourceGroup curRG in rgList)
            {
                log.LogInformation($"{myTargetType} Region Kill for region:  {regionToKill}: checking RG: {curRG.Name}");
                List<IVirtualMachine> vmList = new List<IVirtualMachine>(myAz.VirtualMachines.ListByResourceGroup(curRG.Name));
                foreach (IVirtualMachine curVM in vmList)
                {
                    if (curVM.RegionName == regionToKill)
                    {
                        log.LogInformation($"RegionKill: Got a Region {regionToKill} match for {curVM.Id} - scheduling for termination");
                        ScheduledOperation newOffOp = new ScheduledOperation(DateTime.Now, $"Killing Region {regionToKill} - {myTargetType} Off", myTargetType, "off", curVM.Id);
                        results.Add(newOffOp);
                    }
                }

            }
            return results;
        }

    }
}
