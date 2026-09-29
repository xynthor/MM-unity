Set WshShell = WScript.CreateObject("WScript.Shell")
ok = WshShell.AppActivate(29804)
WScript.Echo "APP=" & CStr(ok)
WScript.Sleep 700
WshShell.SendKeys "^r"
WScript.Sleep 3500
WshShell.SendKeys "^+w"
WScript.Sleep 500
