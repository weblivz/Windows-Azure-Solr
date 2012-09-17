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

msbuild SolrInstWR\ReplSolr.sln
if errorlevel 1 goto error

mkdir build\SolrInstWR\Debug
mkdir build\SolrInstWR\Debug\SolrPkg

XCOPY /Y/I/Q/S SolrInstWR\ConfigFiles build\SolrInstWR\Debug
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrAdminWebRole build\SolrInstWR\Debug\SolrPkg\SolrAdminWebRole /Exclude:ExcludeList.txt 
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\bin\Debug build\SolrInstWR\Debug\SolrPkg\SolrMasterHostWorkerRole
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\bin\Debug build\SolrInstWR\Debug\SolrPkg\SolrSlaveHostWorkerRole
COPY /Y SolrInstWR\ReplSolr\ReplSolr\ServiceConfiguration.Local.cscfg build\SolrInstWR\Debug\SolrPkg\ServiceConfiguration.Local.cscfg
COPY /Y SolrInstWR\ReplSolr\ReplSolr\ServiceDefinition.csdef build\SolrInstWR\Debug\SolrPkg\ServiceDefinition.csdef

@echo Building Release
set Configuration=Release

msbuild SolrInstWR\ReplSolr.sln
if errorlevel 1 goto error

mkdir build\SolrInstWR\Release
mkdir build\SolrInstWR\Release\SolrPkg

XCOPY /Y/I/Q/S SolrInstWR\ConfigFiles build\SolrInstWR\Release
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrAdminWebRole build\SolrInstWR\Release\SolrPkg\SolrAdminWebRole /Exclude:ExcludeList.txt 
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\bin\Release build\SolrInstWR\Release\SolrPkg\SolrMasterHostWorkerRole
XCOPY /Y/I/Q/S SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\bin\Release build\SolrInstWR\Release\SolrPkg\SolrSlaveHostWorkerRole
COPY /Y SolrInstWR\ReplSolr\ReplSolr\ServiceConfiguration.Local.cscfg build\SolrInstWR\Release\SolrPkg\ServiceConfiguration.Local.cscfg
COPY /Y SolrInstWR\ReplSolr\ReplSolr\ServiceDefinition.csdef build\SolrInstWR\Release\SolrPkg\ServiceDefinition.csdef

@echo Preparing build package for release
XCOPY /Y/I/Q/S build\SolrInstWR\Release build\SolrInstWR-build

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