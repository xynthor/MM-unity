import ctypes
from ctypes import wintypes
u=ctypes.windll.user32; wins=[]
@ctypes.WINFUNCTYPE(ctypes.c_bool,wintypes.HWND,wintypes.LPARAM)
def cb(h,l):
 n=u.GetWindowTextLengthW(h)
 if n:
  b=ctypes.create_unicode_buffer(n+1);u.GetWindowTextW(h,b,n+1)
  if 'MMUnityPort' in b.value and 'Unity' in b.value:wins.append((h,b.value))
 return True
u.EnumWindows(cb,0);print(wins)
h=wins[0][0];menu=u.GetMenu(h)
for i in range(u.GetMenuItemCount(menu)):
 sub=u.GetSubMenu(menu,i); n=u.GetMenuStringW(menu,i,None,0,0x400);b=ctypes.create_unicode_buffer(n+1);u.GetMenuStringW(menu,i,b,n+1,0x400)
 if b.value=='MMUnity':
  for j in range(u.GetMenuItemCount(sub)):
   nn=u.GetMenuStringW(sub,j,None,0,0x400);bb=ctypes.create_unicode_buffer(nn+1);u.GetMenuStringW(sub,j,bb,nn+1,0x400);print(j,u.GetMenuItemID(sub,j),bb.value)
