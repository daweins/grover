using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.IO;
using System.Net;
using System.Security.Cryptography;

using Microsoft.Extensions.Logging;

namespace AzureFaultInjector
{

    // Major hat tip to https://dejanstojanovic.net/aspnet/2018/february/send-data-to-azure-log-analytics-from-c-code/
    static public class LogAnalyticsHelper
    {
        const string LAAPIVersion = "2016-04-01";
        const string LAEndpoint = "https://{0}.ods.opinsights.azure.com/api/logs?api-version={1}";

        static public void logToLogAnalytics(string json, ILogger log)
        {
            try
            {
                string WorkspaceId = Environment.GetEnvironmentVariable("LAWorkspaceID");
                //string SharedKey    = Environment.GetEnvironmentVariable("LASharedKey");
                string LogType = Environment.GetEnvironmentVariable("LATable");
                string ApiVersion = LAAPIVersion;
                string requestUriString = String.Format(LAEndpoint, WorkspaceId, ApiVersion);
                DateTime dateTime = DateTime.UtcNow;
                string dateString = dateTime.ToString("r");
                string signature = GetSignature("POST", json.Length, "application/json", dateString, "/api/logs");
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUriString);
                request.ContentType = "application/json";
                request.Method = "POST";
                request.Headers["Log-Type"] = LogType;
                request.Headers["x-ms-date"] = dateString;
                request.Headers["Authorization"] = signature;
                byte[] content = Encoding.UTF8.GetBytes(json);
                using (Stream requestStreamAsync = request.GetRequestStream())
                {
                    requestStreamAsync.Write(content, 0, content.Length);
                }
                using (HttpWebResponse responseAsync = (HttpWebResponse)request.GetResponse())
                {
                    if (responseAsync.StatusCode != HttpStatusCode.OK && responseAsync.StatusCode != HttpStatusCode.Accepted)
                    {
                        Stream responseStream = responseAsync.GetResponseStream();
                        if (responseStream != null)
                        {
                            using (StreamReader streamReader = new StreamReader(responseStream))
                            {
                                throw new Exception(streamReader.ReadToEnd());
                            }
                        }
                    }
                }
                log.LogInformation($"Logged message to Log Analytics: Workspace {WorkspaceId}; Table {LogType}; Message: {json}");
            }
            catch (Exception err)
            {
                log.LogError($"Error logging to log analytics - ironic? Did you set the LAWorkspaceID, LASharedKey, LATable appSettings?: {err.ToString()}");
            }
        }

        static private string GetSignature(string method, int contentLength, string contentType, string date, string resource)
        {
            string WorkspaceId = Environment.GetEnvironmentVariable("LAWorkspaceID");
            string SharedKey = Environment.GetEnvironmentVariable("LASharedKey");

            string message = $"{method}\n{contentLength}\n{contentType}\nx-ms-date:{date}\n{resource}";
            byte[] bytes = Encoding.UTF8.GetBytes(message);

            using (HMACSHA256 encryptor = new HMACSHA256(Convert.FromBase64String(SharedKey)))
            {
                return $"SharedKey {WorkspaceId}:{Convert.ToBase64String(encryptor.ComputeHash(bytes))}";
            }
        }

    }


}

