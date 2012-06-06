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
using AzureDeploymentCmdlets.Model;
using System.Management.Automation;
using SolrDeployCmdlets.Utilities;
using SolrDeployCmdlets.ServiceConfigurationSchema;
using SolrDeployCmdlets.ServiceDefinitionSchema;
using SolrDeployCmdlets.Properties;
using System.Xml.Serialization;
using System.Reflection;
using System.IO;
using AzureDeploymentCmdlets.Scaffolding;
using Microsoft.Win32;

namespace SolrDeployCmdlets.Solr.Cmdlet
{
    [Cmdlet(VerbsCommon.Add, "AzureSolrWebRole")]
    public class AddAzureSolrWebRole : AddRole
    {
        private string azureSolrInstallFolder;

        [Parameter(Mandatory = false, HelpMessage = "Set the VMSize for Solr Web role.")]
        public RoleSize? Size { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                base.ProcessRecord();
                string result = this.addSolrWebRole();
                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, String.Empty, ErrorCategory.CloseError, null));
            }
        }

        private string addSolrWebRole()
        {
            azureSolrInstallFolder = General.Instance.AzureSolrInstallFolder();
            string serviceRootPath = base.GetServiceRootPath();

            string localSvcConfigFileName = General.Instance.LocalSvcConfigFilePath(serviceRootPath);
            string cloudSvcConfigFileName = General.Instance.CloudSvcConfigFilePath(serviceRootPath);
            string serviceDefinitionFileName = General.Instance.SvcDefinitionFilePath(serviceRootPath);

            string roleName;
            int webRoleOccurrence;
            int workerRoleOccurrence;
            ServiceConfiguration localSvcConfig;
            ServiceConfiguration cloudSvcConfig;
            ServiceDefinition svcDef;

            RoleSettings roleSettings;
            WebRole webRole;

            localSvcConfig = General.Instance.ParseFile<ServiceConfiguration>(localSvcConfigFileName);
            cloudSvcConfig = General.Instance.ParseFile<ServiceConfiguration>(cloudSvcConfigFileName);
            svcDef = General.Instance.ParseFile<ServiceDefinition>(serviceDefinitionFileName);

            GetRoleOccurrence(svcDef, out webRoleOccurrence, out workerRoleOccurrence);
            bool updated = false;

            roleName = this.Name;
            if (string.IsNullOrEmpty(this.Name))
            {
                // Use default SolrAdminWebRole
                roleName = Resources.WebRoleName;
            }

            string message = string.Format(Resources.AddAzureSolrWebRoleSuccessMessage,
                (this.Name == null ? Resources.WebRoleName : this.Name), serviceRootPath);

            if (webRoleOccurrence > 0)
            {
                foreach (RoleSettings role in localSvcConfig.Role)
                {
                    if (role.name == roleName)
                    {
                        role.Instances.count = Instances;
                        updated = true;
                        break;
                    }
                }
                foreach (RoleSettings role in cloudSvcConfig.Role)
                {
                    if (role.name == roleName)
                    {
                        role.Instances.count = Instances;
                        updated = true;
                    }
                }
            }

            if (updated == false)
            {
                // Get default RoleSettings template
                roleSettings = GetRoleCSCFG(roleName);

                // Add role to local and cloud *.cscfg
                AddNewRole(localSvcConfig, roleSettings);
                AddNewRole(cloudSvcConfig, roleSettings);

                // Get default WebRole template
                GetWebRoleCSDEF(roleName, ref webRoleOccurrence, out webRole);
                webRole.vmsize = Size == null ? RoleSize.Small : Size.Value;

                AddNewWebRole(svcDef, webRole);

                CreateScaffolding(roleName);
            }
            else
            {
                message = string.Format(Resources.UpdateAzureSolrWebRoleSuccessMessage,
                    (this.Name == null ? Resources.WebRoleName : this.Name), this.Instances, serviceRootPath);
            }

            // Serialize local.cscfg
            General.SerializeXmlFile<ServiceConfiguration>(localSvcConfig, localSvcConfigFileName);
            General.SerializeXmlFile<ServiceConfiguration>(cloudSvcConfig, cloudSvcConfigFileName);
            General.SerializeXmlFile<ServiceDefinition>(svcDef, serviceDefinitionFileName);

            return message;
        }

        private void CreateScaffolding(string roleFolderName)
        {
            string sourceDir = Path.Combine(azureSolrInstallFolder, Resources.SolrWebRoleScaffoldFolder);
            string destinationDir = Path.Combine(base.GetServiceRootPath(), roleFolderName);
            Scaffold.GenerateScaffolding(sourceDir, destinationDir, new Dictionary<string, object>());
        }

        //Setting == CSCFG Settings
        private RoleSettings GetRoleCSCFG(string roleName)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ServiceConfiguration));
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream s = assembly.GetManifestResourceStream(ResourceName.WebRoleSettingsTemplate);
            RoleSettings roleSettings = ((ServiceConfiguration)xmlSerializer.Deserialize(s)).Role[0];
            roleSettings.name = roleName;
            s.Close();
            return roleSettings;
        }

        //Setting == CSCFG Settings
        private void GetWebRoleCSDEF(string webRoleName, ref int webRoleOccurrence, out WebRole webRole)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ServiceDefinition));
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(ResourceName.WebRoleTemplate);
            webRole = ((ServiceDefinition)xmlSerializer.Deserialize(stream)).WebRole[0];
            stream.Close();
            webRole.name = webRoleName;
            webRoleOccurrence++;
        }

        private void AddNewRole(ServiceConfiguration sc, RoleSettings newRole)
        {
            int count = (sc.Role == null) ? 0 : sc.Role.Length;
            RoleSettings[] roleSettings = new RoleSettings[count + 1];

            if (count > 0)
            {
                sc.Role.CopyTo(roleSettings, 0);
            }
            roleSettings[count] = newRole;
            sc.Role = roleSettings;
        }

        private void AddNewWebRole(ServiceDefinition sd, WebRole newWebRole)
        {
            int count = (sd.WebRole == null) ? 0 : sd.WebRole.Length;
            WebRole[] webRoles = new WebRole[count + 1];

            if (count > 0)
            {
                sd.WebRole.CopyTo(webRoles, 0);
            }
            webRoles[count] = newWebRole;
            sd.WebRole = webRoles;
        }

        private void GetRoleOccurrence(ServiceDefinition serviceDefinition, out int webRoleOccurrence, out int workerRoleOccurrence)
        {
            webRoleOccurrence = (serviceDefinition.WebRole == null) ? 0 : serviceDefinition.WebRole.Length;
            workerRoleOccurrence = (serviceDefinition.WorkerRole == null) ? 0 : serviceDefinition.WorkerRole.Length;
        }

    }
}
