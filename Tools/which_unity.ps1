Get-CimInstance Win32_Process |
Where-Object { $_.Name -eq 'Unity.exe' } |
Select-Object ProcessId, CommandLine |
Format-List
