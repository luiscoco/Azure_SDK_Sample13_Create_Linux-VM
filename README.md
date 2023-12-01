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


![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/36a8afba-58cb-490d-bc13-38bb0852992d)



![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/25bf248d-6d15-459f-a743-153e34d78eef)



![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/f5912320-fb8a-408c-b2c7-588a6591d66c)




![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/ce62f6d7-7080-44da-b366-8b82dcf341ab)



![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/21de7513-ca00-4b7b-8a1a-a6654744924f)





## 5. Copy the public key in the C# source code


![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/e572d73e-31e1-4312-b7d5-245a4cfe18d4)



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

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/8a33c493-66d7-4620-ad51-32abe1cda284)


![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/8b634a8e-a8bc-45b8-97c0-c246f85e463a)


![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/47f652c9-b02e-412b-8073-1abe971d769c)


![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/aca33308-2d83-4015-86f5-7166075de4b2)


![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/3cfae7b2-a679-44b5-bbe7-ca70b83a3c99)


![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/ab2f0633-5e78-4767-b8c0-3c31a40e0843)


![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/a3429d84-c654-4ab1-8514-263b739329fb)


![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/3a77c32a-a801-45c2-8b51-460ff0e3cb8e)


## 9. Access with bastion

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/946cf99b-8eab-4b57-accc-821890ac2557)

Then we upload the private key file

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/de6c1653-4765-4bc1-81c8-6639bb7be121)

The we press the "Connect" button. A new internet web browser window will be opened in a new tab with the Linux Virtual Machine connection.

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/ca9ffea1-14d1-49b3-9285-5b828af3eeda)


## 10. Just-in-time policy required Microsoft Defender

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/571f9c85-979a-40e4-9e11-86e18bd1db3a)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/a135dbd5-677c-4847-bb30-54c64afc8a2d)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/713fea6a-9dd6-492b-a4c4-3d85e7898430)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/bd705364-2584-4937-913e-b468be9c2932)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/9bf453ca-4a20-4c41-9c4b-616c53e14c18)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/569fab33-1e49-464e-9e6c-ace47aa4bf8e)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/b86be30c-3709-4382-87ad-9afd4f6882e2)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_VM/assets/32194879/e22d0bd5-838f-4f96-a245-19577c2b52de)

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


