# Grover
Azure Fuzzing, Fault Injection, and Availability Maximization


# Project Structure
Moving components out to seperate branches

- Fault Injector: [master-faultinjector](https://github.com/daweins/grover/tree/master-faultinjector)
- Rigs
- Monitoring Infrastructure
- Availability Helpers 

# How can I help?
We are looking for help in the following primary areas:
 - Fault Injector: There is a lot of room for small, short improvements to build new Azure Fault Injectors. Take a look at the matrix below, see what's missing that you want to build, and contribute! Some fault injectors are easier (VMs come with Off/On Fluent API) than others (Azure SQL requires DB failover & temporary removal & later restoration of Firewall rules to simulate failure).   Also, we are looking to create a scripting mechanism to allow for failure scenarios to be created. If you have requirements/ideas, please contribute!
 - Rigs: We are currently further in the Fault Injector than rigs. Realistic sample applications with real-world architectures would be extraordinarily useful. We'll take Terraform & ARM. 
 - Private Testing: If you don't feel comfortable donating your app as a rig, take the Fault Injector into your own environment (NOT production!!!) - add [Issues](https://github.com/daweins/grover/issues) here at Github, and/or add desired features to the [Backlog](https://github.com/daweins/grover/projects/1)  

# Fault Injectors
|Azure Component          |Single On|Single Off|AZ Failure|Regional Failure|Degraded Performance|Notes|
|:------------------------|:-------:|:--------:|:--------:|:--------------:|:------------------:|:----|
|AKS                      |         |          |          |                |                    | 
|Azure Redis              |         |          |          |                |                    | 
|Azure SQL                |   Yes   |  Yes     |  N/A     |                |                    |Triggers Geo-failover of DBs configured for this, then removes all Firewall rules to simulate failure. Restoration replaces the Firewall rules  
|CosmosDB                 |         |          |          |                |                    | Degradation - alter the RUs? 
|Functions                |   Yes   |  Yes     |          |     Yes        |                    | same implementation as web 
|Network                  |         |          |          |                |                    | Likely to use UDR - need to save existing UDR if in place 
|Network Security Group   |   Yes   |  Yes     |  N/A     |     Yes        |                    | Temporarily places a high priority blocking inbound/outbound rule to simulate network failure
|VM                       |   Yes   |  Yes     |  Yes     |     Yes        |                    | Todo - AvSet 
|VMSS                     |         |          |          | Can we iterate the VMs in the VMSS? |                    | Manual scale to 0, and remember the original scale setting for restoration? 
|Web                      |   Yes   |  Yes     |  N/A     |     Yes        |                    |  
