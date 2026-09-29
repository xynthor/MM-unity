Set WshShell = WScript.CreateObject("WScript.Shell")
ok = WshShell.AppActivate(29804)
WScript.Echo "APP=" & CStr(ok)
WScript.Sleep 600
WshShell.SendKeys "^r"
WScript.Sleep 5000
WshShell.SendKeys "{F9}"
