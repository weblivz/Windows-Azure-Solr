@echo off

setlocal

if "%1"=="/?" goto usage
if "%1"=="-?" goto usage

@echo Building SolrInstWR for external release
@echo Deleting previous build

if exist build rmdir /S /Q build
if exist Inst4WA\bin rmdir /S /Q Inst4WA\bin 
if exist Inst4WA\DeployCmdlets4WA\bin rmdir /S /Q  Inst4WA\DeployCmdlets4WA\bin 

if exist SolrDeployCmdletsSetup\bin  rmdir /S /Q SolrDeployCmdletsSetup\bin 
if exist SolrInstWR\ReplSolr\HelperLib\bin rmdir /S /Q SolrInstWR\ReplSolr\HelperLib\bin
if exist SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\bin rmdir /S /Q SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\bin
if exist SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\bin rmdir /S /Q SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\bin
if exist SolrInstWR\ReplSolr\SolrAdminWebRole\bin rmdir /S /Q SolrInstWR\ReplSolr\SolrAdminWebRole\bin
if exist SolrDeployCmdlets\bin  rmdir /S /Q SolrDeployCmdlets\bin 

if exist Inst4WA\obj rmdir /S /Q Inst4WA\obj 
if exist Inst4WA\DeployCmdlets4WA\obj rmdir /S /Q  Inst4WA\DeployCmdlets4WA\obj 

if exist SolrDeployCmdletsSetup\obj  rmdir /S /Q SolrDeployCmdletsSetup\obj 
if exist SolrInstWR\ReplSolr\HelperLib\obj rmdir /S /Q SolrInstWR\ReplSolr\HelperLib\obj
if exist SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\obj rmdir /S /Q SolrInstWR\ReplSolr\SolrMasterHostWorkerRole\obj
if exist SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\obj rmdir /S /Q SolrInstWR\ReplSolr\SolrSlaveHostWorkerRole\obj
if exist SolrInstWR\ReplSolr\SolrAdminWebRole\obj rmdir /S /Q SolrInstWR\ReplSolr\SolrAdminWebRole\obj
if exist SolrDeployCmdlets\obj  rmdir /S /Q SolrDeployCmdlets\obj 

@echo Building Debug
set Configuration=Debug

msbuild Inst4WA\Inst4WA.sln
if errorlevel 1 goto error

msbuild SolrInstWR\SolrInstWR.sln
if errorlevel 1 goto error

mkdir build\SolrInstWR\Debug

COPY /Y SolrInstWR\SolrDeployCmdletsSetup\bin\Debug build\SolrInstWR\Debug
COPY /Y Inst4WA\bin\Debug build\SolrInstWR\Debug

@echo Building Release
set Configuration=Release

msbuild Inst4WA\Inst4WA.sln
if errorlevel 1 goto error

msbuild SolrInstWR\SolrInstWR.sln
if errorlevel 1 goto error

mkdir build\SolrInstWR\Release

COPY /Y SolrInstWR\SolrDeployCmdletsSetup\bin\Release build\SolrInstWR\Release
COPY /Y Inst4WA\bin\Release build\SolrInstWR\Release

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