Set WshShell = WScript.CreateObject("WScript.Shell")
ok = WshShell.AppActivate(20332)
WScript.Echo "APP=" & CStr(ok)
WScript.Sleep 500
WshShell.SendKeys "^r"
WScript.Sleep 500
