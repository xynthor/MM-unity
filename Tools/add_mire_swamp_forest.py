from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\MMSourceGridExactPostPass.cs')
s=p.read_text(encoding='utf-8')
old='''        }
        return changed;
    }

    static int FixSidewaysVegetation'''
new='''        }
        if(z.key=="MireOfTheDamned")
        {
            var swampA=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_005/Pine_005_01.prefab");
            var swampB=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_006/pine_006_02.prefab");
            var swampC=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_007/pine_007_01.prefab");
            var swampTrees=new[]{swampA,swampB,swampC}.Where(x=>x).ToArray();
            if(swampTrees.Length>0)
            for(int sy=1;sy<N-1;sy++) for(int sx=1;sx<N-1;sx++)
            {
                byte raw=tilemap[sy*N+sx]; if(groups[raw]!=7) continue;
                int h=(sx*73856093)^(sy*19349663)^0x5A17; if((h&0x7fffffff)%9!=0) continue;
''                float x=(sx-64f)*4f+((((h>>5)&255)/255f-.5f)*3.0f);
                float zp=(sy-64f)*4f+((((h>>13)&255)/255f-.5f)*3.0f);
                float nx=(x-terrain.transform.position.x)/terrain.terrainData.size.x;
                float nz=(zp-terrain.transform.position.z)/terrain.terrainData.size.z;
                if(nx<.01f||nz<.01f||nx>.99f||nz>.99f) continue;
                if(terrain.terrainData.GetSteepness(nx,nz)>36f||NearArchitecture(arch,x,zp,4.0f)) continue;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(swampTrees[Mathf.Abs(h)%swampTrees.Length]);
                go.name="SwampForest_"+sx+"_"+sy;
                go.transform.SetParent(biomeRoot.transform,true);
                float y=terrain.SampleHeight(new Vector3(x,0f,zp))+terrain.transform.position.y;
                go.transform.position=new Vector3(x,y,zp);
                go.transform.rotation=Quaternion.Euler(0f,(h&1023)*.3519f,0f);
                ScaleToHeight(go,Mathf.Lerp(5.2f,9.5f,(Mathf.Abs(h>>9)%1000)/999f));
                ApplyRealisticTreeMaterials(go.transform);
                changed++;
            }
        }
        return changed;
    }

    static int FixSidewaysVegetation'''
if old not in s:
    raise SystemExit('anchor missing')
s=s.replace(old,new,1)
p.write_text(s,encoding='utf-8')
print('added Mire swamp forest')
