from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticTerrainBiomePass.cs")
s=p.read_text(encoding="utf-8-sig")

s=s.replace('''Layer("Green","Assets/Environment/PolyHaven/Textures/grass_ground/grass_ground_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/grass_ground/grass_ground_nor_gl_1k.jpg",7.5f,.025f),''',
'''Layer("Green","Assets/EnvironmentAssets/UnitySamples/ground_grass_fells_mossy_CH.png","Assets/EnvironmentAssets/UnitySamples/ground_grass_fells_mossy_NOH.png",9f,.035f),''')
s=s.replace('''Layer("LightGreen","Assets/Environment/PolyHaven/Textures/grass_path_2/grass_path_2_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/grass_path_2/grass_path_2_nor_gl_1k.jpg",7.2f,.035f),''',
'''Layer("LightGreen","Assets/Materials/RealisticWorld/LightGreen_blend.png","Assets/EnvironmentAssets/UnitySamples/ground_grass_fells_mossy_NOH.png",8f,.03f),''')
s=s.replace('''Layer("RoadOverlay","Assets/Environment/PolyHaven/Textures/grass_path_2/grass_path_2_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/grass_path_2/grass_path_2_nor_gl_1k.jpg",5.8f,.04f)''',
'''Layer("RoadOverlay","Assets/EnvironmentAssets/UnitySamples/stone_ground_CH.png","Assets/EnvironmentAssets/UnitySamples/stone_ground_noh.png",6f,.12f)''')

# Strengthen the old-good green look while keeping smooth ecotones.
old='''        if(c==Green){Add(w,Green,amount*.95f);Add(w,LightGreen,amount*.05f);}
        else if(c==LightGreen){Add(w,LightGreen,amount*.88f);Add(w,Green,amount*.12f);}'''
new='''        if(c==Green){Add(w,Green,amount*.985f);Add(w,LightGreen,amount*.015f);}
        else if(c==LightGreen){Add(w,LightGreen,amount*.82f);Add(w,Green,amount*.18f);}'''
if old not in s: raise SystemExit("green sample block missing")
s=s.replace(old,new,1)

old='''            if(bc==Green){w[Green]*=.95f+.08f*n;w[LightGreen]+=.035f*(1f-n);}
            else if(bc==LightGreen){w[LightGreen]*=.94f+.08f*n;w[Green]+=.025f*n;}'''
new='''            if(bc==Green){w[Green]*=1.00f+.04f*n;w[LightGreen]+=.012f*(1f-n);}
            else if(bc==LightGreen){w[LightGreen]*=.96f+.06f*n;w[Green]+=.055f*n;}'''
if old not in s: raise SystemExit("green noise block missing")
s=s.replace(old,new,1)

p.write_text(s,encoding="utf-8")
print("OLD_GOOD_GREEN_STYLE_PATCHED")
