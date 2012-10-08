@echo off

setlocal

if "%1"=="/?" goto usage
if "%1"=="-?" goto usage

@echo Building SolrInstWR for external release
@echo Deleting previous build

if exist build rmdir /S /Q build
if exist SolrInstWR\ReplSolr\HelperLib\bin rmdir /S /Q SolrInstWR\ReplSolr\HelperLib\bin
if exist SolrInstWR\ReplSolr\SolrAdminWebRole\bin rmdir /S /Q SolrInstWR\ReplSolr\SolrAdminWebRole\bin
if exist SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\bin rmdir /S /Q SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\bin
if exist SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\bin rmdir /S /Q SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\bin

if exist SolrInstWR\ReplSolr\HelperLib\obj rmdir /S /Q SolrInstWR\ReplSolr\HelperLib\obj
if exist SolrInstWR\ReplSolr\SolrAdminWebRole\obj rmdir /S /Q SolrInstWR\ReplSolr\SolrAdminWebRole\obj
if exist SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\obj rmdir /S /Q SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\obj
if exist SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\obj rmdir /S /Q SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\obj

@echo Building Debug
set Configuration=Debug

msbuild SolrInstWR\ReplSolr\ReplSolr.sln /p:Platform="Any CPU" /p:PreBuildEvent=;PostBuildEvent=
if errorlevel 1 goto error

mkdir build\SolrInstWR\Debug
mkdir build\SolrInstWR\Debug\SolrPkg

XCOPY /Y/I/Q/S SolrInstWR\ConfigFiles build\SolrInstWR\Debug
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrAdminWebRole build\SolrInstWR\Debug\SolrPkg\SolrAdminWebRole /Exclude:ExcludeList.txt 
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\bin\Debug build\SolrInstWR\Debug\SolrPkg\SolrMasterHostWorkerRole
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\bin\Debug build\SolrInstWR\Debug\SolrPkg\SolrSlaveHostWorkerRole
COPY /Y SolrInstWR\ReplSolr\ReplSolr\ServiceConfiguration.Local.cscfg build\SolrInstWR\Debug\SolrPkg\ServiceConfiguration.Local.cscfg
COPY /Y SolrInstWR\ReplSolr\ReplSolr\ServiceDefinition.csdef build\SolrInstWR\Debug\SolrPkg\ServiceDefinition.csdef

rem copy the solr importer exe and xml files
echo copying solr importer files
COPY /Y SolrInstWR\ReplSolr\SolrImporter\bin\Debug\SolrImporter.exe build\SolrInstWR\Debug\SolrPkg\SolrMasterHostWorkerRole\SolrImporter.exe
COPY /Y SolrInstWR\ReplSolr\SolrImporter\bin\Debug\HelperLib.dll build\SolrInstWR\Debug\SolrPkg\SolrMasterHostWorkerRole\HelperLib.dll
COPY /Y SolrInstWR\ReplSolr\SolrImporter\bin\Debug\uris.xml build\SolrInstWR\Debug\SolrPkg\SolrMasterHostWorkerRole\uris.xml

rem copy the solr files if passed (if you pass a preconfigured Solr directory)
echo copy jre

if not "%2"=="" XCOPY /Y/I/Q/S %2 build\SolrInstWR\Debug\SolrPkg\SolrMasterHostWorkerRole
if "%2"=="" XCOPY /Y/I/Q/S "C:\Program Files (x86)\Java" build\SolrInstWR\Debug\SolrPkg\SolrMasterHostWorkerRole
if not "%2"=="" XCOPY /Y/I/Q/S %2 build\SolrInstWR\Debug\SolrPkg\SolrSlaveHostWorkerRole
if "%2"=="" XCOPY /Y/I/Q/S "C:\Program Files (x86)\Java" build\SolrInstWR\Debug\SolrPkg\SolrSlaveHostWorkerRole

rem copy the jre is specified (needed when running locally but NOT when deploying to cloud)
echo copy solr
if not "%4"=="" XCOPY /Y/I/Q/S %4 build\SolrInstWR\Debug\SolrPkg\SolrMasterHostWorkerRole\Solr
if "%4"=="" XCOPY /Y/I/Q/S .\Solr build\SolrInstWR\Debug\SolrPkg\SolrMasterHostWorkerRole\Solr
if not "%4"=="" XCOPY /Y/I/Q/S %4 build\SolrInstWR\Debug\SolrPkg\SolrSlaveHostWorkerRole\Solr
if "%4"=="" XCOPY /Y/I/Q/S .\Solr build\SolrInstWR\Debug\SolrPkg\SolrSlaveHostWorkerRole\Solr

