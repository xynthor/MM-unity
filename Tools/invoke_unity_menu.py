import ctypes,time
from ctypes import wintypes
u=ctypes.windll.user32
wins=[]
@ctypes.WINFUNCTYPE(ctypes.c_bool,wintypes.HWND,wintypes.LPARAM)
def cb(h,l):
    n=u.GetWindowTextLengthW(h)
    if n:
        b=ctypes.create_unicode_buffer(n+1);u.GetWindowTextW(h,b,n+1)
        if 'MMUnityPort' in b.value and 'Unity' in b.value: wins.append((h,b.value))
    return True
u.EnumWindows(cb,0)
print(wins)
if not wins: raise SystemExit('Unity window not found')
h=wins[0][0];u.ShowWindow(h,9);u.SetForegroundWindow(h);time.sleep(1)
def key(vk): u.keybd_event(vk,0,0,0);u.keybd_event(vk,0,2,0)
u.keybd_event(0x12,0,0,0);key(ord('M'));u.keybd_event(0x12,0,2,0);time.sleep(1)
# First MMUnity menu item is Apply Exact Source Grid To All Regions.
key(0x28);key(0x0D)
print('posted MMUnity first menu item')
