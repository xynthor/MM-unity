from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticTerrainBiomePass.cs")
s=p.read_text(encoding="utf-8-sig")

s=s.replace('''            case "SilverCove":
                return g==255?SlightGreen:VeryGreen;''',
'''            case "SilverCove":
                return g==0 ? VeryGreen : SlightGreen;''')

s=s.replace('''            case "FrozenHighlands":
                return SlightGreen;''',
'''            case "FrozenHighlands":
                return Snow;''')

old='''        if(zone=="Kriegspire"||zone=="FrozenHighlands")
            return sy<64 ? Snow : SlightGreen;'''
new='''        if(zone=="Kriegspire")
            return sy<64 ? Snow : SlightGreen;

        if(zone=="FrozenHighlands")
            return Snow;'''
if old not in s: raise SystemExit("split rule needle missing")
s=s.replace(old,new,1)

p.write_text(s,encoding="utf-8")
print("WHITECAP_SILVERCOVE_FIXED")
