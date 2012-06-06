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
using AzureDeploymentCmdlets.Model;
using AzureDeploymentCmdlets.Scaffolding;
using SolrDeployCmdlets.ServiceDefinitionSchema;
using System.IO;
using System.Xml.Serialization;
using SolrDeployCmdlets.ServiceConfigurationSchema;
using System.Reflection;
using SolrDeployCmdlets.Properties;
using SolrDeployCmdlets.Utilities;
using Microsoft.Win32;

namespace SolrDeployCmdlets.Solr.Cmdlet
{
    [Cmdlet(VerbsCommon.Add, "AzureSolrSlaveWorkerRole")]
    public class AddAzureSolrSlaveWorkerRole : AddRole
    {
        private int numOfInstances;
        private string azureSolrInstallFolder;

        [Parameter(Mandatory = false, HelpMessage = "Set the VMSize for Solr Slave Worker role.")]
        public RoleSize? Size { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Location of JRE on the user machine.")]
        public String JREInstallFolder { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                base.ProcessRecord();
                string result = this.addSolrSlaveWorkerRole();
                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, String.Empty, ErrorCategory.CloseError, null));
            }
        }

        private string addSolrSlaveWorkerRole()
        {
            azureSolrInstallFolder = General.Instance.AzureSolrInstallFolder();
            string serviceRootPath = base.GetServiceRootPath();

            string localSvcConfigFileName = General.Instance.LocalSvcConfigFilePath(serviceRootPath);
            string cloudSvcConfigFileName = General.Instance.CloudSvcConfigFilePath(serviceRootPath);
            string serviceDefinitionFileName = General.Instance.SvcDefinitionFilePath(serviceRootPath);

            string roleName;
            int workerRoleOccurrence;
            ServiceConfiguration localSvcConfig;
            ServiceConfiguration cloudSvcConfig;
            ServiceDefinition svcDef;

            RoleSettings roleSettings;
            WorkerRole workerRole;

            localSvcConfig = General.Instance.ParseFile<ServiceConfiguration>(localSvcConfigFileName);
            cloudSvcConfig = General.Instance.ParseFile<ServiceConfiguration>(cloudSvcConfigFileName);
            svcDef = General.Instance.ParseFile<ServiceDefinition>(serviceDefinitionFileName);

            numOfInstances = Instances;
            workerRoleOccurrence = GetRoleOccurrence(svcDef);

            bool updated = false;

            // Set role name
            roleName = this.Name;
            if (string.IsNullOrEmpty(this.Name))
            {
                // Use default solr slave role name
                roleName = Resources.SlaveWorkerRoleName;
            }

            string message = string.Format(Resources.AddAzureSolrSlaveWorkerRoleSuccessMessage,
               (this.Name == null ? Resources.SlaveWorkerRoleName : this.Name), serviceRootPath, this.Instances);

            if (workerRoleOccurrence > 0)
            {
                foreach (RoleSettings role in localSvcConfig.Role)
                {
                    if (role.name == roleName)
                    {
                        role.Instances.count = numOfInstances;
                        updated = true;
                        break;
                    }
                }
                foreach (RoleSettings role in cloudSvcConfig.Role)
                {
                    if (role.name == roleName)
                    {
                        role.Instances.count = numOfInstances;
                        updated = true;
                    }
                }
            }

            if (!updated)
            {
                // Get default RoleSettings template
                roleSettings = GetRoleCSCFGTemplate(roleName);

                // Set instance count
                roleSettings.Instances.count = numOfInstances == 0 ? 1 : numOfInstances;

                // Add role to local and cloud *.cscfg
                AddNewRole(localSvcConfig, roleSettings);
                AddNewRole(cloudSvcConfig, roleSettings);

                // Get default WorkerRole template
                GetWorkerRoleCSDEFTemplate(roleName, ref workerRoleOccurrence, out workerRole);
                workerRole.vmsize = Size == null ? RoleSize.Small : Size.Value;

                // Add WorkerRole to *.csdef file
                AddNewWorkerRole(svcDef, workerRole);

                // Add WorkerRole scaffolding
                CreateScaffolding(roleName, false);

                //Copy JRE Files to role folder.
                CopyJRE(roleName);
            }
            else
            {
                message = string.Format(Resources.UpdateAzureSolrMasterWorkerRoleSuccessMessage,
                    (this.Name == null ? Resources.SlaveWorkerRoleName : this.Name), this.Instances, serviceRootPath);
            }

            // Serialize local.cscfg
            General.SerializeXmlFile<ServiceConfiguration>(localSvcConfig, localSvcConfigFileName);
            General.SerializeXmlFile<ServiceConfiguration>(cloudSvcConfig, cloudSvcConfigFileName);
            General.SerializeXmlFile<ServiceDefinition>(svcDef, serviceDefinitionFileName);

            return message;
        }

        private void CreateScaffolding(string roleFolderName, bool isWebRole)
        {
            string sourceDir = Path.Combine(azureSolrInstallFolder, Resources.SolrSlaveWorkerRoleScaffoldFolder);
            string destinationDir = Path.Combine(base.GetServiceRootPath(), roleFolderName);
            Scaffold.GenerateScaffolding(sourceDir, destinationDir, new Dictionary<string, object>());
        }

        private int GetRoleOccurrence(ServiceDefinition serviceDefinition)
        {
            int slaveWorkerRoleOccurrence = 0;
            if (serviceDefinition.WorkerRole != null)
            {
                for (int i = 0; i < serviceDefinition.WorkerRole.Length; i++)
                {
                    WorkerRole workerRoleAtIndex = serviceDefinition.WorkerRole[i];
                    if (workerRoleAtIndex.Endpoints != null &&
                        workerRoleAtIndex.Endpoints.InputEndpoint != null &&
                        workerRoleAtIndex.Endpoints.InputEndpoint[0].name == Resources.SlaveWorkerRoleInputEndpointName)
                    {
                        slaveWorkerRoleOccurrence++;
                        break;
                    }
                }
            }
            return slaveWorkerRoleOccurrence;
        }

        private RoleSettings GetRoleCSCFGTemplate(string roleName)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ServiceConfiguration));
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream s = assembly.GetManifestResourceStream(ResourceName.slaveWorkerRoleCSCFGTemplate);
            RoleSettings roleSettings = ((ServiceConfiguration)xmlSerializer.Deserialize(s)).Role[0];
            roleSettings.name = roleName;
            s.Close();

            return roleSettings;
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

        private void GetWorkerRoleCSDEFTemplate(string workerRoleName, ref int workerRoleOccurrence, out WorkerRole workerRole)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ServiceDefinition));
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(ResourceName.slaveWorkerRoleCSDEFTemplate);
            workerRole = ((ServiceDefinition)xmlSerializer.Deserialize(stream)).WorkerRole[0];
            stream.Close();
            workerRole.name = workerRoleName;
            workerRoleOccurrence++;
        }

        private void AddNewWorkerRole(ServiceDefinition sd, WorkerRole newWorkerRole)
        {
            int count = (sd.WorkerRole == null) ? 0 : sd.WorkerRole.Length;
            WorkerRole[] workerRoles = new WorkerRole[count + 1];

            if (count > 0)
            {
                sd.WorkerRole.CopyTo(workerRoles, 0);
            }
            workerRoles[count] = newWorkerRole;
            sd.WorkerRole = workerRoles;
        }

        private void CopyJRE(string roleFolderName)
        {
            WriteObject("Copying JRE files to master role location.");
            String destinationDir = Path.Combine(base.GetServiceRootPath(), roleFolderName + @"\jre6");
            String copyCommand = String.Format("COPY-ITEM \"{0}\" \"{1}\" -recurse -force", JREInstallFolder, destinationDir);
            ExecuteCommands.ExecuteCommand(copyCommand);
        }
    }
}
