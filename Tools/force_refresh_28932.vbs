Set WshShell = WScript.CreateObject("WScript.Shell")
ok=WshShell.AppActivate(28932)
WScript.Sleep 500
WshShell.SendKeys "^r"
