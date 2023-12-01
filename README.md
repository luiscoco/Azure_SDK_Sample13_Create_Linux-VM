# How to create a Linux Virtual Machine with Azure SDK for .NET

**NOTE:** for more information about VM with Azure SDK for .NET visit the URL 

https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/compute/Azure.ResourceManager.Compute/samples/Sample2_ManagingVirtualMachines.md

## 0. Prerequisites

Install .NET 8 SDK: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

Install Azure CLI: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli

Install VSCode: https://code.visualstudio.com/download

## 1. Create a new C# console .Net 8 application in VSCode

Open VSCode and run the command:

```
dotnet new console --framework net8.0
```

## 2. Load the Azure SDK libraries.

From the Nuget web page copy the commands to load the libraries: https://www.nuget.org/

Run these commands to load the libraries:

```
dotnet add package Azure.Identity --version 1.10.4
dotnet add package Azure.ResourceManager --version 1.9.0
dotnet add package Azure.ResourceManager.Network --version 1.6.0
dotnet add package Azure.ResourceManager.Compute --version 1.2.1
```

Now run the command:

```
dotnet restore
```

## 3. Input the C# source code.

```csharp
﻿using System;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;

class Program
{
    static async Task Main(string[] args)
    {
        ArmClient armClient = new ArmClient(new DefaultAzureCredential());
        SubscriptionResource subscription = await armClient.GetDefaultSubscriptionAsync();

        ResourceGroupCollection rgCollection = subscription.GetResourceGroups();
        string rgName = "myRgName";
        AzureLocation location = AzureLocation.WestEurope;
        ResourceGroupResource resourceGroup = await rgCollection.CreateOrUpdate(WaitUntil.Started, rgName, new ResourceGroupData(location)).WaitForCompletionAsync();

        PublicIPAddressCollection publicIPAddressCollection = resourceGroup.GetPublicIPAddresses();
        string publicIPAddressName = "20.61.0.157";
        PublicIPAddressData publicIPInput = new PublicIPAddressData()
        {
            Location = resourceGroup.Data.Location,
            PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
            DnsSettings = new PublicIPAddressDnsSettings()
            {
                DomainNameLabel = "mydomain12319741999"
            }
        };
        PublicIPAddressResource publicIPAddress = await publicIPAddressCollection.CreateOrUpdate(WaitUntil.Completed, publicIPAddressName, publicIPInput).WaitForCompletionAsync();

        VirtualNetworkCollection virtualNetworkCollection = resourceGroup.GetVirtualNetworks();
        string vnetName = "myVnet";
        VirtualNetworkData input = new VirtualNetworkData()
        {
            Location = resourceGroup.Data.Location,
            AddressPrefixes = { "10.0.0.0/16", },
            DhcpOptionsDnsServers = { "10.1.1.1", "10.1.2.4" },
            Subnets = { new SubnetData() { Name = "mySubnet", AddressPrefix = "10.0.1.0/24", } }
        };
        VirtualNetworkResource vnet = await virtualNetworkCollection.CreateOrUpdate(WaitUntil.Completed, vnetName, input).WaitForCompletionAsync();

        VirtualNetworkCollection virtualNetworkCollection1 = resourceGroup.GetVirtualNetworks();
        VirtualNetworkResource virtualNetwork1 = await virtualNetworkCollection1.GetAsync("myVnet");
        Console.WriteLine(virtualNetwork1.Data.Name);

        NetworkInterfaceCollection networkInterfaceCollection = resourceGroup.GetNetworkInterfaces();
        string networkInterfaceName = "myNetworkInterface";
        NetworkInterfaceData networkInterfaceInput = new NetworkInterfaceData()
        {
            Location = resourceGroup.Data.Location,
            IPConfigurations = {
                new NetworkInterfaceIPConfigurationData()
                {
                    Name = "ipConfig",
                    PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    PublicIPAddress = new PublicIPAddressData()
                    {
                        Id = publicIPAddress.Id
                    },
                    Subnet = new SubnetData()
                    {
                        Id = virtualNetwork1.Data.Subnets[0].Id
                    }
                }
            }
        };

        NetworkSecurityGroupCollection nsgCollection = resourceGroup.GetNetworkSecurityGroups();
        string nsgName = "myNetworkSecurityGroup";
        NetworkSecurityGroupData nsgInput = new NetworkSecurityGroupData()
        {
            Location = resourceGroup.Data.Location,
            SecurityRules =
            {
                new SecurityRuleData()
                {
                    Name = "AllowSSH",
                    Priority = 100,
                    Access = SecurityRuleAccess.Allow,
                    Direction = SecurityRuleDirection.Inbound,
                    Protocol = SecurityRuleProtocol.Tcp,
                    SourceAddressPrefix = "*",
                    SourcePortRange = "*",
                    DestinationAddressPrefix = "*",
                    DestinationPortRange = "22", // SSH port
                },
                new SecurityRuleData()
                {
                    Name = "AllowHTTP",
                    Priority = 110,
                    Access = SecurityRuleAccess.Allow,
                    Direction = SecurityRuleDirection.Outbound,
                    Protocol = SecurityRuleProtocol.Tcp,
                    SourceAddressPrefix = "*",
                    SourcePortRange = "*",
                    DestinationAddressPrefix = "*",
                    DestinationPortRange = "80", // HTTP port
                },
                new SecurityRuleData()
                {
                    Name = "AllowHTTPS",
                    Priority = 120,
                    Access = SecurityRuleAccess.Allow,
                    Direction = SecurityRuleDirection.Outbound,
                    Protocol = SecurityRuleProtocol.Tcp,
                    SourceAddressPrefix = "*",
                    SourcePortRange = "*",
                    DestinationAddressPrefix = "*",
                    DestinationPortRange = "443", // HTTPS port
                }
            }
        };

        NetworkSecurityGroupResource nsg = await nsgCollection.CreateOrUpdate(WaitUntil.Completed, nsgName, nsgInput).WaitForCompletionAsync();

        networkInterfaceInput.NetworkSecurityGroup = new NetworkSecurityGroupData()
        {
            Id = nsg.Id
        };

        NetworkInterfaceResource networkInterface = await networkInterfaceCollection.CreateOrUpdate(WaitUntil.Completed, networkInterfaceName, networkInterfaceInput).WaitForCompletionAsync();

        VirtualMachineCollection vmCollection = resourceGroup.GetVirtualMachines();
        string vmName = "myVM";
        string adminusername = "azureuser";
        VirtualMachineData input2 = new VirtualMachineData(resourceGroup.Data.Location)
        {
            HardwareProfile = new VirtualMachineHardwareProfile()
            {
                VmSize = VirtualMachineSizeType.StandardE2SV3
            },
            OSProfile = new VirtualMachineOSProfile()
            {
                AdminUsername = adminusername,
                ComputerName = "myVM",
                CustomData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                        "#cloud-config\n" +
                        "write_files:\n" +
                        "  - path: /etc/systemd/resolved.conf\n" +
                        "    content: |\n" +
                        "      [Resolve]\n" +
                        "      DNS=8.8.8.8 8.8.4.4\n" +
                        "runcmd:\n" +
                        "  - systemctl restart systemd-resolved\n"
                )),
                LinuxConfiguration = new LinuxConfiguration()
                {
                    DisablePasswordAuthentication = true,
                    SshPublicKeys = {
                        new SshPublicKeyConfiguration()
                        {
                            Path = $"/home/" + adminusername + "/.ssh/authorized_keys",
                            KeyData = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABgQC62swVPqUSWDldLCH/UelaV5hBQ7K2UjumZcVO+B4qjL3mCgN2oBtXXEXVI+i3xVDCr7E/sW9g9wxWUUvaENtTXLJTUPPwcmeJeGppxcTFFbf258LAkXV9Gh2fbaDw91DYmXbUIrRCiK7QMvSitEgjJmfrZd8p6a9bNWFPsNIgR7QbpFiTEdsuk4iVX25IA7Tu41c85D6xBsVdy7+nMzLFbP+axb57JWKk7DboRESqb+1YVtrygBRqok30porTCRnbIEu+Z3E5dDxwslCMvpiKcvjG8oAY/90rT4G7GBN5kVgfqdZtI8/uekS1kjRGkGCo2Ymjzj0x1STYVGeyriQurOeHgDKUuK1aaQAnde4z4x8fzwpd6TnTwpP/odEEPKY2dI6rRCcIj6Fl1DCaJZfq70zrwqOmJJn+OCjdYvHzUda+ACeb4g6MAwefEBq3+rZgX78sWwzw5+akhIchuU52P0k7KbPeDplltl3bN8GUEu3gdZTI3/eVk6fLyW5IEOU= generated-by-azure", //<value of the public ssh key>
                        }
                    }                    
                }
            },
            NetworkProfile = new VirtualMachineNetworkProfile()
            {
                NetworkInterfaces =
                {
                    new VirtualMachineNetworkInterfaceReference()
                    {
                        Id = new ResourceIdentifier($"/subscriptions/{subscription.Data.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.Network/networkInterfaces/{networkInterfaceName}"),
                        Primary = true,
                    }
                }
            },
            StorageProfile = new VirtualMachineStorageProfile()
            {
                OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                {
                    OSType = SupportedOperatingSystemType.Linux,
                    Caching = CachingType.ReadWrite,
                    ManagedDisk = new VirtualMachineManagedDisk()
                    {
                        StorageAccountType = StorageAccountType.StandardLrs
                    }
                },
                ImageReference = new ImageReference()
                {
                    Publisher = "Canonical",
                    Offer = "0001-com-ubuntu-server-jammy",
                    Sku = "22_04-lts-gen2",
                    Version = "latest",
                }
            }
        };

        ArmOperation<VirtualMachineResource> lro = await vmCollection.CreateOrUpdateAsync(WaitUntil.Completed, vmName, input2);
        VirtualMachineResource vm = lro.Value;
    }
}
```

