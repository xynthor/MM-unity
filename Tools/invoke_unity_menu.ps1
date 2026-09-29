param([string]$Target)
Add-Type @'
using System; using System.Runtime.InteropServices; using System.Text;
public class UMenu {
 [DllImport("user32.dll")] public static extern IntPtr GetMenu(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr GetSubMenu(IntPtr h,int p);
 [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr h);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetMenuString(IntPtr h,uint id,StringBuilder s,int max,uint f);
 [DllImport("user32.dll")] public static extern uint GetMenuItemID(IntPtr h,int p);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
}
'@
$p=Get-Process Unity | Sort-Object WorkingSet64 -Descending | Select-Object -First 1
$main=[UMenu]::GetMenu($p.MainWindowHandle); $mm=[UMenu]::GetSubMenu($main,6)
for($i=0;$i -lt [UMenu]::GetMenuItemCount($mm);$i++){
 $sb=New-Object Text.StringBuilder 256; [void][UMenu]::GetMenuString($mm,[uint32]$i,$sb,256,0x400)
 if($sb.ToString() -eq $Target){$id=[UMenu]::GetMenuItemID($mm,$i); [void][UMenu]::PostMessage($p.MainWindowHandle,0x111,[IntPtr]$id,[IntPtr]::Zero); Write-Output "POSTED $Target id=$id pid=$($p.Id)"; exit 0}
}
Write-Error "Menu not found: $Target"; exit 2