#region Copyright Notice
/*
Copyright © Microsoft Open Technologies, Inc.
All Rights Reserved
Apache 2.0 License

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using AzureDeploymentCmdlets.WAPPSCmdlet;
using SolrDeployCmdlets.Utilities;
using SolrDeployCmdlets.ServiceConfigurationSchema;
using SolrDeployCmdlets.ServiceDefinitionSchema;
using System.Security.Permissions;
using AzureDeploymentCmdlets.Model;
using SolrDeployCmdlets.Model;
using System.ServiceModel;
using SolrDeployCmdlets.Properties;

namespace SolrDeployCmdlets.Solr.Cmdlet
{
    [Cmdlet(VerbsCommon.Set, "AzureSolrStorageAccount")]
    public class SetAzureSolrStorageAccountCommand : ServiceManagementCmdletBase
    {
        AzureService azureService;

        // Storage Account is mandatory
        [Parameter(Position = 0, HelpMessage = "New Windows Azure Storage Account Name", Mandatory = true)]
        [Alias("st")]
        public string StorageAccountName { get; set; }

        [Parameter(Position = 2, HelpMessage = "Windows Azure Storage Account Location", Mandatory = false)]
        [Alias("l")]
        public string StorageAccountLocation { get; set; }

        [Parameter(Position = 3, HelpMessage = "Windows Azure Subscription", Mandatory = false)]
        [Alias("sn")]
        public string Subscription { get; set; }

        [Parameter(Position = 4, HelpMessage = "Affinity group for storage account", Mandatory = false)]
        [Alias("ag")]
        public string AffinityGroup { get; set; }

        private Session session;
        private string cloudSvcConfigFileName;
        private string serviceDefinitionFileName;
        private ServiceConfiguration cloudSvcConfig;
        private ServiceDefinition svcDef;

        public SetAzureSolrStorageAccountCommand()
        {
        }

        public SetAzureSolrStorageAccountCommand(IServiceManagement channel)
        {
            base.Channel = channel;
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        protected override void ProcessRecord()
        {
            try
            {
                base.ProcessRecord();
                string result = this.SetSolrStorageAccountProcess(base.GetServiceRootPath());
                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, string.Empty, ErrorCategory.CloseError, null));
                throw;
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        private string SetSolrStorageAccountProcess(string rootPath)
        {
            InitializeArgs(rootPath);
            if (!StorageAccountExists(this.StorageAccountName))
            {
                CreateStorageAccount(this.StorageAccountName.ToLower(),
                    this.StorageAccountName.ToLower(),
                    azureService.Components.Settings.Location,
                    this.AffinityGroup);
                WriteObject(string.Format(Resources.AzureStorageAccountCreatedMessage, this.StorageAccountName));
            }
            else
            {
                WriteObject(string.Format(Resources.AzureStorageAccountAlreadyExistsMessage, this.StorageAccountName));
            }

            session = Session.GetSession(SessionState.Path.ParseParent(rootPath, null), false);
            serviceDefinitionFileName = General.Instance.SvcDefinitionFilePath(rootPath);
            cloudSvcConfigFileName = General.Instance.CloudSvcConfigFilePath(rootPath);

            // Loading *.cscfg and *csdef files
            cloudSvcConfig = General.Instance.ParseFile<ServiceConfiguration>(cloudSvcConfigFileName);
            svcDef = General.Instance.ParseFile<ServiceDefinition>(serviceDefinitionFileName);

            ConfigureRoleStorageAccountKeys();

            return string.Format(Resources.AzureStorageAccountConfiguredForSolrRoleMessage, this.StorageAccountName.ToLower());
        }

        private bool ConfigureRoleStorageAccountKeys()
        {
            string primaryKey;
            string secondaryKey;

            if (this.ExtractStorageKeys(this.subscriptionId, this.StorageAccountName, out primaryKey, out secondaryKey))
            {
                const string cloudStorageFormat = "DefaultEndpointsProtocol={0};AccountName={1};AccountKey={2}";
                string storageHttpKey = string.Format(cloudStorageFormat, "http", this.StorageAccountName, primaryKey);
                string storageHttpsKey = string.Format(cloudStorageFormat, "https", this.StorageAccountName, primaryKey);

                for (int i = 0; i < cloudSvcConfig.Role.Length; i++)
                {
                    ServiceConfigurationSchema.ConfigurationSetting newSetting;
                    newSetting = new ServiceConfigurationSchema.ConfigurationSetting() { name = Resources.DataConnectionString, value = storageHttpKey };
                    UpdateSetting(ref cloudSvcConfig.Role[i], newSetting);

                    newSetting = new ServiceConfigurationSchema.ConfigurationSetting() { name = Resources.DiagnosticsConnectionString, value= storageHttpsKey };
                    UpdateSetting(ref cloudSvcConfig.Role[i], newSetting);
                }
                General.SerializeXmlFile<ServiceConfiguration>(cloudSvcConfig, cloudSvcConfigFileName);
                return true;
            }
            return false;
        }

        private bool UpdateSetting(ref RoleSettings rs, ServiceConfigurationSchema.ConfigurationSetting cs)
        {
            bool done = false;
            int count = (rs.ConfigurationSettings == null) ? 0 : rs.ConfigurationSettings.Length;
            for (int i = 0; i < count; i++)
            {
                ServiceConfigurationSchema.ConfigurationSetting setting = rs.ConfigurationSettings[i];

                if (setting.name == cs.name)
                {
                    setting.value = cs.value;
                    done = true;
                }
            }
            return done;
        }

        private bool ExtractStorageKeys(string subscriptionId, string storageName, out string primaryKey, out string secondaryKey)
        {
            StorageService storageService = null;
            try
            {
                storageService = this.Channel.GetStorageKeys(
                    subscriptionId,
                    storageName);
            }
            catch (CommunicationException)
            {
                throw;
            }
            primaryKey = storageService.StorageServiceKeys.Primary;
            secondaryKey = storageService.StorageServiceKeys.Secondary;

            return true;
        }

        public bool StorageAccountExists(string storageAccountName)
        {
            StorageService storageService = null;

            try
            {
                storageService = this.RetryCall(s => this.Channel.GetStorageService(s, storageAccountName));
            }
            catch (CommunicationException)
            {
                // Don't write error message, this exception is to detect that there's no such endpoint
                //
                return false;
            }

            return (storageService != null);
        }

        public void CreateStorageAccount(string storageAccountName, string label, string location, string affinityGroup)
        {
            AzureStorageAccount createStorageAccount = new AzureStorageAccount();
            createStorageAccount.Create(this.certificate, this.subscriptionId, storageAccountName, affinityGroup: affinityGroup, location: location);
        }

        public void InitializeArgs(string rootPath)
        {
            azureService = new AzureService(rootPath, null);
            azureService.Components.Settings.Location = SetLocation();
            azureService.Components.Settings.Subscription = new GlobalComponents(GlobalPathInfo.GlobalSettingsDirectory).GetSubscriptionId(Subscription);
            this.subscriptionId = azureService.Components.Settings.Subscription;

            // Issue #101: what to do in case of having empty storage account name?
            azureService.Components.Settings.StorageAccountName = SetStorageAccountName();
        }

        private string SetLocation()
        {
            // Check if user provided location or not
            //
            if (string.IsNullOrEmpty(StorageAccountLocation))
            {
                // Check if there is no location set in service settings
                //
                if (string.IsNullOrEmpty(azureService.Components.Settings.Location))
                {
                    if (string.IsNullOrEmpty(this.AffinityGroup) == true)
                    {
                        // Randomly use "North Central US" or "South Central US"
                        //
                        int randomLocation = General.GetRandomFromTwo(
                            (int)AzureDeploymentCmdlets.Model.Location.NorthCentralUS,
                            (int)AzureDeploymentCmdlets.Model.Location.SouthCentralUS);
                        return ArgumentConstants.Locations[(AzureDeploymentCmdlets.Model.Location)randomLocation];
                    }
                    else
                    {
                        return string.Empty;
                    }
                }

                return azureService.Components.Settings.Location;
            }
            else
            {
                // If location is provided use it
                //
                return StorageAccountLocation;
            }

        }

        private string SetStorageAccountName()
        {
            // Check if user provided storage account name or not
            //
            if (string.IsNullOrEmpty(StorageAccountName))
            {
                // User have not provided name, check to see if settings have a storager name
                //
                if (string.IsNullOrEmpty(azureService.Components.Settings.StorageAccountName))
                {
                    // settings doesn't have such thing, use service name as default option
                    //
                    return azureService.ServiceName.ToLower();
                }
                else
                {
                    // Settings have default storage account name
                    //
                    return azureService.Components.Settings.StorageAccountName;
                }
            }
            else
            {
                // User have specified storage account name for this particular publish
                //
                return StorageAccountName.ToLower();
            }
        }
    }
}
