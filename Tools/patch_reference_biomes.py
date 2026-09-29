from pathlib import Path
import shutil
p=Path(r"C:\MMUnityPort\Assets\Editor\MMWitcherTerrainPass.cs")
s=p.read_text(encoding="utf-8-sig")
shutil.copy2(p,Path(r"C:\MMUnityPort\Backups\MMWitcherTerrainPass_pre_reference_lock_20260918.cs"))
start=s.index("    static int CanonicalLand")
end=s.index("    static int ShoreClass",start)
new='''    static int CanonicalLand(string zone,byte g)
    {
        switch(zone)
        {
            // Canonical GREEN world: all non-water land uses the same green family.
            case "NewSorpigal":
            case "CastleIronfist":
            case "MistyIslands":
            case "BootlegBay":
            case "EelInfestedWaters":
            case "SilverCove":
                return Green;

            // Mire is green/wet forest, with only true swamp source cells staying marshy.
            case "MireOfTheDamned":
                return g==7?Marsh:Green;

            // Dragonsand is the yellow desert, but preserve explicit green source patches/islands.
            case "Dragonsand":
                return g==0?Green:Sand;

            // Canonical grey volcanic/dead zones.
            case "HermitsIsle":
            case "SweetWater":
                return Volcanic;

            // Canonical Witcher-like brown/muddy belt. Preserve only explicit green source patches.
            case "ParadiseValley":
                return Mud;
            case "Blackshire":
            case "FreeHaven":
                return g==0?Green:Mud;
            case "Kriegspire":
                return g==3?Volcanic:Mud;

            // Frozen Highlands is the white snow biome.
            case "FrozenHighlands":
                return Snow;
        }
        if(g==0)return Green;if(g==1)return Snow;if(g==2)return Sand;if(g==3)return Volcanic;
        if(g==4)return Mud;if(g==6)return Volcanic;if(g==7)return Marsh;return Green;
    }

'''
s=s[:start]+new+s[end:]
s=s.replace('Layer("DarkVolcanic","Assets/EnvironmentAssets/Biomes/black_soil.png","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_nor_gl_1k.jpg",7f,.02f)',
            'Layer("DarkVolcanic","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_nor_gl_1k.jpg",7f,.02f)')
s=s.replace('Layer("Mud","Assets/EnvironmentAssets/UnitySamples/mud_cracked_dry_c.png","Assets/Environment/PolyHaven/Textures/damp_sand/damp_sand_nor_gl_1k.jpg",7f,.08f)',
            'Layer("Mud","Assets/Environment/PolyHaven/Textures/grass_path_2/grass_path_2_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/damp_sand/damp_sand_nor_gl_1k.jpg",7f,.06f)')
p.write_text(s,encoding="utf-8")
print("REFERENCE_BIOME_LOCK_PATCHED")
