using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Threading;



namespace AzureFaultInjector
{
    public static class AzureFaultInjector
    {
        static Random rnd = new Random();


        [FunctionName("AzureFaultInjector")]
        public static void Run([TimerTrigger("0 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"Fault Injector - VM function started at: {DateTime.Now}");
                string subListStr   = Environment.GetEnvironmentVariable("targetSubscriptionIDList");
                string rgListStr    = Environment.GetEnvironmentVariable("targetRGList");
                string clientId     = Environment.GetEnvironmentVariable("clientId");
                string tenantId     = Environment.GetEnvironmentVariable("tenantId");
                string clientPwd    = Environment.GetEnvironmentVariable("clientPassword");
                string ingestConn   = Environment.GetEnvironmentVariable("ingestConn");
                int vmFuzzPct       = Convert.ToInt32(Environment.GetEnvironmentVariable("vmFuzzPct"));
                int nsgFuzzPct      = Convert.ToInt32(Environment.GetEnvironmentVariable("nsgFuzzPct"));

                log.LogInformation($"Params: SubscriptionIDList: {subListStr}; RGList: {rgListStr}");

                ServicePrincipalLoginInformation spi = new ServicePrincipalLoginInformation
                {
                    ClientId = clientId,
                    ClientSecret = clientPwd

                };
                AzureCredentials myAzCreds = new AzureCredentials(spi, tenantId, AzureEnvironment.AzureGlobalCloud);
                Microsoft.Azure.Management.Fluent.IAzure myAz = Azure.Configure().Authenticate(myAzCreds).WithSubscription(subListStr);

                foreach (string curRGName in rgListStr.Split(","))
                {
                    log.LogInformation($"Iterating over RG {curRGName}");
                    FIVM vmFuzzer = new FIVM(log, myAz, curRGName, ingestConn, "annandale", "faultInjections");
                    vmFuzzer.Fuzz(1);

                    FINSG nsgFuzzer = new FINSG(log, myAz, curRGName, ingestConn, "annandale", "faultInjections");
                    nsgFuzzer.Fuzz(25);
                }


                log.LogInformation($"Fault Injector - VM function finished at: {DateTime.Now}");

            }
            catch(Exception err)
            {
                log.LogError(err.ToString());
            }
        }
    }
}
