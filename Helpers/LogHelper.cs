using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AzureFaultInjector
{
    public class LogHelper
    {
        static ILogger myLog;
        public LogHelper(ILogger iLog)
        {
            myLog = iLog;
        }

        public void logEvent(string iResourceType, string iResource, string iAction)
        {
            FIActionLogEntry curAction = new FIActionLogEntry();
            curAction.resource = iResource;
            curAction.action = iAction;
            curAction.resourceType = iResourceType;
            string actionLogJSON = JsonSerializer.Serialize(curAction);
            LogAnalyticsHelper.logToLogAnalytics(actionLogJSON, myLog);
            

        }



        private class FIActionLogEntry
        {

            public DateTime eventTimeUTC { get; set; }
            public string resourceType { get; set; }
            public string resource { get; set; }
            public string action { get; set; }

            public FIActionLogEntry()
            {
                eventTimeUTC = DateTime.UtcNow;
            }
        }
    }
}
