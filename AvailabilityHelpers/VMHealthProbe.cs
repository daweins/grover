using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Collections.Generic;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


namespace AvailabilityHelpers
{
    public static class VMHealthProbe
    {
        static HttpClient httpClient = new HttpClient();
        [FunctionName("VMHealthProbe")]
        public static void Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"VMHealthProbe Timer trigger function executed at: {DateTime.Now}");
            string ipList = Environment.GetEnvironmentVariable("targetIPList");
            string lbName = Environment.GetEnvironmentVariable("slbName");
            string subListStr = Environment.GetEnvironmentVariable("targetSubscriptionID");
            string rgNameStr = Environment.GetEnvironmentVariable("targetRG");
            string clientId = Environment.GetEnvironmentVariable("clientId");
            string tenantId = Environment.GetEnvironmentVariable("tenantId");
            string clientPwd = Environment.GetEnvironmentVariable("clientPassword");
            string ingestConn = Environment.GetEnvironmentVariable("ingestConn");
            log.LogInformation($"Params: SubscriptionIDList: {subListStr}; RGList: {rgNameStr}");


            ServicePrincipalLoginInformation spi = new ServicePrincipalLoginInformation
            {
                ClientId = clientId,
                ClientSecret = clientPwd

            };
            AzureCredentials myAzCreds = new AzureCredentials(spi, tenantId, AzureEnvironment.AzureGlobalCloud);
            var myAz = Azure.Configure().Authenticate(myAzCreds).WithSubscription(subListStr);
            var myLB = myAz.LoadBalancers.GetByResourceGroup(rgNameStr, lbName);
            

            string backendName = "";
            string backendID = "";
            // TODO: fix this laziness
            foreach (string curBackendName in myLB.Backends.Keys)
            {
                backendName = curBackendName;
                backendID = myLB.Backends[backendName].Inner.Id;
            }
            IReadOnlyDictionary<string, string> LBDictionary = myLB.Backends[backendName].BackendNicIPConfigurationNames;

            // Populate an IP -> NIC dictionary
            Dictionary<string,string> myNICDictionary = new Dictionary<string, string>();
            foreach (string curNICID in LBDictionary.Keys)
            {
                // Get NIC IP
                // TODO: fix this laziness - remember the IPConfig

                var curNIC = myAz.NetworkInterfaces.GetById(curNICID);
                myNICDictionary[curNIC.PrimaryPrivateIP] = curNICID;
                log.LogInformation($"Adding NIC to dictionary: {curNIC.PrimaryPrivateIP } -> {curNICID}");
            }





            // Build a REST API token in case we need it to remove NICs from NLB

