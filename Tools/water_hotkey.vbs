Set WshShell = WScript.CreateObject("WScript.Shell")
ok = WshShell.AppActivate(29804)
WScript.Echo "APP=" & CStr(ok)
WScript.Sleep 800
WshShell.SendKeys "^+w"
