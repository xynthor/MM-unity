from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticTerrainBiomePass.cs")
s=p.read_text(encoding="utf-8-sig")

# Blackshire: never VeryGreen as baseline; desert patches stay sand.
s=s.replace('''            case "Blackshire":
                if(g==2)return Sand;
                if(g==0)return VeryGreen;
                return SlightGreen;''',
'''            case "Blackshire":
                if(g==2)return Sand;
                return SlightGreen;''')

# Kriegspire base fallback is slight green; snow is applied geographically in BaseClassAt.
s=s.replace('''            case "Kriegspire":
                if(g==1)return Snow;
                if(g==3)return Volcanic;
                return SlightGreen;''',
'''            case "Kriegspire":
                return SlightGreen;''')

# Frozen Highlands base fallback is slight green; north/south split handled geographically too.
s=s.replace('''            case "FrozenHighlands":
                if(g==1)return Snow;
                return SlightGreen;''',
'''            case "FrozenHighlands":
                return SlightGreen;''')

needle='''        return SlightGreen;
    }

    static void Add(float[] w,int k,float v)'''
insert='''        return SlightGreen;
    }

    public static int BaseClassAt(string zone,byte g,byte f,int sy)
    {
        int baseClass=BaseClass(zone,g,f);
        if(baseClass==Road||baseClass==Rock||baseClass==Sand||baseClass==WetMud)return baseClass;

        // Reference-map geographic rule: north is snow, south is slightly-green muddy.
        // Source Y grows southward, so sy < 64 is north.
        if(zone=="Kriegspire"||zone=="FrozenHighlands")
            return sy<64 ? Snow : SlightGreen;

        return baseClass;
    }

    static void Add(float[] w,int k,float v)'''
if needle not in s: raise SystemExit("BaseClassAt insertion point missing")
s=s.replace(needle,insert,1)

# SampleSource must use geographic rule.
s=s.replace('''        int c=BaseClass(zone,g,f);''','''        int c=BaseClassAt(zone,g,f,iy);''',1)

# Main current-cell class must use geographic rule.
s=s.replace('''            int bc=BaseClass(z.key,g,f);''','''            int bc=BaseClassAt(z.key,g,f,cy);''',1)

p.write_text(s,encoding="utf-8")
print("BLACKSHIRE_KRIEGSPIRE_RULES_PATCHED")