## 4. Create a new SSH key pair in Azure Portal

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/8723cdfe-5adc-4c4f-b196-e6fed73f65d2)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/6edd1d6e-d993-4853-8a38-92d872e41475)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/64a6afbe-6cea-4640-a454-63596a3d4172)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/47784a10-90c0-4048-8963-4c76e756e1a8)

## 5. Copy the public key in the C# source code

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/806a7569-29c4-41ea-93e6-00afd465197a)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/789f7cbe-5cab-429c-909a-0407ee9cd602)

## 6. Download the private key *.pem file

Copy the private key file (*.pem) and paste it in: "C:\Users\LEnriquez\.ssh"

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/199af886-9be5-48ca-9972-19e5af83242b)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/c899d04f-1bd9-4c5f-a3bb-81c5d06684fc)

## 7. Build and run the application

Execute the command:

```
dotnet run
```

## 8. Access to the Virtual Machine 

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/5af957a3-c06f-4ca6-b75d-a25bd5d4cec4)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/10dfda0a-8cdb-41a6-bea7-4324c7c38a22)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/e7a93764-61e0-48ac-82a0-e228d78b6c60)


## 9. Access with bastion



Then we upload the private key file



The we press the "Connect" button. A new internet web browser window will be opened in a new tab with the Linux Virtual Machine connection.




