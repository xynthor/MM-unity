from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMVegetationDesertRepairPass.cs")
s=p.read_text(encoding="utf-8-sig")
old='''        string n=go.name.ToLowerInvariant();
        string p=SourcePath(go).ToLowerInvariant();
        return n.Contains("tree")||n.Contains("pine")||n.Contains("grove")||p.Contains("tree")||p.Contains("pine");'''
new='''        string n=go.name.ToLowerInvariant();
        string p=SourcePath(go).ToLowerInvariant();
        if(p.Contains("quiver_tree")||n.Contains("quiver"))return false; // naturally broad but upright; transform audit handles tilt
        return n.Contains("tree")||n.Contains("pine")||n.Contains("grove")||p.Contains("tree")||p.Contains("pine");'''
if old not in s: raise SystemExit("TreeLike block not found")
p.write_text(s.replace(old,new,1),encoding="utf-8")
print("PATCHED_QUICKER_VERTICAL_RULE")