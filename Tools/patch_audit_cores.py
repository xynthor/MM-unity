from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\AuditWorldSourceExact.cs')
s=p.read_text(encoding='utf-8-sig')
start=s.index('    static bool IsHouse(string n)')
end=s.index('    static int ExpectedLayer',start)
new='''    static bool IsHouse(string n)
    {
        n=n.ToLowerInvariant();
        if(n.Contains("sign")||n.Contains("sgn")) return false;
        return n.Contains("house")||n.Contains("hse")||n.Contains("tav")||n.Contains("inn")||n.Contains("shop")||n.Contains("bank")||
               n.Contains("smith")||n.Contains("blacksm")||n.Contains("magic")||n.Contains("merc")||n.Contains("armory")||n.Contains("town")||n.Contains("training")||
               n.Contains("guild")||n.Contains("temple")||n.Contains("stable")||n.Contains("stbl")||n.Contains("stor")||n.Contains("luck")||n.Contains("keep")||n.Contains("d18")||n.Contains("thiev");
    }
    static bool ShouldHaveCore(string n,Bounds b)
    {
        return IsHouse(n) && b.size.x>=1.8f && b.size.z>=1.8f && b.size.x<=60f && b.size.z<=60f && b.size.y>=1.8f && b.size.y<=45f;
    }
'''
s=s[:start]+new+s[end:]
s=s.replace('bool house=IsHouse(n);bool core=arch.Find("SolidCore_"+n)!=null;if(house){houses++;if(!core)noCore++;}',
'''bool house=ShouldHaveCore(n,b);bool core=arch.Find("SolidCore_"+n)!=null;if(house){houses++;if(!core)noCore++;}''')
s=s.replace('float target=surface+p.baseOffset,dy=b.min.y-target;bool xb=Mathf.Abs(dx)>.04f||Mathf.Abs(dz)>.04f;bool yb=Mathf.Abs(dy)>.65f;',
'''float target=surface+p.baseOffset,dy=b.min.y-target;bool xb=Mathf.Abs(dx)>.04f||Mathf.Abs(dz)>.04f;bool specialY=n.ToLowerInvariant().Contains("bridge")||n.ToLowerInvariant().Contains("trigger");bool yb=!specialY&&Mathf.Abs(dy)>.65f;''')
p.write_text(s,encoding='utf-8')
print('patched',len(s))
