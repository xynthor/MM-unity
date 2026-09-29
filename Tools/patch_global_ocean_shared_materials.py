from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMGlobalOceanPass.cs")
s=p.read_text(encoding="utf-8-sig")
a=s.index('    static Material OceanMaterial()')
b=s.index('    static Material SeabedMaterial()',a)
c=s.index('\n    }\n}',b)+6
new='''    static Material OceanMaterial()
    {
        return MMUnifiedWorldWaterPass.SharedWaterMaterial();
    }

    static Material SeabedMaterial()
    {
        return MMUnifiedSeabedPass.SharedMaterial();
    }
'''
s=s[:a]+new+s[c:]
p.write_text(s,encoding="utf-8")
print("GLOBAL_OCEAN_SHARED_MATERIALS_PATCHED")
