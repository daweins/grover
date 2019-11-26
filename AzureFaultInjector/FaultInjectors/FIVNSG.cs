using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Extensions.Logging;


namespace AzureFaultInjector
{
    class FINSG : FI
    {

        static string myNewRuleName = "FuzzerBlockingRule";
        static int myNewRulePriority = 100;


        public FINSG (ILogger iLog, Microsoft.Azure.Management.Fluent.IAzure iAzure, string iRGName, string kustoConn, string kustoDBName, string kustoTableName) : base(iLog,iAzure, iRGName, kustoConn, kustoDBName, kustoTableName)
        {
            myResourceCollection = iAzure.NetworkSecurityGroups.ListByResourceGroup(iRGName);
            
            myTargetType = "NSG";
        }


        protected override bool turnOn(Microsoft.Azure.Management.ResourceManager.Fluent.Core.IResource curResource)
        {
            Microsoft.Azure.Management.Network.Fluent.INetworkSecurityGroup curNSG = (Microsoft.Azure.Management.Network.Fluent.INetworkSecurityGroup)curResource;

            try
            {
                log.LogInformation($"Removing new highest priority blocking rule to NSG {curNSG.Name}");


                curNSG.Update().WithoutRule(myNewRuleName).Apply();
                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error unblocking NSG {curSubName} -> {curRGName} -> {curNSG.Name}: {err}");
                return false;
            }

        }

        protected override bool turnOff(Microsoft.Azure.Management.ResourceManager.Fluent.Core.IResource curResource)
        {
            Microsoft.Azure.Management.Network.Fluent.INetworkSecurityGroup curNSG = (Microsoft.Azure.Management.Network.Fluent.INetworkSecurityGroup)curResource;

            try
            {
                // Add a blocking rule
                log.LogInformation($"Adding new highest priority blocking rule to NSG {curNSG.Name}");
                curNSG.Update().DefineRule(myNewRuleName)
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
                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error blocking NSG {curSubName} -> {curRGName} -> {curNSG.Name}: {err}");
                return false;
            }
        }

    }
}
