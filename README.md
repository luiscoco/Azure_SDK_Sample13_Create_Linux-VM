# How to create a Linux Virtual Machine (Ubuntu Server) with Azure SDK for .NET. After creating the VM we will install VSCode, Google Chrome and .NET 8

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
                },
                new SecurityRuleData()
                {
                    Name = "AllowRDP",
                    Priority = 130,
                    Access = SecurityRuleAccess.Allow,
                    Direction = SecurityRuleDirection.Inbound,
                    Protocol = SecurityRuleProtocol.Tcp,
                    SourceAddressPrefix = "*",
                    SourcePortRange = "*",
                    DestinationAddressPrefix = "*",
                    DestinationPortRange = "3389", // RDP port
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

The PUBLIC key is available in the SSH key pair Azure resource

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/80a29552-2d79-452f-a478-0ec9b19616b0)

The PRIVATE key *.pem file is downloaded

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/eb04ab56-4640-42cc-8062-8c4d2ad1a4eb)

## 5. Copy the PUBLIC key in the C# source code

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/806a7569-29c4-41ea-93e6-00afd465197a)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/789f7cbe-5cab-429c-909a-0407ee9cd602)

## 6. Download the PRIVATE key *.pem file

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

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/4e7d44c4-44a0-407a-ad69-1f44ad2f3e7b)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/09e003e4-6199-4367-9ec2-dbd187b79ba4)

Then we upload the private key file

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/4a37d870-ace3-40a7-af08-f21804cd43d7)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/083d1475-c7cc-4b50-bd2b-668159438a2e)

The we press the "**Connect**" button. A new internet web browser window will be opened in a new tab with the Linux Virtual Machine connection.

## 10. Just-in-time policy required Microsoft Defender

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/cdfcd9a7-2305-48bf-83f1-82d869a81456)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/aff79ab7-47a9-4ece-8801-3823cd437283)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/14f3b082-78dc-43f1-b497-55c0bf4f0548)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/16e76bb0-ecea-46d6-841f-a2e4a276ad5e)

After requesting the access with the Just-in-time policy we can use the Azure CLI for accessing the Linux VM.

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/eb22d2e8-a528-41f7-a92e-76d3d8ffc975)

![image](https://github.com/luiscoco/Azure_SDK_Sample13_Create_Linux-VM/assets/32194879/281ecd40-869b-4be1-b668-6a0c7f49ae71)

We can connect to the Linux VM with these commands:

```
az ssh vm --ip 13.95.138.186
```

or

```
az ssh vm --resource-group myRgName --name myVM --subscription 846901e6-da09-45c8-98ca-7cca2353ff0e
```

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

## 12. How to install in the Ubuntu Server the GUI Desktop, VSCode, Google Chrome and .NET 8

### 12.1. Run these commands to stall "xfce" using "apt":

```bash
sudo apt-get update
sudo DEBIAN_FRONTEND=noninteractive apt-get -y install xfce4
sudo apt install xfce4-session
```

### 12.2. Install and configure a remote desktop server:

```bash
sudo apt-get -y install xrdp
sudo systemctl enable xrdp
sudo adduser xrdp ssl-cert
echo xfce4-session >~/.xsession
sudo service xrdp restart
```

### 12.3. Set a local user account password, for example:"**Thismypassword123456**"

```bash
sudo passwd azureuser
```

### 12.4. OPEN REMOTE DESKTOP CONNECTION

Now Open "**Remote Desktop Connection**" application and type the Azure VM **Public IP address** and the username "**azureuser**" and then press connect:

![image](https://github.com/luiscoco/Azure_VM_Ubuntu_with_GUI_Desktop/assets/32194879/b1d97b46-f61a-4b84-9ea2-6fb2077df1db)

Then enter the password "**Thismypassword123456**" to access the Linux GUI Desktop, it also requires another password, set the same one as before "**Thismypassword123456**":

![image](https://github.com/luiscoco/Azure_VM_Ubuntu_with_GUI_Desktop/assets/32194879/88da3f7f-0f29-449f-8f81-ecb7dffe64ce)

![image](https://github.com/luiscoco/Azure_VM_Ubuntu_with_GUI_Desktop/assets/32194879/d881368c-7876-4c09-851a-7e660470ae83)

**IMPORTANT NOTE**: if you cannot access:

Restart the VM 

![image](https://github.com/luiscoco/Azure_VM_Ubuntu_with_GUI_Desktop/assets/32194879/74422ae2-9c96-4f86-803e-cf634a9b917a)

And Check the access, 

![image](https://github.com/luiscoco/Azure_VM_Ubuntu_with_GUI_Desktop/assets/32194879/5f0bdce7-fe17-4078-b703-5c49f9b5f238)

After try/run again the  **Remote Desktop Connection**" application.

### 12.5. HOW TO INSTALL VSCODE

Open a Terminal Emulator window and run the following commands to install the VSCode application

![image](https://github.com/luiscoco/Azure_VM_Ubuntu_with_GUI_Desktop/assets/32194879/98e76ee1-5832-4536-9692-babdce81e9ad)

```bash
sudo apt install software-properties-common apt-transport-https wget
wget -q https://packages.microsoft.com/keys/microsoft.asc -O- | sudo apt-key add -
sudo add-apt-repository "deb [arch=amd64] https://packages.microsoft.com/repos/vscode stable main"
sudo apt update
sudo apt install code
```

For accessing VSCode type the command:

```
code
```

### 12.6. HOW TO INSTALL GOOGLE CHROME

```bash
wget https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb
sudo dpkg -i google-chrome-stable_current_amd64.deb
```


### 12.7. HOW TO INSTALL .NET 8 SDK

```bash
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo apt update
sudo apt install -y apt-transport-https
sudo apt update
sudo apt install -y dotnet-sdk-8.0

dotnet --version
```
