using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Extensions.Logging;


namespace AzureFaultInjector
{
    class FIVM : FI
    {
        public FIVM (ILogger iLog, Microsoft.Azure.Management.Fluent.IAzure iAzure, string iRGName, string kustoConn, string kustoDBName, string kustoTableName) : base(iLog,iAzure, iRGName, kustoConn, kustoDBName, kustoTableName)
        {
            myResourceCollection = iAzure.VirtualMachines.ListByResourceGroup(curRGName);
            
            myTargetType = "VM";
        }

        protected override bool turnOn(Microsoft.Azure.Management.ResourceManager.Fluent.Core.IResource curResource)
        {
            Microsoft.Azure.Management.Compute.Fluent.IVirtualMachine curVM = (Microsoft.Azure.Management.Compute.Fluent.IVirtualMachine)curResource;

            try
            {
                curVM.Start();
                return true;
            }
            catch(Exception err)
            {
                log.LogError($"Error turning on VM {curSubName} -> {curRGName} -> {curVM.Name}: {err}");
                return false;
            }
        }

        protected override bool turnOff(Microsoft.Azure.Management.ResourceManager.Fluent.Core.IResource curResource)
        {
            Microsoft.Azure.Management.Compute.Fluent.IVirtualMachine curVM = (Microsoft.Azure.Management.Compute.Fluent.IVirtualMachine)curResource;

            try
            {
                curVM.PowerOff();
                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error turning on VM {curSubName} -> {curRGName} -> {curVM.Name}: {err}");
                return false;
            }

        }


    }
}
