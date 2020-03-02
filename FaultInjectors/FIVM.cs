using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Azure.Management.ResourceGraph.Models;
using Newtonsoft.Json.Linq;
using System.Linq;

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

        protected override bool turnOff()
        {
            Microsoft.Azure.Management.Compute.Fluent.IVirtualMachine curVM = (Microsoft.Azure.Management.Compute.Fluent.IVirtualMachine)myResource;

            try
            {
                log.LogInformation($"Turning off VM: {curVM.Id}");
                curVM.PowerOff();
                log.LogInformation($"Turned off VM: {curVM.Id}");

                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error turning on VM {curSubName} -> {curTarget} -> {curVM.Name}: {err}");
                return false;
            }

        }

        static public List<ScheduledOperation> getSampleSchedule(ResourceGraphClient resourceGraphClient,List<string> subList, ILogger log)
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

                // Get VMs
                string vmListQuery = @"Resources 
                    | where type =~ 'Microsoft.Compute/virtualMachines'
                    | where tags.allowFaultInjection=~'true'
                    | project id
                    ";

                QueryResponse vmListResponse = resourceGraphClient.Resources(new QueryRequest(subList, vmListQuery));
                log.LogInformation($"Got VMs: {vmListResponse.Count}");
                JObject vmJObj = JObject.Parse(vmListResponse.Data.ToString());
                IList<string> vmIDList = vmJObj.SelectTokens("$.rows[*][0]").Select(s => (string)s).ToList();

                // Pick 1 VM
                int vmID = rnd.Next(vmIDList.Count);

                ScheduledOperation newOffOp = new ScheduledOperation(DateTime.Now, "Sample VM Off", "vm", "off", vmIDList[vmID]);
                results.Add(newOffOp);

                ScheduledOperation newOnOp = new ScheduledOperation(DateTime.Now.AddMinutes(5), "Sample VM On", "vm", "on", vmIDList[vmID]);
                results.Add(newOnOp);

                return results;
            }
        }

    }
}
