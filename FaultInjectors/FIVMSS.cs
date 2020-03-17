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
using Microsoft.Azure.Management.Storage.Fluent;

namespace AzureFaultInjector
{
    class FIVMSS : FI
    {

        public static string myTargetType = "VMSS";

        public FIVMSS(ILogger iLog, Microsoft.Azure.Management.Fluent.IAzure iAzure, string iTarget) : base(iLog, iAzure, iTarget)
        {
            try
            {
                myResource = iAzure.VirtualMachineScaleSets.GetById(iTarget);
            }
            catch (Exception err)
            {
                log.LogError($"Error in {myTargetType} constructor: {err.ToString()}");
            }
        }

        protected override bool turnOn(string payload)
        {
            Microsoft.Azure.Management.Compute.Fluent.IVirtualMachineScaleSet curVMSS = (Microsoft.Azure.Management.Compute.Fluent.IVirtualMachineScaleSet)myResource;

            try
            {
                log.LogInformation($"Turning on {myTargetType}: {curVMSS.Id} with payload {payload}");
                if (payload.Length > 0)
                {
                    // We have a specific instance to restore
                    log.LogInformation("Targetting specific VM in the VMSS for Turning On");
                    var curVM = curVMSS.VirtualMachines.GetInstance(payload);
                    curVM.StartAsync();

                }
                else
                {
                    log.LogInformation("Turning on entire VMSS");
                    curVMSS.StartAsync();
                    log.LogInformation($"Turning on {myTargetType} (async): {curVMSS.Id}");
                    myLogHelper.logEvent(myTargetType, curTarget, "on");
                }
                return true;
            }
            catch(Exception err)
            {
                log.LogError($"Error turning on {myTargetType} {curSubName} -> {curTarget} -> {curVMSS.Name}: {err}");
                return false;
            }
        }

        protected override bool turnOff(long durationTicks, string payload = "")
        {
            Microsoft.Azure.Management.Compute.Fluent.IVirtualMachineScaleSet curVMSS = (Microsoft.Azure.Management.Compute.Fluent.IVirtualMachineScaleSet)myResource;

            try
            {
                if(curVMSS.Capacity > 0)
                {
                    log.LogInformation($"Turning off VMSS: {curVMSS.Id} with payload {payload}");
                    if (payload.Length > 0)
                    {
                        // We have a specific instance to kill
                        log.LogInformation("Targetting specific VM in the VMSS for Turning On");
                        var curVM = curVMSS.VirtualMachines.GetInstance(payload);
                        curVM.PowerOffAsync();
                        log.LogInformation($"Turning off VMSS (async): {curVMSS.Id} Instance {curVM.Id}.  Creating the compensating On action");
                        ScheduledOperation onOp = new ScheduledOperation(DateTime.Now.AddTicks(durationTicks), $"Compensating On action for turning off a {myTargetType}", myTargetType, "on", curTarget, 0,curVM.InstanceId);
                        ScheduledOperationHelper.addSchedule(onOp, log);

                    }
                    else
                    {

                        log.LogInformation("Turning off entire VMSS");
                        curVMSS.PowerOffAsync();   // We don't really care if this fails - worst case we turn it on when it's already on
                        log.LogInformation($"Turning off VMSS (async): {curVMSS.Id}. Creating the compensating On action");
                        ScheduledOperation onOp = new ScheduledOperation(DateTime.Now.AddTicks(durationTicks), $"Compensating On action for turning off a {myTargetType}", myTargetType, "on", curTarget,0);
                        ScheduledOperationHelper.addSchedule(onOp, log);
                        myLogHelper.logEvent(myTargetType, curTarget, "off");
                    }
                }
                else
                {
                    log.LogInformation($"Turning off {myTargetType} {curVMSS.Id}, but it was already not running");
                }
                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error turning off {myTargetType} {curSubName} -> {curTarget} -> {curVMSS.Name}: {err}");
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

                log.LogInformation($"Adding a {myTargetType} sample schedule this iteration");

                // Pick a random RG from the list
                int rgIndex = rnd.Next(rgList.Count);
                List<IVirtualMachineScaleSet> VMSSList = new List<IVirtualMachineScaleSet>(  myAz.VirtualMachineScaleSets.ListByResourceGroup(rgList[rgIndex].Name));
                if (VMSSList.Count > 0 )
                {
                    // Pick a random VMSS from the RG
                    int VMSSID = rnd.Next(VMSSList.Count);

                    ScheduledOperation newOffOp = new ScheduledOperation(DateTime.Now, $"Sample {myTargetType} Off", myTargetType, "off", VMSSList[VMSSID].Id);
                    results.Add(newOffOp);
                }
                return results;
            }
        }
        */

        static public List<ScheduledOperation> killAZ(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, int azToKill, long startTicks, long endTicks, ILogger log)
        {
            List<ScheduledOperation> results = new List<ScheduledOperation>();
            foreach (IResourceGroup curRG in rgList)
            {
                log.LogInformation($"{myTargetType} AZKill for Zone {azToKill}: checking RG: {curRG.Name}");
                List<IVirtualMachineScaleSet> VMSSList = new List<IVirtualMachineScaleSet>(myAz.VirtualMachineScaleSets.ListByResourceGroup(curRG.Name));
                foreach (IVirtualMachineScaleSet curVMSS in VMSSList)
                {
                    // Iterate over each VM in the VMSS
                    IEnumerable<IVirtualMachineScaleSetVM> vmList = curVMSS.VirtualMachines.List();

                    foreach (IVirtualMachineScaleSetVM curVM in vmList)
                    {
                        // VMSS VM doesn't explicitly state which AZ it is in, have to break into its inner object. :( PG Feedback to be sent
                        
                        foreach (var curZone in curVM.Inner.Zones)
                        {
                            if (curZone == azToKill.ToString())  // Unlike a VM, the Zone here is a string - that's easier for us
                            {
                                log.LogInformation($"AZKill: Got a Zone {azToKill} match for {curVM.Id} - scheduling for termination");

                                ScheduledOperation newOffOp = new ScheduledOperation(new DateTime(startTicks), $"Killing AZ {azToKill} - {myTargetType} VM Instance Off", myTargetType, "off", curVMSS.Id, endTicks - startTicks, curVM.InstanceId);
                                results.Add(newOffOp);
                            }
                        }
                    }
                }

            }
            return results;
        }
            

            
        static public List<ScheduledOperation> killRegion(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, string regionToKill, long startTicks, long endTicks, ILogger log)
        {
            List<ScheduledOperation> results = new List<ScheduledOperation>();
            foreach (IResourceGroup curRG in rgList)
            {
                log.LogInformation($"{myTargetType} Region Kill for region:  {regionToKill}: checking RG: {curRG.Name}");
                List<IVirtualMachineScaleSet> VMSSList = new List<IVirtualMachineScaleSet>(myAz.VirtualMachineScaleSets.ListByResourceGroup(curRG.Name));
                foreach (IVirtualMachineScaleSet curVMSS in VMSSList)
                {
                    if (curVMSS.RegionName == regionToKill)
                    {
                        log.LogInformation($"RegionKill: Got a Region {regionToKill} match for {curVMSS.Id} - scheduling for termination");
                        ScheduledOperation newOffOp = new ScheduledOperation(new DateTime(startTicks), $"Killing Region {regionToKill} - {myTargetType} Off", myTargetType, "off", curVMSS.Id, endTicks - startTicks);
                        results.Add(newOffOp);
                    }
                }

            }
            return results;
        }

    }
}
