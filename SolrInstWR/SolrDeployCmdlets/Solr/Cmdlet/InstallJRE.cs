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
using System.IO;
using SolrDeployCmdlets.Properties;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Security.Permissions;
using SolrDeployCmdlets.Utilities;

namespace SolrDeployCmdlets.Solr.Cmdlet
{
    [Cmdlet("Install", "JRE")]
    public class InstallJRE : DownloadAndInstallBase
    {
        [Parameter(Mandatory = true)]
        public string JRESetupPath { get; set; }

        [Parameter(Mandatory = true)]
        public string JREInstallFolder { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                base.ProcessRecord();
                string result = this.installJRE();
                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, String.Empty, ErrorCategory.CloseError, null));
            }
        }

        private string installJRE()
        {
            String javaHomePath = GetJavaHomePath();
            WriteObject("JAVA_HOME: " + javaHomePath);

            if (String.IsNullOrEmpty(javaHomePath) == true)
            {
                String downloadLocation = base.DownloadSetup(JRESetupPath, "exe");
                Install(downloadLocation);
            }

            return "JRE installation completed.";
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        private string GetJavaHomePath()
        {
            PSDataCollection<PSObject> output = ExecuteCommands.ExecuteCommand("Get-ChildItem env:JAVA_HOME");
            if (output == null || output.Count == 0)
            {
                return String.Empty;
            }

            return output[0].Members["Value"].Value.ToString();
        }
    }
}
