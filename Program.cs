using System;
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
                        "  - systemctl restart systemd-resolved\n" +
                        "  - sudo apt-get update\n" +
                        "  - sudo DEBIAN_FRONTEND=noninteractive apt-get -y install xfce4\n" +
                        "  - sudo apt install xfce4-session\n" +
                        "  - sudo apt-get -y install xrdp\n" +
                        "  - sudo systemctl enable xrdp\n" +
                        "  - sudo adduser xrdp ssl-cert\n" +
                        "  - echo xfce4-session >~/.xsession\n" +
                        "  - sudo service xrdp restart\n" +
                        $"  - echo 'azureuser:Thismypassword123456' | sudo chpasswd\n"
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
