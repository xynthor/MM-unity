Add-Type @'
using System;
using System.Runtime.InteropServices;
public class LockFinder {
[DllImport("rstrtmgr.dll", CharSet=CharSet.Unicode)] static extern int RmStartSession(out uint h, int f, string key);
[DllImport("rstrtmgr.dll", CharSet=CharSet.Unicode)] static extern int RmRegisterResources(uint h,uint n,string[] files,uint na,IntPtr apps,uint ns,IntPtr svcs);
[DllImport("rstrtmgr.dll")] static extern int RmGetList(uint h,out uint need,ref uint count,[In,Out] RM_PROCESS_INFO[] info,ref uint reboot);
[DllImport("rstrtmgr.dll")] static extern int RmEndSession(uint h);
[StructLayout(LayoutKind.Sequential)] struct RM_UNIQUE_PROCESS { public int pid; public System.Runtime.InteropServices.ComTypes.FILETIME start; }
[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] struct RM_PROCESS_INFO { public RM_UNIQUE_PROCESS Process; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=256)] public string AppName; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] public string ServiceShortName; public uint AppType, AppStatus, TSSessionId; [MarshalAs(UnmanagedType.Bool)] public bool Restartable; }
public static int[] Find(string file) {
uint h; string k=Guid.NewGuid().ToString(); if(RmStartSession(out h,0,k)!=0) return new int[0];
RmRegisterResources(h,1,new[]{file},0,IntPtr.Zero,0,IntPtr.Zero); uint need=0,count=0,reboot=0; int r=RmGetList(h,out need,ref count,null,ref reboot);
if(r!=234 || need==0){RmEndSession(h); return new int[0];}
var arr=new RM_PROCESS_INFO[need]; count=need; r=RmGetList(h,out need,ref count,arr,ref reboot); var ids=new int[count];
for(int i=0;i<count;i++) ids[i]=arr[i].Process.pid; RmEndSession(h); return ids;
}
}
'@
$path='C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorld.cs'
[LockFinder]::Find($path) | ForEach-Object {
  try { Get-CimInstance Win32_Process -Filter "ProcessId=$_" | Select-Object ProcessId,Name,CommandLine }
  catch { $_ }
}