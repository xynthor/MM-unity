using System; using System.IO; using System.Linq; using System.Text; using System.Globalization; using System.Collections.Generic; using System.Security.Cryptography;
using UnityEngine; using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine.SceneManagement;
internal class CommandScript : IRunCommand {
 const string Root="Validation/Coast17"; const string Backup="Backups/Coast17_20260919";
 string MeshPath(Renderer r){var mf=r.GetComponent<MeshFilter>();return mf?AssetDatabase.GetAssetPath(mf.sharedMesh):r is SkinnedMeshRenderer sk?AssetDatabase.GetAssetPath(sk.sharedMesh):"none";}
 string F(float x)=>x.ToString("R",CultureInfo.InvariantCulture);
 string V(Vector3 v)=>F(v.x)+","+F(v.y)+","+F(v.z);
 string Id(UnityEngine.Object o)=>o?GlobalObjectId.GetGlobalObjectIdSlow(o).ToString():"null";
 string Hash(IEnumerable<string> lines){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n",lines)))).Replace("-","");}
 string[] Snapshot(Scene s,HashSet<string> permitted){var rows=new List<string>();foreach(var t in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))){rows.Add("T|"+Id(t)+"|"+Id(t.parent)+"|"+V(t.localPosition)+"|"+t.localRotation.ToString("R")+"|"+V(t.localScale)+"|"+V(t.position)+"|"+t.gameObject.activeSelf+"|"+GameObjectUtility.GetStaticEditorFlags(t.gameObject));foreach(var r in t.GetComponents<Renderer>()){var text=EditorJsonUtility.ToJson(r);if(permitted.Contains(Id(r)))text=text.Replace("\"m_Enabled\":true","\"m_Enabled\":false").Replace("\"m_Enabled\":1","\"m_Enabled\":0");rows.Add("R|"+Id(r)+"|"+text);}foreach(var mf in t.GetComponents<MeshFilter>())rows.Add("M|"+Id(mf)+"|"+Id(mf.sharedMesh));foreach(var c in t.GetComponents<Collider>())rows.Add("C|"+Id(c)+"|"+EditorJsonUtility.ToJson(c));}rows.Sort(StringComparer.Ordinal);return rows.ToArray();}
 void Protect(bool[,] mask,Terrain t,Scene s){int n=mask.GetLength(0);float step=t.terrainData.size.x/(n-1);var origin=t.transform.position;
 foreach(var tr in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))){if(tr.GetComponent<Terrain>()||IsWater(tr.name))continue;var p=tr.position-origin;int x=Mathf.RoundToInt(p.x/step),z=Mathf.RoundToInt(p.z/step);if(x<0||z<0||x>=n||z>=n)continue;int rad=Mathf.CeilToInt(2f/step);for(int j=Math.Max(0,z-rad);j<=Math.Min(n-1,z+rad);j++)for(int i=Math.Max(0,x-rad);i<=Math.Min(n-1,x+rad);i++)mask[j,i]=true;}
 foreach(var r in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true))){if(IsWater(r.name))continue;var b=r.bounds;if(b.min.y>.7f||b.size.x*b.size.z>5000)continue;int x0=Math.Max(0,Mathf.FloorToInt((b.min.x-origin.x-1)/step)),x1=Math.Min(n-1,Mathf.CeilToInt((b.max.x-origin.x+1)/step));int z0=Math.Max(0,Mathf.FloorToInt((b.min.z-origin.z-1)/step)),z1=Math.Min(n-1,Mathf.CeilToInt((b.max.z-origin.z+1)/step));for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)mask[z,x]=true;}}
 bool IsWater(string n)=>n.StartsWith("Water -")||n=="Internal Water - Smooth"||n=="Unified Water Bottom"||n.StartsWith("Curved Seabed")||n=="Global Ocean Surface"||n=="Ocean + Central Lake Water";
 void Capture(Scene s,Terrain t,string path,Vector3 point){var go=new GameObject("__CoastAuditCamera");SceneManager.MoveGameObjectToScene(go,s);try{var c=go.AddComponent<Camera>();c.nearClipPlane=.1f;c.farClipPlane=5000;c.fieldOfView=55;c.depthTextureMode=DepthTextureMode.Depth;c.transform.position=point+new Vector3(32,25,-38);c.transform.LookAt(point);MMSequentialEvidence.Render(c,path+"_detail.png",960,720);c.orthographic=true;c.orthographicSize=t.terrainData.size.x*.55f;c.transform.position=t.transform.position+new Vector3(t.terrainData.size.x*.5f,800,t.terrainData.size.z*.5f);c.transform.rotation=Quaternion.Euler(90,0,0);MMSequentialEvidence.Render(c,path+"_top.png",800,800);}finally{UnityEngine.Object.DestroyImmediate(go);}}
 public void Execute(ExecutionResult result){
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene; refusing switch");
  Directory.CreateDirectory(Root);Directory.CreateDirectory(Backup);
  string scenePath=File.ReadAllText(Root+"/next.txt").Trim();string key=Path.GetFileNameWithoutExtension(scenePath);string outdir=Root+"/"+key;Directory.CreateDirectory(outdir);
  if(File.Exists(outdir+"/done.txt")){result.Log(key+" already processed");return;}
  // Terrain assets are shared with the linked world: protect both layouts before editing.
  var linked=SceneManager.GetActiveScene();if(linked.name!="Enroth_Linked_OpenWorld")linked=EditorSceneManager.OpenScene("Assets/Scenes/Enroth_Linked_OpenWorld.unity",OpenSceneMode.Single);
  var linkedMasks=new Dictionary<string,bool[,]>();
  foreach(var t in linked.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))){var mask=new bool[t.terrainData.heightmapResolution,t.terrainData.heightmapResolution];Protect(mask,t,linked);linkedMasks[AssetDatabase.GetAssetPath(t.terrainData)]=mask;}
  var s=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);var terrain=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();var td=terrain.terrainData;string asset=AssetDatabase.GetAssetPath(td);
  var rs=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).ToArray();
  // Only the legacy draped overlay is always redundant with the underlying terrain.
  var redundant=rs.Where(r=>r.name=="Unified Water Bottom").ToArray();var allowed=new HashSet<string>(redundant.Select(r=>Id(r)));
  var snapshot=Snapshot(s,allowed);File.WriteAllLines(outdir+"/invariants_before.txt",snapshot);string beforeHash=Hash(snapshot);
  var enabled=rs.ToDictionary(r=>Id(r),r=>r.enabled);File.WriteAllLines(outdir+"/renderer_states.txt",rs.Select(r=>Id(r)+"|"+r.name+"|"+r.enabled+"|"+MeshPath(r)));
  string sceneBackup=Backup+"/"+Path.GetFileName(scenePath);if(!File.Exists(sceneBackup))File.Copy(scenePath,sceneBackup);
  string assetBackup=Backup+"/"+Path.GetFileName(asset);if(!File.Exists(assetBackup))File.Copy(asset,assetBackup);
  int n=td.heightmapResolution;float step=td.size.x/(n-1),water=.12f,baseY=terrain.transform.position.y,scale=td.size.y;var original=td.GetHeights(0,0,n,n);var heights=(float[,])original.Clone();var protect=linkedMasks.ContainsKey(asset)?linkedMasks[asset]:new bool[n,n];Protect(protect,terrain,s);
  var editable=new bool[n,n];int candidates=0;float maxSlopeBefore=0;
  // Freeze the exact shoreline crossing cells, dry land, object footprints, and a 4m seam collar.
  for(int z=4;z<n-4;z++)for(int x=4;x<n-4;x++){if(protect[z,x]||baseY+original[z,x]*scale>=water-.2f)continue;bool wet=true;for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)if(baseY+original[z+dz,x+dx]*scale>=water)wet=false;if(wet){editable[z,x]=true;candidates++;maxSlopeBefore=Mathf.Max(maxSlopeBefore,Mathf.Abs(original[z,x]-original[z,x+1])*scale/step,Mathf.Abs(original[z,x]-original[z+1,x])*scale/step);}}
  // Diffuse shelf discontinuities without increasing terrain resolution or introducing dry/wet crossings.
  for(int pass=0;pass<32;pass++){var next=(float[,])heights.Clone();for(int z=4;z<n-4;z++)for(int x=4;x<n-4;x++)if(editable[z,x]){float avg=(heights[z-1,x]+heights[z+1,x]+heights[z,x-1]+heights[z,x+1])*0.25f;next[z,x]=Mathf.Min((water-.2f-baseY)/scale,Mathf.Lerp(heights[z,x],avg,.65f));}heights=next;}
  int changed=0,dryChanged=0,maskChanges=0,protectedChanges=0;float maxDelta=0,maxSlopeAfter=0;int bestX=n/2,bestZ=n/2;
  for(int z=0;z<n;z++)for(int x=0;x<n;x++){float a=baseY+original[z,x]*scale,b=baseY+heights[z,x]*scale;if((a<water)!=(b<water))maskChanges++;if(original[z,x]!=heights[z,x]){changed++;if(a>=water)dryChanged++;if(protect[z,x])protectedChanges++;float d=Mathf.Abs(a-b);if(d>maxDelta){maxDelta=d;bestX=x;bestZ=z;}}if(editable[z,x])maxSlopeAfter=Mathf.Max(maxSlopeAfter,Mathf.Abs(heights[z,x]-heights[z,x+1])*scale/step,Mathf.Abs(heights[z,x]-heights[z+1,x])*scale/step);}
  if(dryChanged!=0||maskChanges!=0||protectedChanges!=0||maxSlopeAfter>maxSlopeBefore+.0001f)throw new Exception("Terrain preflight failed");
  Vector3 view=terrain.transform.position+new Vector3(bestX*step,water-baseY+1,bestZ*step);if(key=="NewSorpigal_OpenWorld")view=new Vector3(50,1,-30);
  Capture(s,terrain,outdir+"/before",view);
  try{
   td.SetHeights(0,0,heights);terrain.Flush();long removed=0;foreach(var r in redundant)if(r.enabled){var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh)for(int k=0;k<mesh.subMeshCount;k++)removed+=(long)mesh.GetIndexCount(k)/3;r.enabled=false;}
   string afterHash=Hash(Snapshot(s,allowed));if(afterHash!=beforeHash)throw new Exception("Transform/renderer/mesh invariant failed");
   Capture(s,terrain,outdir+"/after",view);
   // Save only this terrain and scene; never flush unrelated dirty assets.
   EditorUtility.SetDirty(td);AssetDatabase.SaveAssetIfDirty(td);
   if(redundant.Any(r=>enabled[Id(r)])){EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Scene save failed");}
   s=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
   var savedSnapshot=Snapshot(s,allowed);if(Hash(savedSnapshot)!=beforeHash)throw new Exception("Saved/reloaded invariant mismatch");
   File.WriteAllLines(outdir+"/invariants_after.txt",savedSnapshot);
   File.WriteAllText(outdir+"/done.txt","scene="+scenePath+"\nterrain="+asset+"\nhash_before="+beforeHash+"\nhash_after="+afterHash+"\nchanged_height_samples="+changed+"\ndry_changed="+dryChanged+"\nwet_mask_changes="+maskChanges+"\nprotected_changes="+protectedChanges+"\nmax_depth_delta_m="+F(maxDelta)+"\nmax_slope_before="+F(maxSlopeBefore)+"\nmax_slope_after="+F(maxSlopeAfter)+"\ntriangles_added=0\noverlay_triangles_disabled="+removed+"\nshoreline_and_seam_crossings=preserved\n");
   result.Log(key+" saved; height_samples="+changed+" invariant_hash=PASS dry_changes=0 wet_mask_changes=0 triangles_added=0");
  }catch{td.SetHeights(0,0,original);EditorUtility.SetDirty(td);AssetDatabase.SaveAssetIfDirty(td);foreach(var r in rs)r.enabled=enabled[Id(r)];throw;}
 }
}

