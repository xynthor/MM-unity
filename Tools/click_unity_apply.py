import ctypes,time
from ctypes import wintypes
u=ctypes.windll.user32; wins=[]
@ctypes.WINFUNCTYPE(ctypes.c_bool,wintypes.HWND,wintypes.LPARAM)
def cb(h,l):
 n=u.GetWindowTextLengthW(h)
 if n:
  b=ctypes.create_unicode_buffer(n+1);u.GetWindowTextW(h,b,n+1)
  if 'MMUnityPort' in b.value and 'Unity' in b.value:wins.append(h)
 return True
u.EnumWindows(cb,0);h=wins[0];u.ShowWindow(h,9);u.SetForegroundWindow(h);time.sleep(.5)
menu=u.GetMenu(h); idx=None
for i in range(u.GetMenuItemCount(menu)):
 n=u.GetMenuStringW(menu,i,None,0,0x400);b=ctypes.create_unicode_buffer(n+1);u.GetMenuStringW(menu,i,b,n+1,0x400)
 if b.value=='MMUnity':idx=i;break
r=wintypes.RECT();u.GetMenuItemRect(h,menu,idx,ctypes.byref(r));x=(r.left+r.right)//2;y=(r.top+r.bottom)//2
u.SetCursorPos(x,y);u.mouse_event(2,0,0,0,0);u.mouse_event(4,0,0,0,0);time.sleep(.5)
sub=u.GetSubMenu(menu,idx);r2=wintypes.RECT();u.GetMenuItemRect(h,sub,0,ctypes.byref(r2));x=(r2.left+r2.right)//2;y=(r2.top+r2.bottom)//2
u.SetCursorPos(x,y);u.mouse_event(2,0,0,0,0);u.mouse_event(4,0,0,0,0);print('clicked apply')
