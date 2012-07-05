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
using System.IO;
using System.Net;
using System.Threading;
//using AzureDeploymentCmdlets.Model;
using SolrDeployCmdlets.Utilities;
using System.Management.Automation;


namespace SolrDeployCmdlets.Solr.Cmdlet
{
    public abstract class DownloadAndInstallBase : PSCmdlet
    {
        private AutoResetEvent threadBlocker;
        private int downloadProgress;

        protected String DownloadSetup(String downloadLocation, String extn)
        {
            String tempLocationToSave = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "." + extn);
            try
            {
                using (WebClient setupDownloader = new WebClient())
                {
                    setupDownloader.DownloadProgressChanged += new DownloadProgressChangedEventHandler(setupDownloader_DownloadProgressChanged);
                    setupDownloader.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(setupDownloader_DownloadFileCompleted);
                    setupDownloader.DownloadFileAsync(new Uri(downloadLocation), tempLocationToSave);

                    Console.Write("Downloading setup - ");
                    threadBlocker = new AutoResetEvent(false);
                    threadBlocker.WaitOne();
                }
            }
            finally
            {
                if (threadBlocker != null) { threadBlocker.Close(); }
            }

            return tempLocationToSave;
        }

        private void setupDownloader_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if ((e.ProgressPercentage % 10 == 0) && (e.ProgressPercentage > downloadProgress))
            {
                downloadProgress = e.ProgressPercentage;
                Console.Write(String.Concat(" ", e.ProgressPercentage + "%"));
            }
        }

        private void setupDownloader_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Console.WriteLine(String.Empty);
            threadBlocker.Set();
        }

        protected void Install(string setupLocation) 
        {
            String command = String.Format("Start-Process -File \"{0}\" -Wait", setupLocation);
            ExecuteCommands.ExecuteCommand(command);
        }

        protected void Copy(string srcLocation, string destination) 
        {
            String copyCommand = String.Format("COPY-ITEM \"{0}\" \"{1}\" -recurse -force", srcLocation, destination);
            ExecuteCommands.ExecuteCommand(copyCommand);
        }
    }
}