@echo Building Release
set Configuration=Release

msbuild SolrInstWR\ReplSolr\ReplSolr.sln /p:Platform="Any CPU" /p:PreBuildEvent=;PostBuildEvent=
if errorlevel 1 goto error

mkdir build\SolrInstWR\Release
mkdir build\SolrInstWR\Release\SolrPkg

XCOPY /Y/I/Q/S SolrInstWR\ConfigFiles build\SolrInstWR\Release
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrAdminWebRole build\SolrInstWR\Release\SolrPkg\SolrAdminWebRole /Exclude:ExcludeList.txt 
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\bin\Release build\SolrInstWR\Release\SolrPkg\SolrMasterHostWorkerRole
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\bin\Release build\SolrInstWR\Release\SolrPkg\SolrSlaveHostWorkerRole
COPY /Y SolrInstWR\ReplSolr\ReplSolr\ServiceConfiguration.Local.cscfg build\SolrInstWR\Release\SolrPkg\ServiceConfiguration.Local.cscfg
COPY /Y SolrInstWR\ReplSolr\ReplSolr\ServiceDefinition.csdef build\SolrInstWR\Release\SolrPkg\ServiceDefinition.csdef

rem copy the solr importer exe and xml files
echo copying solr importer files
COPY /Y SolrInstWR\ReplSolr\SolrImporter\bin\Release\SolrImporter.exe build\SolrInstWR\Release\SolrPkg\SolrMasterHostWorkerRole\SolrImporter.exe
COPY /Y SolrInstWR\ReplSolr\SolrImporter\bin\Release\HelperLib.dll build\SolrInstWR\Release\SolrPkg\SolrMasterHostWorkerRole\HelperLib.dll
COPY /Y SolrInstWR\ReplSolr\SolrImporter\bin\Release\uris.xml build\SolrInstWR\Release\SolrPkg\SolrMasterHostWorkerRole\uris.xml

rem copy the solr files if passed (if you pass a preconfigured Solr directory)
echo copy jre
if not "%2"=="" XCOPY /Y/I/Q/S %2 build\SolrInstWR\Release\SolrPkg\SolrMasterHostWorkerRole
if "%2"=="" XCOPY /Y/I/Q/S "C:\Program Files (x86)\Java" build\SolrInstWR\Release\SolrPkg\SolrMasterHostWorkerRole
if not "%2"=="" XCOPY /Y/I/Q/S %2 build\SolrInstWR\Release\SolrPkg\SolrSlaveHostWorkerRole
if "%2"=="" XCOPY /Y/I/Q/S "C:\Program Files (x86)\Java" build\SolrInstWR\Release\SolrPkg\SolrSlaveHostWorkerRole

rem copy the jre is specified (needed when running locally but NOT when deploying to cloud)
echo copy solr
if not "%4"=="" XCOPY /Y/I/Q/S %4 build\SolrInstWR\Release\SolrPkg\SolrMasterHostWorkerRole\Solr
if "%4"=="" XCOPY /Y/I/Q/S .\Solr build\SolrInstWR\Release\SolrPkg\SolrMasterHostWorkerRole\Solr
if not "%4"=="" XCOPY /Y/I/Q/S %4 build\SolrInstWR\Release\SolrPkg\SolrSlaveHostWorkerRole\Solr
if "%4"=="" XCOPY /Y/I/Q/S .\Solr build\SolrInstWR\Release\SolrPkg\SolrSlaveHostWorkerRole\Solr

@echo Preparing build package for release
XCOPY /Y/I/Q/S build\SolrInstWR\Release build\SolrInstWR-build

rem now copy the release build to the deploy folder
@echo Preparing build package for deploy
if exist deploy rmdir /S /Q deploy
XCOPY /Y/I/Q/S build\SolrInstWR-build deploy
XCOPY /Y/I/Q/S .\deployfiles deploy

goto noerror

:error
@echo !!! Build Error !!!
goto end

:noerror
exit /b 0

:usage
@echo.
@echo Builds SolrInstWR for external release
@echo Usage: build -jre pathtojre - solr pathtosolr
goto end

:end
endlocal