using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace AzureFaultInjector
{
    public class SQLFirewallDefinition
    {

        // Make sure all new properties are properties, not fields, so they get serialized to JSON
        public  List<SQLFirewallRule> ruleList { get; set; }
        public bool allowAzureAccess { get; set;  }

      
        public SQLFirewallDefinition()
        {
            ruleList = new List<SQLFirewallRule>();
        }

        public void addRule(string iName, string iStartIP, string iEndIP)
        {
            SQLFirewallRule newRule = new SQLFirewallRule(iName, iStartIP, iEndIP);
            ruleList.Add(newRule);
        }



        public string toJSON()
        {
            return JsonSerializer.Serialize(this);
        }

        static public SQLFirewallDefinition deserialize(string iJSON)
        {
            return JsonSerializer.Deserialize<SQLFirewallDefinition>(iJSON);
        }
    }



    public class SQLFirewallRule
    {
        public string name { get; set; }
        public string startIP { get; set; }
        public string endIP { get; set; }


        public SQLFirewallRule()
        {
            name = "unknown";
            startIP = "unknown";
            endIP = "unknown";
        }

        public SQLFirewallRule(string iName, string iStartIP, string iEndIP)
        {
            name = iName;
            startIP = iStartIP;
            endIP = iEndIP;
        }
    }
}
