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
using SolrDeployCmdlets.Properties;
using System.IO;
using SolrDeployCmdlets.Utilities;
using Microsoft.Win32;

namespace SolrDeployCmdlets.Solr.Cmdlet
{
    [Cmdlet(VerbsCommon.Get, "Solr")]
    public class DownloadSolr : DownloadAndInstallBase
    {
        [Parameter(Mandatory = true)]
        public string SolrSetupPath { get; set; }

        private string azureSolrInstallFolder;

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            azureSolrInstallFolder = General.Instance.AzureSolrInstallFolder();
            string status = GetSolr();
            WriteObject(status);
        }

        private string GetSolr()
        {
            //Download
            String downloadLocation = base.DownloadSetup(SolrSetupPath, "zip");

            //Unzip
            String unzippedLocation = Unzip(downloadLocation);
            String apacheSolrFolder = Path.Combine(unzippedLocation, "apache-solr-3.5.0\\*");

            //COPY Solr Binaries to Master worker role directory.
            String solrMasterRoleLocation = Path.Combine(azureSolrInstallFolder, Resources.SolrMasterWorkerRoleScaffoldFolder + "\\Solr");
            if (Directory.Exists(solrMasterRoleLocation) == false) { Directory.CreateDirectory(solrMasterRoleLocation); }
            WriteObject(String.Format("Copying SOLR files to {0}", solrMasterRoleLocation));
            Copy(apacheSolrFolder, solrMasterRoleLocation);

            //COPY Solr Binaries to Slave worker role directory.
            String solrSlaveRoleLocation = Path.Combine(azureSolrInstallFolder, Resources.SolrSlaveWorkerRoleScaffoldFolder + "\\Solr");
            if (Directory.Exists(solrSlaveRoleLocation) == false) { Directory.CreateDirectory(solrSlaveRoleLocation); }
            WriteObject(String.Format("Copying SOLR files to {0}", solrSlaveRoleLocation));
            Copy(apacheSolrFolder, solrSlaveRoleLocation);

            File.Delete(downloadLocation);
            Directory.Delete(unzippedLocation, true);

            return "Solr files copied successfully to worker roles.";
        }

        private string Unzip(string downloadLocation)
        {
            String unzipLocation = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(unzipLocation);
            String unzipCommand = String.Format(@"function Unzip([string]$locationOfZipFile, [string]$unzipLocation)
                                                {{
                                                    Write-Host $locationOfZipFile
                                                    Write-Host $unzipLocation
                                                    $shell_app = new-object -com shell.application
                                                    $zip_file = $shell_app.namespace($locationOfZipFile)
                                                    $destination = $shell_app.namespace($unzipLocation)
                                                    $destination.Copyhere($zip_file.items())
                                                }}
                                                Unzip ""{0}""  ""{1}""
                                                ", downloadLocation, unzipLocation);
            ExecuteCommands.ExecuteCommand(unzipCommand);
            return unzipLocation;
        }

    }
}
