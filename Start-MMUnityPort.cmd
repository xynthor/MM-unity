@echo off
setlocal
rem Desktop Commander shells can omit Windows common-profile variables.
rem Unity Hub and the Package Manager need these inherited by Unity.exe.
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
echo Opening MMUnityPort with complete Windows environment...
start "" "%UNITY_EXE%" -projectPath "%~dp0" -acceptSoftwareTermsForThisRunOnly
endlocal
