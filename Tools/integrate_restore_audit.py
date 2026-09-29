from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMSourceDecorationRestorePass.cs")
s=p.read_text(encoding="utf-8-sig")
insert=r'''
    public static void AuditAll(){
        Directory.CreateDirectory("Validation/SourceDecorationAudit");
        var sum=new List<string>{"zone,rows,trees,rocks,sprites,nonvisual,missing,xz_bad,extra_source_visuals"};
        foreach(var z in Zones){
            var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
            var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
            var all=root.GetComponentsInChildren<Transform>(true).ToArray();
            string[] lines=File.ReadAllLines($"Assets/World/{z.key}/Data/decorations.csv");
            int rows=0,trees=0,rocks=0,sprites=0,nonvisual=0,missing=0,bad=0,extra=0;
            var expected=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var detail=new List<string>{"index,name,category,expected_x,expected_z,scene_x,scene_z,dx,dz,status"};
            for(int i=1;i<lines.Length;i++){
                var q=lines[i].Split(',');if(q.Length<10)continue;string n=q[1].Trim().ToLowerInvariant();if(Start(n))continue;
                if(!P(q[4],out float x)||!P(q[6],out float oz))continue;float zz=-oz;int idx=i-1;int.TryParse(q[0],out idx);
                string cat,sceneName;
                if(Tree(n)){cat="TREE";sceneName=$"SourceTree_{idx:0000}_{n}";trees++;}
                else if(Rock(n)){cat="ROCK";sceneName=$"SourceRock_{idx:0000}_{n}";rocks++;}
                else if(NonVisual(n)){cat="NONVISUAL";sceneName="";nonvisual++;rows++;continue;}
                else{cat="SPRITE";sceneName=$"SRC_{i:000}_{n}";sprites++;}
                rows++;expected.Add(sceneName);
                var t=all.FirstOrDefault(a=>a.name.Equals(sceneName,StringComparison.OrdinalIgnoreCase));
                if(!t){missing++;detail.Add($"{idx},{n},{cat},{x:F4},{zz:F4},,,,,MISSING");continue;}
                float dx=t.position.x-x,dz=t.position.z-zz;bool xb=Mathf.Abs(dx)>.01f||Mathf.Abs(dz)>.01f;if(xb)bad++;
                detail.Add($"{idx},{n},{cat},{x:F4},{zz:F4},{t.position.x:F4},{t.position.z:F4},{dx:F4},{dz:F4},{(xb?"XZ_BAD":"OK")}");
            }
            foreach(var t in all){
                string n=t.name;
                if((n.StartsWith("SRC_",StringComparison.OrdinalIgnoreCase)||n.StartsWith("SourceTree_",StringComparison.OrdinalIgnoreCase)||n.StartsWith("SourceRock_",StringComparison.OrdinalIgnoreCase))&&!expected.Contains(n))extra++;
            }
            File.WriteAllLines($"Validation/SourceDecorationAudit/{z.key}.csv",detail);
            sum.Add($"{z.key},{rows},{trees},{rocks},{sprites},{nonvisual},{missing},{bad},{extra}");
            Debug.Log($"SOURCE_DECOR_AUDIT {z.key} rows={rows} missing={missing} xzBad={bad} extra={extra}");
        }
        File.WriteAllLines("Validation/SourceDecorationAudit/summary.csv",sum);
        Debug.Log("SOURCE_DECOR_AUDIT_DONE zones="+Zones.Length);
    }
'''
idx=s.rfind("\n}")
if idx<0: raise SystemExit("class end not found")
s=s[:idx]+insert+s[idx:]
p.write_text(s,encoding="utf-8")
print("AUDIT_INTEGRATED")
