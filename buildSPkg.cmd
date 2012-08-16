@echo off

setlocal

if "%1"=="/?" goto usage
if "%1"=="-?" goto usage

@echo Building SolrInstWR for Inst4WA Package
@echo Deleting previous build

if exist build rmdir /S /Q SolrPkg

if exist SolrInstWR\ReplSolr\HelperLib\bin rmdir /S /Q SolrInstWR\ReplSolr\HelperLib\bin
if exist SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\bin rmdir /S /Q SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\bin
if exist SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\bin rmdir /S /Q SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\bin
if exist SolrInstWR\ReplSolr\SolrAdminWebRole\bin rmdir /S /Q SolrInstWR\ReplSolr\SolrAdminWebRole\bin

if exist SolrInstWR\ReplSolr\HelperLib\obj rmdir /S /Q SolrInstWR\ReplSolr\HelperLib\obj
if exist SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\obj rmdir /S /Q SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\obj
if exist SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\obj rmdir /S /Q SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\obj
if exist SolrInstWR\ReplSolr\SolrAdminWebRole\obj rmdir /S /Q SolrInstWR\ReplSolr\SolrAdminWebRole\obj

@echo Building Release
set Configuration=Release

msbuild SolrInstWR\SolrInstWR.sln
if errorlevel 1 goto error

mkdir SolrPkg

XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\bin\release SolrPkg\SolrMasterHostWorkerRole
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\bin\release SolrPkg\SolrSlaveHostWorkerRole
XCOPY /Y/I/Q/S  SolrInstWR\ReplSolr\SolrAdminWebRole SolrPkg\SolrAdminWebRole /Exclude:ExcludeList.txt 

COPY /Y SolrInstWR\ReplSolr\ReplSolr\ServiceConfiguration.Local.cscfg SolrPkg\ServiceConfiguration.Local.cscfg
COPY /Y SolrInstWR\ReplSolr\ReplSolr\ServiceDefinition.csdef SolrPkg\ServiceDefinition.csdef

goto noerror

:error
@echo !!! Build Error !!!
goto end

:noerror
exit /b 0

:usage
@echo.
@echo Builds SolrInstWR for external release
@echo Usage: build
goto end

:end
endlocal