@echo off
setlocal
if not defined ALLUSERSPROFILE set "ALLUSERSPROFILE=C:\ProgramData"
if not defined ProgramData set "ProgramData=C:\ProgramData"
if not defined PUBLIC set "PUBLIC=C:\Users\Public"
if not defined APPDATA set "APPDATA=%USERPROFILE%\AppData\Roaming"
if not defined LOCALAPPDATA set "LOCALAPPDATA=%USERPROFILE%\AppData\Local"

set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe"
if not exist "%UNITY_EXE%" (
  echo Unity 6000.5.6f1 is not installed at the expected location.
  pause
  exit /b 1
)

if not exist "%~dp0Logs" mkdir "%~dp0Logs"

echo Building MM Unity public Windows player...
"%UNITY_EXE%" -batchmode -quit -projectPath "%~dp0" -acceptSoftwareTermsForThisRunOnly -executeMethod MMPublicBuild.BuildWindowsBatch -logFile "%~dp0Logs\PublicBuild.log"
if errorlevel 1 (
  echo Build failed. See Logs\PublicBuild.log
  exit /b 1
)

echo Build complete: Builds\Windows\MMUnity.exe
endlocal
