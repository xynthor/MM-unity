Set WshShell = WScript.CreateObject("WScript.Shell")
ok = WshShell.AppActivate(29804)
WScript.Echo "APP=" & CStr(ok)
WScript.Sleep 700
WshShell.SendKeys "^+j"
WScript.Sleep 500
