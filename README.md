Solr 4 Upgrade
===

This includes :

- Now works with all version of Solr up to 4.
- Improved the Build so that is allows local Solr uploads.
- Upgraded to VS2012 - added events to allow running fully locally.
- Improved deloyment support.

Note: Read the README_BUILD.txt for Solr 4 upgrade data including build and deploy support.

You are responsible for reading licences and the info below and related https://github.com/MSOpenTech/Windows-Azure-Solr project is key.

Solr/Lucene on Azure
===
In this project we showcase how to configure and host Solr/Lucene in Windows Azure using multi-instance replication for index-serving and single-instance for index generation with a persistent index mounted in Azure storage. Typical scenarios we address with this sample are commercial and publisher sites that need to scale the traffic with increasing query volume and need to index maximum 16 TB of data and require couple of index updates per day.

As part of this install the following Microsoft or third party software will be installed on your local machine as following: 

- Solr/Lucene which is owned by The Apache Software Foundation., will be downloaded from http://www.apache.org/dyn/closer.cgi/lucene/solr/.The license agreement to Apache License, Version 2.0 may be included with the software.  You are responsible for and must separately locate, read and accept these license terms.

- Microsoft Windows Azure SDK for .Net and NodeJS which is owned by Microsoft , will be downloaded from http://www.microsoft.com/windowsazure/sdk/.

You are responsible for and must locate and read the license terms for each of the software above. 

## Prerequisites for installer

1. Windows machine: Windows 7 (64 bit) or Windows Server 2008 R2 (64 bit)

2. IIS including the web roles ASP.Net, Tracing, logging & CGI Services needs to be enabled.
    - http://learn.iis.net/page.aspx/29/installing-iis-7-and-above-on-windows-server-2008-or-windows-server-2008-r2/ 
  
3. .Net Framework 4.0 Full version
   
4. Download JRE for Windows 64-bit which is owned by Sun Microsystems, Inc., from http://www.java.com/en/download/manual.jsp  . You are responsible for reading and accepting the license terms.

5. Note if you start with a clean machine:  To download public setting file the enhanced security configuration of IE needs to be disabled. Go to Server Manager -> configure IE ESC -> disable for Administrators.

## Copy the binaries
1. Download and extract on your local computer the latest version SolrInstWRMMDDYYYY.zip (for example SolrInstWR06072012.zip) from https://github.com/MSOpenTech/Windows-Azure-Solr/downloads.

2. Please make sure that you unblock all the dll’s and config files using instructions at http://msdn.microsoft.com/en-us/library/ee890038(VS.100).aspx. 

3. Launch a command prompt (cmd.exe) as an administrator and cd to the local folder selected above.

## Run the installer:
    - Inst4WA.exe -XmlConfigPath <yourpath>/SolrInstWR.xml -DomainName <youruniquename>  -Subscription <yoursubscription>


Note: While the installer is running, it will open a browser to download your publish settings file. Save this file to either your downloads folder or the SolrInstaller folder. You must save the file in one of those two locations for the installer to see it and import the settings.
Do not write your publish settings over an existing file. The installer will be watching these two locations for a new file to be created.

## Administering Solr/Lucene

In the panel for your deployment you will find the DNS name `http://<Deployment_Endpoint>.cloudapp.net`

- Start in a browser `http://<Deployment_Endpoint>.cloudapp.net` to the typical tasks for Solr
- Crawl or Import data from the sample Windows Azure blob in the Crawl or Import tabs
- Validate that the index is replicated across SolrSlave  instances on the Home tab by checking the index size.
- After the index is replicated execute a search in the Search tab
