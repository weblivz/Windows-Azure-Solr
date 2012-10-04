These files are used in the deployment process. The folder structure is :

deploy
	DeployCmdlets4WA.dll
	Inst4WA.exe
	SolrInstEmul[WithSolr].xml
	SolrInstWR[WithSolr].xml
	SolrPkg
		...

SolrPkg is the output from the msbuild command on the project output - the release build is at SolrInstWR-build.
You then run the Inst4WA.exe command.