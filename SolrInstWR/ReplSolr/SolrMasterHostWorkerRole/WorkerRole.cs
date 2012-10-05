#region Copyright Notice
/*
Copyright � Microsoft Open Technologies, Inc.
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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace SolrMasterHostWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private static CloudDrive _solrStorageDrive = null;
        private static String _logFileLocation;
        private static Process _solrProcess = null;
        private static string _port = null;
        private static string _mySolrUrl = null;
        private static decimal _solrVersion = 3.0m;
        private static bool _isSolrConfigured = false;

        public override void Run()
        {
            Log("SolrMasterHostWorkerRole Run() called", "Information");

            while (true)
            {
                Thread.Sleep(10000);
                Log("Working", "Information");

                if ((_solrProcess != null) && (_solrProcess.HasExited == true))
                {
                    Log("Solr Process Exited. Hence recycling master role.", "Information");
                    RoleEnvironment.RequestRecycle();
                    return;
                }
            }
        }

        public override bool OnStart()
        {
            Log("SolrMasterHostWorkerRole Start() called", "Information");

            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            RoleEnvironment.Changing += (sender, arg) =>
            {
                RoleEnvironment.RequestRecycle();
            };

            InitDiagnostics();
            StartSolr();

            return base.OnStart();
        }

        public override void OnStop()
        {
            Log("SolrMasterHostWorkerRole OnStop() called", "Information");

            if (_solrProcess != null)
            {
                try
                {
                    _solrProcess.Kill();
                    _solrProcess.WaitForExit(2000);
                }
                catch { }
            }

            if (_solrStorageDrive != null)
            {
                try
                {
                    _solrStorageDrive.Unmount();
                }
                catch { }
            }

            base.OnStop();
        }

        private void StartSolr()
        {
            try
            {
                // we can use the IsConfigured flag if we are including a pre-configured "Solr" directory at the root of the role.
                _isSolrConfigured = Boolean.Parse(RoleEnvironment.GetConfigurationSettingValue("SolrIsConfigured"));

                // we use an Azure drive to store the solr index and conf data
                String vhdPath = CreateSolrStorageVhd();

                InitializeLogFile(vhdPath);

                InitRoleInfo();

                // Create the necessary directories in the Azure drive.
                CreateSolrStoragerDirs(vhdPath);

                if (!_isSolrConfigured)
                {
                    //Set IP Endpoint and Port Address.
                    //ConfigureIPEndPointAndPortAddress();

                    // Copy solr files such as configuration and additional libraries etc.
                    CopySolrFiles(vhdPath);

                    Log("Done - Creating storage dirs and copying conf files", "Information");
                }

                string cmdLineFormat =
                    @"%RoleRoot%\approot\jre{0}\bin\java.exe -Djetty.home={1}SolrStorage\{3} -Dsolr.solr.home={1}SolrStorage\{3}\solr -Djetty.port={2} -Denable.master=true -DdefaultCoreName=masterCore -jar %RoleRoot%\approot\Solr\{3}\start.jar";

                string cmdLine = String.Format(cmdLineFormat, RoleEnvironment.GetConfigurationSettingValue("JavaVersion"), vhdPath, _port, RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"));
                Log("Solr start command line: " + cmdLine, "Information");

                _solrProcess = ExecuteShellCommand(cmdLine, false, Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"\"));
                _solrProcess.Exited += new EventHandler(_solrProcess_Exited);

                Log("Done - Starting Solr", "Information");
            }
            catch (Exception ex)
            {
                Log("Exception occured in StartSolr " + ex.Message, "Error");
            }
        }

        void _solrProcess_Exited(object sender, EventArgs e)
        {
            Log("Solr Exited", "Information");
            RoleEnvironment.RequestRecycle();
        }

        private String CreateSolrStorageVhd()
        {
            CloudStorageAccount storageAccount;
            LocalResource localCache;
            CloudBlobClient client;
            CloudBlobContainer drives;

            // get the version of solr we are using
            _solrVersion = Decimal.Parse(RoleEnvironment.GetConfigurationSettingValue("SolrVersion"));

            localCache = RoleEnvironment.GetLocalResource("AzureDriveCache");
            Log(String.Format("AzureDriveCache {0} {1} MB", localCache.RootPath, localCache.MaximumSizeInMegabytes - 50), "Information");
            CloudDrive.InitializeCache(localCache.RootPath.TrimEnd('\\'), localCache.MaximumSizeInMegabytes - 50);

            storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("DataConnectionString"));
            client = storageAccount.CreateCloudBlobClient();

            string roleId = RoleEnvironment.CurrentRoleInstance.Id;
            string containerAddress = ContainerNameFromRoleId(roleId);
            drives = client.GetContainerReference(containerAddress);

            try { drives.CreateIfNotExist(); }
            catch { };

            var vhdUrl = client.GetContainerReference(containerAddress).GetBlobReference("SolrStorage.vhd").Uri.ToString();
            Log(String.Format("SolrStorage.vhd {0}", vhdUrl), "Information");
            _solrStorageDrive = storageAccount.CreateCloudDrive(vhdUrl);

            int cloudDriveSizeInMB = int.Parse(RoleEnvironment.GetConfigurationSettingValue("CloudDriveSize"));
            try { _solrStorageDrive.Create(cloudDriveSizeInMB); }
            catch (CloudDriveException) { }

            Log(String.Format("CloudDriveSize {0} MB", cloudDriveSizeInMB), "Information");

            var dataPath = _solrStorageDrive.Mount(localCache.MaximumSizeInMegabytes - 50, DriveMountOptions.Force);
            Log(String.Format("Mounted as {0}", dataPath), "Information");

            return dataPath;
        }

        // follow container naming conventions to generate a unique container name
        private static string ContainerNameFromRoleId(string roleId)
        {
            return roleId.Replace('(', '-').Replace(").", "-").Replace('.', '-').Replace('_', '-').ToLower();
        }

        #region Copy Solr Files

        //###
        private void CreateSolrStoragerDirs(String vhdPath)
        {
            String solrStorageDir, solrConfDir, solrDataDir, solrLibDir;
            String solrStorage = Path.Combine(vhdPath, "SolrStorage");
            solrStorageDir = Path.Combine(vhdPath, "SolrStorage");

            if (Directory.Exists(solrStorageDir) == false)
            {
                Directory.CreateDirectory(solrStorageDir);
            }

            // if solr has been configured manually (i.e. you have downloaded & configured Solr for a deploy) then just copy everything
            if (_isSolrConfigured)
            {
                String sourceFilesDir = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr");
                ExecuteShellCommand(String.Format("XCOPY \"{0}\" \"{1}\"  /E /Y", sourceFilesDir, solrStorage), true);
                return;
            }

            // we will configure from a downloaded source
            string solrInstanceDir = solrStorageDir;
            // in solr 4 things by default are put into collection1
            if (_solrVersion >= 4.0m)
            {
                solrInstanceDir = Path.Combine(solrStorageDir, "collection1");

                if (Directory.Exists(solrInstanceDir) == false)
                {
                    Directory.CreateDirectory(solrInstanceDir);
                }
            }

            solrConfDir = Path.Combine(solrInstanceDir, "conf");
            solrDataDir = Path.Combine(solrInstanceDir, "data");
            solrLibDir = Path.Combine(solrInstanceDir, "lib");

            if (Directory.Exists(solrConfDir) == false)
            {
                Directory.CreateDirectory(solrConfDir);
            }
            if (Directory.Exists(solrDataDir) == false)
            {
                Directory.CreateDirectory(solrDataDir);
            }
            if (Directory.Exists(solrLibDir) == false)
            {
                Directory.CreateDirectory(solrLibDir);
            }
        }

        //###
        private void CopySolrFiles(String vhdPath)
        {
            // Copy solr conf files.
            if (_solrVersion < 4.0m)
            {
                IEnumerable<String> confFiles = Directory.EnumerateFiles(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\conf"));
                foreach (String sourceFile in confFiles)
                {
                    String confFileName = System.IO.Path.GetFileName(sourceFile);
                    File.Copy(sourceFile, Path.Combine(vhdPath, "SolrStorage", "conf", confFileName), true);
                }
            }
            else
            {
                IEnumerable<String> confFiles = Directory.EnumerateFiles(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\collection1\conf"));
                foreach (String sourceFile in confFiles)
                {
                    String confFileName = System.IO.Path.GetFileName(sourceFile);
                    File.Copy(sourceFile, Path.Combine(vhdPath, "SolrStorage", "collection1\\conf", confFileName), true);
                }
            }

            if (_solrVersion < 4.0m)
            {
                // Copy lang Directory.
                IEnumerable<String> langFiles = Directory.EnumerateFiles(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\conf\lang"));
                foreach (String sourceFile in langFiles)
                {
                    String confFileName = System.IO.Path.GetFileName(sourceFile);
                    File.Copy(sourceFile, Path.Combine(vhdPath, "SolrStorage", @"conf\lang", confFileName), true);
                }
            }
            else
            {
                // Copy lang Directory.
                IEnumerable<String> langFiles = Directory.EnumerateFiles(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\collection1\conf\lang"));
                foreach (String sourceFile in langFiles)
                {
                    String confFileName = System.IO.Path.GetFileName(sourceFile);
                    File.Copy(sourceFile, Path.Combine(vhdPath, "SolrStorage", "collection1\\conf\\lang", confFileName), true);
                }
            }

            // copy data import handler
            if (_solrVersion < 4.0m)
            {
                if (Directory.Exists(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\conf\dataimporthandler")))
                {
                    // Copy lang Directory.
                    IEnumerable<String> dhFiles = Directory.EnumerateFiles(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\conf\dataimporthandler"));
                    foreach (String sourceFile in dhFiles)
                    {
                        String confFileName = System.IO.Path.GetFileName(sourceFile);
                        File.Copy(sourceFile, Path.Combine(vhdPath, "SolrStorage", @"conf\\dataimporthandler", confFileName), true);
                    }
                }

                // copy the jdbc driver if we have it
                if (Directory.Exists(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\lib")))
                {
                    if (File.Exists(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\lib\sqljdbc4.jar")))
                    {
                        string sourceFile = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\lib\sqljdbc4.jar");
                        File.Copy(sourceFile, Path.Combine(vhdPath, "SolrStorage", "conf\\lib\\sqljdbc4.jar"), true);
                    }
                }
            }
            else
            {
                if (Directory.Exists(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\collection1\conf\lang")))
                {
                    // Copy lang Directory.
                    IEnumerable<String> dhFiles = Directory.EnumerateFiles(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\collection1\conf\lang"));
                    foreach (String sourceFile in dhFiles)
                    {
                        String confFileName = System.IO.Path.GetFileName(sourceFile);
                        File.Copy(sourceFile, Path.Combine(vhdPath, "SolrStorage", "collection1\\conf\\dataimporthandler", confFileName), true);
                    }
                }

                // copy the jdbc driver if we have it
                if (Directory.Exists(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\collection1\lib")))
                {
                    if (File.Exists(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\collection1\lib\sqljdbc4.jar")))
                    {
                        string sourceFile = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\collection1\lib\sqljdbc4.jar");
                        File.Copy(sourceFile, Path.Combine(vhdPath, "SolrStorage", "collection1\\conf\\lib\\sqljdbc4.jar"), true);
                    }
                }
            }

            // copy solr.xml file
            if (_solrVersion >= 4.0m)
            {
                File.Copy(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\", RoleEnvironment.GetConfigurationSettingValue("SolrInstanceName"), @"solr\solr.xml"), Path.Combine(vhdPath, "SolrStorage", "solr.xml"), true);
            }

            // Overwrite original versions of SOLR files.
            string modifiedSolrFileSrc = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\SolrFiles\");
            string modifiedSolrFileDestination = Path.Combine(vhdPath, "SolrStorage", "conf");
            if (_solrVersion < 4.0m) modifiedSolrFileDestination = Path.Combine(vhdPath, "SolrStorage", "collection1\\conf");
            File.Copy(Path.Combine(modifiedSolrFileSrc, "data-config.xml"), Path.Combine(modifiedSolrFileDestination, "data-config.xml"), true);
            File.Copy(Path.Combine(modifiedSolrFileSrc, "schema.xml"), Path.Combine(modifiedSolrFileDestination, "schema.xml"), true);
            File.Copy(Path.Combine(modifiedSolrFileSrc, "solrconfig.xml"), Path.Combine(modifiedSolrFileDestination, "solrconfig.xml"), true);
            File.Copy(Path.Combine(modifiedSolrFileSrc, "solr.xml"), Path.Combine(Path.Combine(vhdPath, "SolrStorage"), "solr.xml"), true);

            if (_solrVersion >= 4.0m)
            {
                CopyContextFiles(Path.Combine(vhdPath, "SolrStorage"));
                CopyWebAppsFiles(Path.Combine(vhdPath, "SolrStorage"));
                CopyWebAppFiles(Path.Combine(vhdPath, "SolrStorage"));
                CopyEtcFiles(Path.Combine(vhdPath, "SolrStorage"));
            }
            CopyLibFiles(Path.Combine(vhdPath, "SolrStorage"));
            CopyExtractionFiles(Path.Combine(vhdPath, "SolrStorage"));
        }

        //###
        private void CopyEtcFiles(string solrStorage)
        {
            String etcDir = Path.Combine(solrStorage, "etc");
            String sourceExtractionFilesDir = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\etc");
            ExecuteShellCommand(String.Format("XCOPY \"{0}\" \"{1}\"  /E /Y", sourceExtractionFilesDir, etcDir), true);
        }

        private void CopyWebAppFiles(string solrStorage)
        {
            String webappDir = Path.Combine(solrStorage, "solr-webapp");
            String sourceExtractionFilesDir = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\solr-webapp");
            ExecuteShellCommand(String.Format("XCOPY \"{0}\" \"{1}\"  /E /Y", sourceExtractionFilesDir, webappDir), true);
        }

        private void CopyWebAppsFiles(string solrStorage)
        {
            String webappsDir = Path.Combine(solrStorage, "webapps");
            String sourceExtractionFilesDir = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\webapps");
            ExecuteShellCommand(String.Format("XCOPY \"{0}\" \"{1}\"  /E /Y", sourceExtractionFilesDir, webappsDir), true);
        }

        private void CopyContextFiles(string solrStorage)
        {
            String contextsDir = Path.Combine(solrStorage, "contexts");
            String sourceExtractionFilesDir = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\contexts");
            ExecuteShellCommand(String.Format("XCOPY \"{0}\" \"{1}\"  /E /Y", sourceExtractionFilesDir, contextsDir), true);
        }

        private void CopyExtractionFiles(string solrStorage)
        {
            String libDir = Path.Combine(solrStorage, "lib");
            String sourceExtractionFilesDir = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\contrib\extraction\lib");
            ExecuteShellCommand(String.Format("XCOPY \"{0}\" \"{1}\"  /E /Y", sourceExtractionFilesDir, libDir), true);
        }

        //###
        private void CopyLibFiles(String solrStorage)
        {
            String libFileName, libFileLocation;
            IEnumerable<String> libFiles;

            libFiles = Directory.EnumerateFiles(Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Solr\dist"));
            libFileLocation = Path.Combine(solrStorage, "lib");
            foreach (String sourceFile in libFiles)
            {
                libFileName = System.IO.Path.GetFileName(sourceFile);
                File.Copy(sourceFile, Path.Combine(libFileLocation, libFileName), true);
            }
        }

        #endregion

        private void InitializeLogFile(string vhdPath)
        {
            String logFileName;
            String logFileDirectoryLocation;

            logFileDirectoryLocation = Path.Combine(vhdPath, "LogFiles");
            if (Directory.Exists(logFileDirectoryLocation) == false)
            {
                Directory.CreateDirectory(logFileDirectoryLocation);
            }

            logFileName = String.Format("Log_{0}.txt", DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss"));
            using (FileStream logFileStream = File.Create(Path.Combine(logFileDirectoryLocation, logFileName)))
            {
                _logFileLocation = Path.Combine(logFileDirectoryLocation, logFileName);
            }
        }

        // figure out and set port, master / slave, master Url etc.
        private void InitRoleInfo()
        {
            IPEndPoint endpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["SolrMasterEndpoint"].IPEndpoint;
            _port = endpoint.Port.ToString();
            _mySolrUrl = string.Format("http://{0}/solr/", endpoint);

            HelperLib.Util.AddRoleInfoEntry(RoleEnvironment.CurrentRoleInstance.Id, endpoint.Address.ToString(), endpoint.Port, true);

            Log("My SolrURL: " + _mySolrUrl, "Information");
        }

        private Process ExecuteShellCommand(String command, bool waitForExit, String workingDir = null)
        {
            Process processToExecuteCommand = new Process();

            processToExecuteCommand.StartInfo.FileName = "cmd.exe";
            if (workingDir != null)
            {
                processToExecuteCommand.StartInfo.WorkingDirectory = workingDir;
            }

            processToExecuteCommand.StartInfo.Arguments = @"/C " + command;
            processToExecuteCommand.StartInfo.RedirectStandardInput = true;
            processToExecuteCommand.StartInfo.RedirectStandardError = true;
            processToExecuteCommand.StartInfo.RedirectStandardOutput = true;
            processToExecuteCommand.StartInfo.UseShellExecute = false;
            processToExecuteCommand.StartInfo.CreateNoWindow = true;
            processToExecuteCommand.EnableRaisingEvents = false;
            processToExecuteCommand.Start();

            processToExecuteCommand.OutputDataReceived += new DataReceivedEventHandler(processToExecuteCommand_OutputDataReceived);
            processToExecuteCommand.ErrorDataReceived += new DataReceivedEventHandler(processToExecuteCommand_ErrorDataReceived);
            processToExecuteCommand.BeginOutputReadLine();
            processToExecuteCommand.BeginErrorReadLine();

            if (waitForExit == true)
            {
                processToExecuteCommand.WaitForExit();
                processToExecuteCommand.Close();
                processToExecuteCommand.Dispose();
                processToExecuteCommand = null;
            }

            return processToExecuteCommand;
        }

        private void processToExecuteCommand_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Log(e.Data, "Message");
        }

        private void processToExecuteCommand_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Log(e.Data, "Message");
        }

        private void InitDiagnostics()
        {
#if DEBUG
            // Get the default initial configuration for DiagnosticMonitor.
            DiagnosticMonitorConfiguration diagnosticConfiguration = DiagnosticMonitor.GetDefaultInitialConfiguration();

            // Filter the logs so that only error-level logs are transferred to persistent storage.
            diagnosticConfiguration.Logs.ScheduledTransferLogLevelFilter = LogLevel.Undefined;

            // Schedule a transfer period of 30 minutes.
            diagnosticConfiguration.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(2.0);

            // Specify a buffer quota of 1GB.
            diagnosticConfiguration.Logs.BufferQuotaInMB = 1024;

            // Start the DiagnosticMonitor using the diagnosticConfig and our connection string.
            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", diagnosticConfiguration);
#endif
        }

        private void Log(string message, string category)
        {
#if DEBUG
            message = RoleEnvironment.CurrentRoleInstance.Id + "=> " + message;

            try
            {
                if (String.IsNullOrWhiteSpace(_logFileLocation) == false)
                {
                    File.AppendAllText(_logFileLocation, String.Concat(message, Environment.NewLine));
                }
            }
            catch
            { }

            Trace.WriteLine(message, category);
#endif
        }
    }
}
