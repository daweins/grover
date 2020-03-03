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

namespace AzureFaultInjector
{
    class FIVM : FI
    {
        public FIVM (ILogger iLog, Microsoft.Azure.Management.Fluent.IAzure iAzure, string iTarget) : base(iLog,iAzure, iTarget)
        {
            myResource= iAzure.VirtualMachines.GetById(iTarget) ;
            
            myTargetType = "VM";
        }

        protected override bool turnOn()
        {
            Microsoft.Azure.Management.Compute.Fluent.IVirtualMachine curVM = (Microsoft.Azure.Management.Compute.Fluent.IVirtualMachine)myResource;

            try
            {
                log.LogInformation($"Turning on VM: {curVM.Id}");
                curVM.Start();
                log.LogInformation($"Turned on VM: {curVM.Id}");

                return true;
            }
            catch(Exception err)
            {
                log.LogError($"Error turning on VM {curSubName} -> {curTarget} -> {curVM.Name}: {err}");
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
                    curVM.PowerOff();
                    log.LogInformation($"Turned off VM: {curVM.Id}. Creating the compensating On action");
                    ScheduledOperation onOp = new ScheduledOperation(DateTime.Now.AddMinutes(numMinutes), "Compensating On action for turning off a VM", "vm", "on", curTarget);
                    ScheduledOperationHelper.addSchedule(onOp, log);

                }
                else
                {
                    log.LogInformation($"Turning off VM {curVM.Id}, but it was already not running");
                }
                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error turning on VM {curSubName} -> {curTarget} -> {curVM.Name}: {err}");
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

                log.LogInformation("Adding a VM sample schedule this iteration");

                // Pick a random RG from the list
                int rgIndex = rnd.Next(rgList.Count);
                List<IVirtualMachine> vmList = new List<IVirtualMachine>(  myAz.VirtualMachines.ListByResourceGroup(rgList[rgIndex].Name));

                // Pick a random VM from the RG
                int vmID = rnd.Next(vmList.Count);

                ScheduledOperation newOffOp = new ScheduledOperation(DateTime.Now, "Sample VM Off", "vm", "off", vmList[vmID].Id);
                results.Add(newOffOp);

                return results;
            }
        }

        static public List<ScheduledOperation> killAZ(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, int azToKill, ILogger log)
        {
            List<ScheduledOperation> results = new List<ScheduledOperation>();
            foreach(IResourceGroup curRG in rgList)
            {
                log.LogInformation($"VM AZKill: checking RG: {curRG.Name}");
                List<IVirtualMachine> vmList = new List<IVirtualMachine>(myAz.VirtualMachines.ListByResourceGroup(curRG.Name));
                foreach(IVirtualMachine curVM in vmList)
                {
                    // This is interesting that a VM object can be in multiple AZs... let's just roll with it
                    foreach(var curZone in curVM.AvailabilityZones)
                    {
                        if(curZone.Value.ToString() == azToKill.ToString())
                        {
                            log.LogInformation($"AZKill: Got a Zone {azToKill} match for {curVM.Id} - scheduling for termination");
                            
                            ScheduledOperation newOffOp = new ScheduledOperation(DateTime.Now, $"Killing AZ {azToKill} - VM Off", "vm", "off", curVM.Id);
                            results.Add(newOffOp);
                        }
                    }
                }

            }
            return results;
        }


    }
}
