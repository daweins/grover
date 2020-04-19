using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Azure.Management.ResourceGraph.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Threading.Tasks;


namespace AzureFaultInjector
{
    public abstract class FI
    {

        public static Random rnd = new Random();


        protected string curSubName;
        protected string curTarget;
        protected Microsoft.Azure.Management.Fluent.IAzure myAzure;
        protected ILogger log;
        protected LogHelper myLogHelper;


        // Must be defined by subclass
        // TODO: Enforce this
        public static string myTargetType = "Unknown";
        protected Microsoft.Azure.Management.ResourceManager.Fluent.Core.IResource myResource = null;


        abstract protected bool turnOn(ScheduledOperation curOp);
        abstract protected bool turnOff(ScheduledOperation curOp);

        // This should be overridden by most implementations. C# doesn't have abstract statics, or I'd use that. 
        static public List<ScheduledOperation> getSampleSchedule(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, ILogger log)
        {
            return new List<ScheduledOperation>();
        }
        // This should be overridden by most implementations. C# doesn't have abstract statics, or I'd use that. 
        static public List<ScheduledOperation> killAZ(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, int azToKill, long startTicks, long endTicks, ILogger log)
        {
            return new List<ScheduledOperation>();
        }
        static public List<ScheduledOperation> killRegion(Microsoft.Azure.Management.Fluent.IAzure myAz, List<IResourceGroup> rgList, string regionToKill, long startTicks, long endTicks, ILogger log)
        {
            return new List<ScheduledOperation>();
        }

        protected FI(ILogger iLog, Microsoft.Azure.Management.Fluent.IAzure iAzure, string iTarget)
        {
            curTarget = iTarget;
            log = iLog;
            curSubName = iAzure.SubscriptionId;
            myLogHelper = new LogHelper(iLog);


        }




        public bool processOp(ScheduledOperation curOp)
        {
            switch (curOp.operation)
            {
                case "on":
                    this.turnOn(curOp);
                    return true;
                case "off":
                    this.turnOff(curOp);
                    return true;
                default:
                    log.LogError($"Unknown op: {curOp.ToString()} for {this.ToString()}");
                    break;
            }
            return false;
        }


        // Used to allow for easy "plugin" extension
        static public IEnumerable<Type> getSubTypes()
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(FI).IsAssignableFrom(t));
        }

        static public Type getFIType(string targetType)
        {
            foreach (Type curFI in FI.getSubTypes())
            {
                string curTargetType = (string)curFI.GetField("myTargetType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null);
                if (curTargetType == targetType)
                {
                    return (curFI);
                }
            }
            return null;
        }


    }



}
