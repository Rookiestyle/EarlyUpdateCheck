@echo off
cd %~dp0

if "%1" == "Debug" (
	xcopy /y /c /i ExternalPluginUpdates\*.xml "..\_KeePass_Debug\Plugins"
)
if "%1" == "ReleasePlgx" (
	xcopy /y /c /i ExternalPluginUpdates\*.xml "..\_KeePass_Release\Plugins"
	xcopy /y /c /i ExternalPluginUpdates\*.xml "..\_Releases"
)