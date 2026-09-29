Set WshShell = WScript.CreateObject("WScript.Shell")
WshShell.AppActivate 20332
WScript.Sleep 500
WshShell.SendKeys "^r"
WScript.Sleep 5000
WshShell.SendKeys "%m"