            string authContextURL = "https://login.windows.net/" + tenantId;
            var authenticationContext = new AuthenticationContext(authContextURL);
            var credential = new ClientCredential(clientId, clientPwd);
            var result = authenticationContext
                .AcquireTokenAsync("https://management.azure.com/", credential).Result;
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }
            string token = result.AccessToken;


            // TODO: fix lazy typing - parallel dictionaries are dangerous
            Dictionary<string, string> unhealthyIPtoNicID = new Dictionary<string, string>();


            //DEBUG
            //myNICDictionary.Clear();
            //unhealthyIPtoNicID["10.0.1.5"] = "/subscriptions/82e247ea-1d53-46d8-8ea4-374aa9dd4ae5/resourceGroups/lifelimb-rig-basic/providers/Microsoft.Network/networkInterfaces/rigbasic-vm1-networkInterface";


            for (int numLoops = 0; numLoops < 36000; numLoops++)
            {
                // Do Deletes
                List<string> delList = new List<string>();
                foreach (string curIP in myNICDictionary.Keys)
                {
                    // Poke each IP
                    string targetURL = $"http://{curIP}";
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(targetURL);
                    req.Timeout = 500;
                    bool isGood = false;
                    try
                    {
                        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                        {
                            if (resp.StatusCode == HttpStatusCode.OK)
                            {
                                isGood = true;
                                log.LogInformation($"IP {curIP} is good");

                            }
                            else
                            {
                                log.LogInformation($"IP {curIP} had status {resp.StatusCode.ToString()}");

                            }
                        }
                    }
                    catch
                    {
                        log.LogInformation($"IP {curIP} Timed out");

                    }
                    if (!isGood)
                    {
                        delList.Add(curIP);
                    }
                }
                foreach (string curIP in delList)
                {
                    try
                    {
                        log.LogInformation($"Removing IP {curIP} from SLB");
                        var delNICId = myNICDictionary[curIP];
                        // TODO: fix this laziness - remember the IPConfig


                        string nicManageURL = $"https://management.azure.com{delNICId}?api-version=2018-07-01";


                        httpClient.DefaultRequestHeaders.Remove("Authorization");
                        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                        HttpResponseMessage response = httpClient.GetAsync(nicManageURL).Result;

                        string statusCode = response.StatusCode.ToString();
                        log.LogInformation($"Got result for NIC template: {statusCode}");

                        string nicDef = response.Content.ReadAsStringAsync().Result;
                        log.LogInformation($"Got NIC template of size: {nicDef.Length}");

                        JObject nicJObj = JObject.Parse(nicDef);

                        // Add to the sick list for future re-adding
                        try
                        {
                            unhealthyIPtoNicID[curIP] = delNICId;
                            nicJObj["properties"]["ipConfigurations"][0]["properties"]["loadBalancerBackendAddressPools"].First.Remove();
                        }
                        catch
                        {
                            log.LogInformation($"Trying to remove an IP {curIP} from the backend that is already gone!");
                        }
                        string updateSLB = nicJObj.ToString();
                        log.LogInformation($"Altered NIC template");
                        StringContent content = new StringContent(updateSLB, System.Text.Encoding.UTF8, "application/json");
                        response = httpClient.PutAsync(nicManageURL, content).Result;

                        // string responseContent = response.Content.ReadAsStringAsync().Result;


                        statusCode = response.StatusCode.ToString();

                        log.LogInformation($"Removal Result: {statusCode}");


                        myNICDictionary.Remove(curIP);
                        log.LogInformation("Removed from health check");


                    }
                    catch (Exception err)
                    {
                        log.LogError($"Failure to remove IP {curIP}: {err.ToString()}");
                    }
                }
        
        



                // TODO consolidate adds + deletes using decomposition - too much repeated code
                // TOdo - add state so it takes a few successes to come back
                if (numLoops % 60 == 0)
                {
                    List<string> addList = new List<string>();
                    log.LogInformation($"Starting re-add check on {unhealthyIPtoNicID.Count} unhealthy items");
                    foreach(string curIP in unhealthyIPtoNicID.Keys)
                    {
                        string targetURL = $"http://{curIP}";
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(targetURL);
                        req.Timeout = 500;
                        bool isGood = false;
                        try
                        {
                            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                            {
                                if (resp.StatusCode == HttpStatusCode.OK)
                                {
                                    isGood = true;
                                    log.LogInformation($"IP {curIP} is good");

                                }
                                else
                                {
                                    log.LogInformation($"IP {curIP} had status {resp.StatusCode.ToString()}");

                                }
                            }
                        }
                        catch
                        {
                            log.LogInformation($"IP {curIP} Timed out");

                        }
                        if (isGood)
                        {
                            log.LogInformation($"IP {curIP} is now healthy - adding to the resurrection list");
                            addList.Add(curIP);
                        }

                    }

                    //DEBUG
                    //addList.Add("10.0.1.5");

                    // Do adds
                    foreach(string curIP in addList)
                    {
                        try
                        {
                            log.LogInformation($"Adding {curIP} back to backend pool");
                            string addNICId = unhealthyIPtoNicID[curIP];
                            string nicManageURL = $"https://management.azure.com{addNICId}?api-version=2018-07-01";


                            httpClient.DefaultRequestHeaders.Remove("Authorization");
                            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                            HttpResponseMessage response = httpClient.GetAsync(nicManageURL).Result;

                            string statusCode = response.StatusCode.ToString();
                            log.LogInformation($"Got result for NIC template: {statusCode}");

                            string nicDef = response.Content.ReadAsStringAsync().Result;
                            log.LogInformation($"Got NIC template of size: {nicDef.Length}");

                            JObject nicJObj = JObject.Parse(nicDef);





                            string beTemplate = @"[
            {
                ""id"": ""{BEID}""
            }
          ]".Replace("{BEID}",backendID);

                            JProperty beToken = new JProperty("loadBalancerBackendAddressPools", "");

                            JToken curBE = nicJObj["properties"]["ipConfigurations"][0]["properties"]["loadBalancerBackendAddressPools"];
                            if(curBE == null)
                            {
                                ((JObject)nicJObj["properties"]["ipConfigurations"][0]["properties"]).Add(beToken);
                            }
                            
                            nicJObj["properties"]["ipConfigurations"][0]["properties"]["loadBalancerBackendAddressPools"] = "{BETEMPLATE}";
                            log.LogInformation($"Added loadbalancer def: ");                         
                            string updateSLB = nicJObj.ToString();
                            updateSLB =  updateSLB.Replace("\"{BETEMPLATE}\"",beTemplate);
                            log.LogInformation($"Altered NIC template: {updateSLB}");
                            StringContent content = new StringContent(updateSLB, System.Text.Encoding.UTF8, "application/json");
                            response = httpClient.PutAsync(nicManageURL, content).Result;

                            // string responseContent = response.Content.ReadAsStringAsync().Result;


                            statusCode = response.StatusCode.ToString();

                            log.LogInformation($"Add Result: {statusCode}");

                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                log.LogInformation($"Add Result successful - removing from the sick list");
                                myNICDictionary[curIP] = unhealthyIPtoNicID[curIP];
                                unhealthyIPtoNicID.Remove(curIP);
                               
                                
                            }
                        }
                        catch(Exception addError)
                        {
                            log.LogError($"Error Adding {curIP}: {addError} ");
                        }


                    }

                }
                //TODO: make this sleep to the top of the second, not just guess
                System.Threading.Thread.Sleep(250);
            }
        }


    }

}
