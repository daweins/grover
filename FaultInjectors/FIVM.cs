using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Extensions.Logging;


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
                curVM.Start();
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
                curVM.PowerOff();
                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error turning on VM {curSubName} -> {curTarget} -> {curVM.Name}: {err}");
                return false;
            }

        }


    }
}
