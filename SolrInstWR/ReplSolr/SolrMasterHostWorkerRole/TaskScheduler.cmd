@echo off
REM ********************************************************
REM Start the Task Scheduler Service
REM ********************************************************
net start "task scheduler"
REM ********************************************************
REM Create scheduled tasks.
REM ********************************************************
SCHTASKS /Create /SC MINUTE /MO 30 /TN MyTaskName /TR "%RoleRoot%\approot\SolrImporter\SolrImporter.exe" /RU SYSTEM /F /RL HIGHEST