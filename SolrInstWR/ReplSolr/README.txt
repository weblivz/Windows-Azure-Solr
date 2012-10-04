1. There is a VS212 solution with the Solr files. The jre7 is included so we can debug locally as the jre needs deployed. The Solr files are added locally to the solutions as well so we can deploy the files we have rather than download the Solr zip.

2. You call the build command in the https://github.com/MSOpenTech/Windows-Azure-Solr project

3. Get the binaries from https://github.com/MSOpenTech/Windows-Azure-Solr/downloads and get the Inst4WA.exe and DeployCmdlets4WA.dll files

4. Put the SolrPkg from the msbuild output into a Deploy folder with the files from 3. Use the custom SolrInstEmulWithSolr.xml and SolrInstWRWithSolr.xml files if you already have the source in a pre-configured "Solr" directory.

5. Now run the Inst4WA.exe command.