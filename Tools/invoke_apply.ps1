Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$p=Get-Process Unity|Where-Object{$_.MainWindowTitle -like '*MMUnityPort*'}|Select-Object -First 1
$root=[System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
$c=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'MMUnity')
$m=$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$c)
if(-not $m){throw 'MMUnity menu not found'}
$ep=$m.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern);$ep.Expand();Start-Sleep -Milliseconds 500
$c2=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'Apply Exact Source Grid To All Regions')
$item=$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$c2)
if(-not $item){throw 'Apply item not found'}
$ip=$item.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern);$ip.Invoke()
'Invoked exact source grid'
