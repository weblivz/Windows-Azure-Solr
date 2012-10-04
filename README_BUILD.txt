1. The "SoltInstWR" folder is the source tree. 
The solution can be run directly in a local Azure instance. 
MSBuild tasks will copy the appropriate folders (just make sure you point to the correct jre in those tasks).

2. The "build" folder is the output from the build.cmd script. The builds the debug and release outputs.

3. The "deploy" folder is the output from the build.cmd script. It contains everything you need to push locally or to the cloud.

4. The "deployfiles" contains a couple of framework files needed for deployment.

5. The "Solr" folder contains a pre-configured solr sample instance (rather than downloading gfrom the web). You need to pass this is as an argument to the build.cmd if needed.

6. build.cmd builds the project and deployment. It takes the following:

build.cmd -jre <AbsolutePathToJREFolder> -solr <AbsolutePathToColrFolder>

   e.g.
   build.cmd -jre "C:\Program Files (x86)\Java" -solr "C:\dev\Solr4Azure\Solr"

If not specified the jre comes from "C:\Program Files (x86)\Java" and Solr comes from a "Solr" folder in the same folder as build.cmd

7. Once you have run build and configured the .Xml files you can copy the deploy folder and in a command prompt run one of the following commands:

To download solr :
Ints4WA.exe -XmlConfigPath "SolrInstWR.xml" - Subscription <name> -DomainName <name>
Inst4WA.exe -XmlConfigPath "SolrInstEmul.xml"

If you have a local copy of Solr you'll run this:

Inst4WA.exe -XmlConfigPath "SolrInstWRWithSolr.xml" - Subscription <name> -DomainName <name>
Inst4WA.exe -XmlConfigPath "SolrInstEmulWithSolr.xml"