Set WshShell = WScript.CreateObject("WScript.Shell")
ok = WshShell.AppActivate(20332)
WScript.Sleep 500
WshShell.SendKeys "{F6}"
