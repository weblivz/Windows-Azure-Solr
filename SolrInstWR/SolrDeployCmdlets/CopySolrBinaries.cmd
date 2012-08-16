::Batch file to copy the Solr Binaries from Src to Cmdlet folders

@ECHO OFF

setlocal

	set solutionDir=%2
	set targetDir=%1
	set configuration=%3
	
	::Copy Web Role folder

	set srcWebRoleDir=%solutionDir%ReplSolr\SolrAdminWebRole\
	set destinationWebRoleDir=%targetDir%Scaffolding\Solr\SolrAdminWebRole\
	
	call:CopyFiles %srcWebRoleDir% %destinationWebRoleDir% bin
	call:CopyFiles %srcWebRoleDir% %destinationWebRoleDir% content
	call:CopyFiles %srcWebRoleDir% %destinationWebRoleDir% Controllers
	call:CopyFiles %srcWebRoleDir% %destinationWebRoleDir% Crawler
	call:CopyFiles %srcWebRoleDir% %destinationWebRoleDir% Properties
	call:CopyFiles %srcWebRoleDir% %destinationWebRoleDir% Scripts
	call:CopyFiles %srcWebRoleDir% %destinationWebRoleDir% Views

	COPY /Y %srcWebRoleDir%Global.asax %destinationWebRoleDir%Global.asax
	COPY /Y %srcWebRoleDir%Global.asax.cs %destinationWebRoleDir%Global.asax.cs
	COPY /Y %srcWebRoleDir%packages.config %destinationWebRoleDir%packages.config
	COPY /Y %srcWebRoleDir%Web.config %destinationWebRoleDir%Web.config
	COPY /Y %srcWebRoleDir%Web.Debug.config  %destinationWebRoleDir%Web.Debug.config
	COPY /Y %srcWebRoleDir%Web.Release.config %destinationWebRoleDir%Web.Release.config

	::Copy Master Role Folder

	set srcMasterRoleDir=%solutionDir%ReplSolr\SolrMasterHostWorkerRole\bin\%configuration%\
	set destinationMasterRoleDir=%targetDir%Scaffolding\Solr\SolrMasterHostWorkerRole\

	call:CopyFiles %srcMasterRoleDir% %destinationMasterRoleDir% SolrFiles
	COPY /Y %srcMasterRoleDir%Microsoft.WindowsAzure.CloudDrive.dll %destinationMasterRoleDir%Microsoft.WindowsAzure.CloudDrive.dll
	COPY /Y %srcMasterRoleDir%Microsoft.WindowsAzure.Diagnostics.dll %destinationMasterRoleDir%Microsoft.WindowsAzure.Diagnostics.dll
	COPY /Y %srcMasterRoleDir%Microsoft.WindowsAzure.StorageClient.dll %destinationMasterRoleDir%Microsoft.WindowsAzure.StorageClient.dll
	COPY /Y %srcMasterRoleDir%HelperLib.dll  %destinationMasterRoleDir%HelperLib.dll
	COPY /Y %srcMasterRoleDir%SolrMasterHostWorkerRole.dll %destinationMasterRoleDir%SolrMasterHostWorkerRole.dll
	COPY /Y %srcMasterRoleDir%SolrMasterHostWorkerRole.dll.config %destinationMasterRoleDir%SolrMasterHostWorkerRole.dll.config
	COPY /Y %srcMasterRoleDir%Microsoft.WindowsAzure.CloudDrive.xml %destinationMasterRoleDir%Microsoft.WindowsAzure.CloudDrive.xml
	COPY /Y %srcMasterRoleDir%Microsoft.WindowsAzure.Diagnostics.xml %destinationMasterRoleDir%Microsoft.WindowsAzure.Diagnostics.xml
	COPY /Y %srcMasterRoleDir%Microsoft.WindowsAzure.StorageClient.xml %destinationMasterRoleDir%Microsoft.WindowsAzure.StorageClient.xml
	COPY /Y %srcMasterRoleDir%startup.cmd %destinationMasterRoleDir%startup.cmd

	::Copy Slave Role Folder

	set srcSlaveRoleDir=%solutionDir%ReplSolr\SolrSlaveHostWorkerRole\bin\%configuration%\
	set destinationSlaveRoleDir=%targetDir%Scaffolding\Solr\SolrSlaveHostWorkerRole\

	call:CopyFiles %srcSlaveRoleDir% %destinationSlaveRoleDir% SolrFiles
	COPY /Y %srcSlaveRoleDir%Microsoft.WindowsAzure.CloudDrive.dll %destinationSlaveRoleDir%Microsoft.WindowsAzure.CloudDrive.dll
	COPY /Y %srcSlaveRoleDir%Microsoft.WindowsAzure.Diagnostics.dll %destinationSlaveRoleDir%Microsoft.WindowsAzure.Diagnostics.dll
	COPY /Y %srcSlaveRoleDir%Microsoft.WindowsAzure.StorageClient.dll %destinationSlaveRoleDir%Microsoft.WindowsAzure.StorageClient.dll
	COPY /Y %srcSlaveRoleDir%HelperLib.dll  %destinationSlaveRoleDir%HelperLib.dll
	COPY /Y %srcSlaveRoleDir%SolrSlaveHostWorkerRole.dll %destinationSlaveRoleDir%SolrSlaveHostWorkerRole.dll
	COPY /Y %srcSlaveRoleDir%SolrSlaveHostWorkerRole.dll.config %destinationSlaveRoleDir%SolrSlaveHostWorkerRole.dll.config
	COPY /Y %srcSlaveRoleDir%Microsoft.WindowsAzure.CloudDrive.xml %destinationSlaveRoleDir%Microsoft.WindowsAzure.CloudDrive.xml
	COPY /Y %srcSlaveRoleDir%Microsoft.WindowsAzure.Diagnostics.xml %destinationSlaveRoleDir%Microsoft.WindowsAzure.Diagnostics.xml
	COPY /Y %srcSlaveRoleDir%Microsoft.WindowsAzure.StorageClient.xml %destinationSlaveRoleDir%Microsoft.WindowsAzure.StorageClient.xml
	COPY /Y %srcSlaveRoleDir%startup.cmd %destinationSlaveRoleDir%startup.cmd

endlocal
GOTO:EOF
	
:CopyFiles
	setlocal
		
	set srcDir=""
	set srcDir=%1%3
			
	set destinationDir=""
	set destinationDir=%2%3
	mkdir %destinationDir%
	XCOPY /E/Y "%srcDir%" "%destinationDir%"
		
	endlocal
GOTO:EOF
	


