using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Azure.Management.ResourceGraph.Models;
using Newtonsoft.Json.Linq;
using System.Linq;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Threading.Tasks;

namespace AzureFaultInjector
{
    class FIWeb : FI
    {

        static string myTargetType = "Web";
        public FIWeb (ILogger iLog, Microsoft.Azure.Management.Fluent.IAzure iAzure, string iTarget) : base(iLog,iAzure, iTarget)
        {
            try
            {
                myResource = iAzure.WebApps.GetById(iTarget);
            }
            catch(Exception err)
            {
                log.LogError($"Error in {myTargetType} constructor: {err.ToString()}");
            }
}

        protected override bool turnOn(string payload)
        {
            Microsoft.Azure.Management.AppService.Fluent.IWebApp curWeb = (Microsoft.Azure.Management.AppService.Fluent.IWebApp)myResource;

            try
            {
                log.LogInformation($"Turning on Web: {curWeb.Id}");
                curWeb.Start();
                log.LogInformation($"Turned on Web: {curWeb.Id}");

                return true;
            }
            catch(Exception err)
            {
                log.LogError($"Error turning on Web {curSubName} -> {curTarget} -> {curWeb.Name}: {err}");
                return false;
            }
        }

        protected override bool turnOff(int numMinutes = 5)
        {
            Microsoft.Azure.Management.AppService.Fluent.IWebApp curWeb = (Microsoft.Azure.Management.AppService.Fluent.IWebApp)myResource;

            try
            {
                if(curWeb.State == "Running")
                {
                    log.LogInformation($"Turning off Web: {curWeb.Id}");
                    curWeb.StopAsync();   // We don't really care if this fails - worst case we turn it on when it's already on
                    log.LogInformation($"Turned off Web: {curWeb.Id}. Creating the compensating On action");
                    ScheduledOperation onOp = new ScheduledOperation(DateTime.Now.AddMinutes(numMinutes), $"Compensating On action for turning off a {myTargetType}", myTargetType, "on", curTarget);
                    ScheduledOperationHelper.addSchedule(onOp, log);

                }
                else
                {
                    log.LogInformation($"Turning off Web {curWeb.Id}, but it was already not running");
                }
                return true;
            }
            catch (Exception err)
            {
                log.LogError($"Error turning on Web {curSubName} -> {curTarget} -> {curWeb.Name}: {err}");
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

                log.LogInformation("Adding a Web sample schedule this iteration");

                // Pick a random RG from the list
                int rgIndex = rnd.Next(rgList.Count);
                List<IWebApp> WebList = new List<IWebApp>(  myAz.WebApps.ListByResourceGroup(rgList[rgIndex].Name));
                if (WebList.Count > 0)
                {
                    // Pick a random Web from the RG
                    int WebID = rnd.Next(WebList.Count);

                    ScheduledOperation newOffOp = new ScheduledOperation(DateTime.Now, $"Sample {myTargetType} Off", myTargetType, "off", WebList[WebID].Id);
                    results.Add(newOffOp);
                }
                return results;
            }
        }

        static public List<ScheduledOperation> killAZ(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, int azToKill, ILogger log)
        {

            List<ScheduledOperation> results = new List<ScheduledOperation>();
            log.LogInformation($"Web AZKill for Zone {azToKill}: Nothing to do - App Service is zonally redundant");
            // TODO - should we flip it on & off quickly to simulate the potential time until the SLB notices the failed backend? can we flip it fast enough?

            return results;
        }


        static public List<ScheduledOperation> killRegion(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, string regionToKill, ILogger log)
        {
            List<ScheduledOperation> results = new List<ScheduledOperation>();
            foreach (IResourceGroup curRG in rgList)
            {
                log.LogInformation($"Web Region Kill for region:  {regionToKill}: checking RG: {curRG.Name}");
                List<IWebApp> WebList = new List<IWebApp>(myAz.WebApps.ListByResourceGroup(curRG.Name));
                foreach (IWebApp curWeb in WebList)
                {
                    if (curWeb.RegionName == regionToKill)
                    {
                        log.LogInformation($"RegionKill: Got a Region {regionToKill} match for {curWeb.Id} - scheduling for termination");
                        ScheduledOperation newOffOp = new ScheduledOperation(DateTime.Now, $"Killing Region {regionToKill} - {myTargetType} Off", myTargetType, "off", curWeb.Id);
                        results.Add(newOffOp);
                    }
                }

            }
            return results;
        }

    }
}
