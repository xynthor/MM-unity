from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMSourceDecorationPlacementAudit.cs")
s=p.read_text(encoding="utf-8-sig")
lines=s.splitlines()
for i,l in enumerate(lines):
    if "static string Q(string s)" in l:
        lines[i]='    static string Q(string s)=>"\\\""+(s??"").Replace("\\\"","\\\"\\\"")+"\\\"";'
p.write_text("\n".join(lines)+"\n",encoding="utf-8")
print("SOURCE_DECOR_Q_FIXED")