from pathlib import Path
import re
root=Path(r'C:\MMUnityPort\Assets\Editor')
files=sorted(root.glob('Build*OpenWorld.cs'))
files=[p for p in files if p.name not in ('BuildEnrothOpenWorld.cs','BuildEnrothFullWorld.cs','BuildEnrothFullWorldFinal.cs')]
new=r'''    static bool IsBuildingObject(string n)
    {
        n=n.ToLowerInvariant();
        return n.Contains("hse")||n.Contains("tav")||n.Contains("inn")||n.Contains("shop")||n.Contains("bank")||n.Contains("smith")||n.Contains("blacksm")||n.Contains("magic")||n.Contains("merc")||n.Contains("armory")||n.Contains("town")||n.Contains("training")||n.Contains("guild")||n.Contains("temple")||n.Contains("stable")||n.Contains("stbl")||n.Contains("stor")||n.Contains("luck")||n.Contains("keep")||n.Contains("d18")||n.Contains("thiev");
    }

    static Material PickBuildingMaterial(GameObject go,bool roof)
    {
        Material fallback=null;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)) foreach(var m in r.sharedMaterials)
        {
            if(!m) continue; if(!fallback) fallback=m;
            string n=m.name.ToLowerInvariant(); bool isRoof=n.Contains("roof")||n.Contains("ruf")||n.Contains("shing")||n.Contains("tile");
            if(roof && isRoof) return m;
            if(!roof && !isRoof && (n.Contains("wal")||n.Contains("wall")||n.Contains("brick")||n.Contains("brck")||n.Contains("wood")||n.Contains("str")||n.Contains("beam"))) return m;
        }
        return fallback;
    }
'''
new+=r'''    static void AddArchitectureSolidCores(Transform root)
    {
        int ncore=0;
        foreach(Transform child in root)
        {
            string n=child.name.ToLowerInvariant();
            if(n.StartsWith("solidcore_")||n.StartsWith("housemass_")||n.StartsWith("houseroof_")) continue;
            if(!IsBuildingObject(n)) continue;
            Bounds b=GetRendererBounds(child.gameObject);
            if(b.size.x<4f||b.size.z<4f||b.size.x>40f||b.size.z>40f||b.size.y<2f||b.size.y>26f) continue;
            Material wall=PickBuildingMaterial(child.gameObject,false), roof=PickBuildingMaterial(child.gameObject,true); if(!wall) continue; if(!roof) roof=wall;
            float sx=Mathf.Max(2f,b.size.x*.68f), sz=Mathf.Max(2f,b.size.z*.68f), sy=Mathf.Max(1.6f,b.size.y*.58f);
            var core=GameObject.CreatePrimitive(PrimitiveType.Cube); core.name="HouseMass_"+child.name; core.transform.SetParent(root,true);
            core.transform.position=new Vector3(b.center.x,b.min.y+sy*.50f,b.center.z); core.transform.localScale=new Vector3(sx,sy,sz);
            core.GetComponent<MeshRenderer>().sharedMaterial=wall; var col=core.GetComponent<Collider>(); if(col) UnityEngine.Object.DestroyImmediate(col);
            float roofY=b.min.y+Mathf.Min(b.size.y*.82f,sy+Mathf.Max(.5f,b.size.y*.16f));
            bool ridgeX=b.size.x>=b.size.z; float pitch=27f;
            for(int side=-1;side<=1;side+=2)
            {
                var rg=GameObject.CreatePrimitive(PrimitiveType.Cube); rg.name="HouseRoof_"+child.name+(side<0?"_L":"_R"); rg.transform.SetParent(root,true);
                if(ridgeX){rg.transform.localScale=new Vector3(Mathf.Max(2f,b.size.x*.76f),.28f,Mathf.Max(1.3f,b.size.z*.43f)); rg.transform.rotation=Quaternion.Euler(side*pitch,0,0); rg.transform.position=new Vector3(b.center.x,roofY,b.center.z+side*b.size.z*.15f);}
                else {rg.transform.localScale=new Vector3(Mathf.Max(1.3f,b.size.x*.43f),.28f,Mathf.Max(2f,b.size.z*.76f)); rg.transform.rotation=Quaternion.Euler(0,0,-side*pitch); rg.transform.position=new Vector3(b.center.x+side*b.size.x*.15f,roofY,b.center.z);}
                rg.GetComponent<MeshRenderer>().sharedMaterial=roof; var rc=rg.GetComponent<Collider>(); if(rc) UnityEngine.Object.DestroyImmediate(rc);
            }
            ncore++;
        }
        Debug.Log("ARCHITECTURE_HOUSE_MASSING "+ncore);
    }
'''
pat=re.compile(r'    static Material ArchitectureCoreMaterial\(\).*?\n\n    static float ArchitectureScaleFactor',re.S)
changed=0
for p in files:
    s=p.read_text(encoding='utf-8-sig')
    if not pat.search(s):
        print('NO_MATCH',p.name); continue
    s=pat.sub(new+'\n\n    static float ArchitectureScaleFactor',s,1)
    p.write_text(s,encoding='utf-8')
    changed+=1
print('patched',changed,'builders')