## 10. Just-in-time policy required Microsoft Defender



## 11. Acces from Linux Virtual Machine to Internet

Be sure your Linux Virtual Machine has access to internet. To do so run the following commands: 

```
ping 8.8.8.8
```

Also try to run this command:

```
nslookup google.com
```

If you cannot connect to internet then run the command:

```
sudo nano /etc/systemd/resolved.conf
```

Then uncomment the line: 

```
[Resolve]
DNS=8.8.8.8 8.8.4.4
```

The file will be like this:

```
#  This file is part of systemd.
#
#  systemd is free software; you can redistribute it and/or modify it under the
#  terms of the GNU Lesser General Public License as published by the Free
#  Software Foundation; either version 2.1 of the License, or (at your option)
#  any later version.
#
# Entries in this file show the compile time defaults. Local configuration
# should be created by either modifying this file, or by creating "drop-ins" in
# the resolved.conf.d/ subdirectory. The latter is generally recommended.
# Defaults can be restored by simply deleting this file and all drop-ins.
#
# Use 'systemd-analyze cat-config systemd/resolved.conf' to display the full config.
#
# See resolved.conf(5) for details.

[Resolve]
# Some examples of DNS servers which may be used for DNS= and FallbackDNS=:
# Cloudflare: 1.1.1.1#cloudflare-dns.com 1.0.0.1#cloudflare-dns.com 2606:4700:4700::1111#cloudflare-dns.com 2606:4700:4># Google:     8.8.8.8#dns.google 8.8.4.4#dns.google 2001:4860:4860::8888#dns.google 2001:4860:4860::8844#dns.google
# Quad9:      9.9.9.9#dns.quad9.net 149.112.112.112#dns.quad9.net 2620:fe::fe#dns.quad9.net 2620:fe::9#dns.quad9.net
DNS=8.8.8.8 8.8.4.4
#FallbackDNS=
#Domains=
#DNSSEC=no
#DNSOverTLS=no
```

Then press **Ctrl + o** for saving the file, and then press **Enter** and finally press **Ctrl + x** to exit the nano editor

Then type the command:

```
sudo systemctl restart systemd-resolved
```

Then try again to connect with the command:

```
nslookup google.com
```

You should get this output:

```
azureuser@myVM:~$ nslookup google.com
Server:         127.0.0.53
Address:        127.0.0.53#53

Non-authoritative answer:
Name:   google.com
Address: 142.251.36.14
Name:   google.com
Address: 2a00:1450:400e:811::200e
```


