using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class BuildDragonIsleOpenWorld
{
    const string ScenePath="Assets/Scenes/DragonIsle_Reference.unity";
    const string Gen="Assets/World/DragonIsle/Generated";
    const string Mat="Assets/Materials/DragonIsle";
    const float Size=512f, BaseY=-24f, Height=320f, WaterY=.12f;
    const int R=513;

    [MenuItem("MMUnity/Build Dragon Isle Reference")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory(Gen); Directory.CreateDirectory(Mat);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        scene.name="DragonIsle_Reference";
        var root=new GameObject("Dragon Isle - Open World 1x1 Reference");
        var terrain=BuildTerrain(root.transform);
        BuildWater(root.transform);
        BuildForest(root.transform,terrain);
        BuildLandmarks(root.transform,terrain);
        SetupLight(root.transform);
        EditorSceneManager.SaveScene(scene,ScenePath);
        AssetDatabase.SaveAssets();
        RenderPreview(scene);
        Debug.Log("DRAGON_ISLE_DONE size=512 referenceShape=long_north_spur forest=SW lake=central landmarks=4");
    }

    static float Blob(float x,float z,float cx,float cz,float rx,float rz,float rotDeg=0f)
    {
        float a=rotDeg*Mathf.Deg2Rad,c=Mathf.Cos(a),s=Mathf.Sin(a),dx=x-cx,dz=z-cz;
        float qx=c*dx+s*dz,qz=-s*dx+c*dz;
        return 1f-Mathf.Sqrt(qx*qx/(rx*rx)+qz*qz/(rz*rz));
    }
    static float IslandField(float x,float z)
    {
        float f=-10f;
        f=Mathf.Max(f,Blob(x,z,-.16f,-.43f,.34f,.36f,-10f));
        f=Mathf.Max(f,Blob(x,z,-.04f,-.12f,.31f,.38f,-8f));
        f=Mathf.Max(f,Blob(x,z,.08f,.20f,.23f,.36f,-12f));
        f=Mathf.Max(f,Blob(x,z,.20f,.48f,.15f,.33f,-18f));
        f=Mathf.Max(f,Blob(x,z,.31f,.72f,.075f,.30f,-22f));
        f=Mathf.Max(f,Blob(x,z,-.30f,-.62f,.16f,.16f,0f));
        return f;
    }
    static float LakeField(float x,float z){return Blob(x,z,-.07f,.14f,.105f,.135f,10f);}

    static TerrainLayer Layer(string n,string tex,float tile)
    {
        string path=$"{Mat}/{n}.terrainlayer"; var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if(!l){l=new TerrainLayer();AssetDatabase.CreateAsset(l,path);} l.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(tex);
        l.tileSize=new Vector2(tile,tile);l.metallic=0;l.smoothness=.05f;EditorUtility.SetDirty(l);return l;
    }
    static Terrain BuildTerrain(Transform parent)
    {
        string p=$"{Gen}/DragonIsleTerrain.asset";var td=AssetDatabase.LoadAssetAtPath<TerrainData>(p);
        if(!td){td=new TerrainData();AssetDatabase.CreateAsset(td,p);} td.heightmapResolution=R;td.size=new Vector3(Size,Height,Size);
        var grass=Layer("Grass","Assets/EnvironmentAssets/Biomes/temperate_grass.png",12f);
        var sand=Layer("ShoreSand","Assets/EnvironmentAssets/Biomes/sand.png",10f);
        var dirt=Layer("Dirt","Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png",10f);
        var rock=Layer("Rock","Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_c.png",9f);
        td.terrainLayers=new[]{grass,sand,dirt,rock};
        var hm=new float[R,R];
        for(int y=0;y<R;y++)for(int x=0;x<R;x++)
        {
            float wx=-Size*.5f+x/(float)(R-1)*Size,wz=-Size*.5f+y/(float)(R-1)*Size;
            float u=wx/(Size*.5f),v=wz/(Size*.5f);float f=IslandField(u,v);
            float coastNoise=(Mathf.PerlinNoise(u*3.1f+7.7f,v*3.1f+19.2f)-.5f)*.085f+(Mathf.PerlinNoise(u*8.7f+31.4f,v*8.1f+4.6f)-.5f)*.025f;
            f+=coastNoise*Mathf.Clamp01(1f-Mathf.Abs(f)*2.8f);float lake=LakeField(u,v);float world;
            if(f<0f) world=-Mathf.Lerp(1.2f,16f,Mathf.Clamp01(-f*2.4f));
            else
            {
                float edge=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(0f,.24f,f));
                float n1=(Mathf.PerlinNoise(u*3.7f+11.2f,v*4.0f+3.7f)-.5f)*5.2f;
                float n2=(Mathf.PerlinNoise(u*11.2f+41.8f,v*10.6f+27.1f)-.5f)*1.35f;
                float eastRidge=Mathf.Exp(-Mathf.Pow((u-(.13f+.16f*v))/.16f,2f))*Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(-.25f,.70f,v));
                float southMass=Mathf.Exp(-((u+.24f)*(u+.24f)/.055f+(v+.47f)*(v+.47f)/.085f));
                float northCliff=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.18f,.78f,v))*Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.04f,.22f,f))*5.5f;
                world=.48f+edge*(5.2f+n1+n2+eastRidge*24f+southMass*14f+northCliff);
                if(lake>.18f) world=Mathf.Lerp(world,-2.8f,Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.18f,.50f,lake)));
            }
            hm[y,x]=Mathf.Clamp01((world-BaseY)/Height);
        }
        td.SetHeights(0,0,hm);td.alphamapResolution=512;var a=new float[512,512,4];
        for(int y=0;y<512;y++)for(int x=0;x<512;x++)
        {
            float wx=-256f+(x+.5f),wz=-256f+(y+.5f),u=wx/256f,v=wz/256f;float f=IslandField(u,v);
            float coastNoise=(Mathf.PerlinNoise(u*3.1f+7.7f,v*3.1f+19.2f)-.5f)*.085f+(Mathf.PerlinNoise(u*8.7f+31.4f,v*8.1f+4.6f)-.5f)*.025f;
            f+=coastNoise*Mathf.Clamp01(1f-Mathf.Abs(f)*2.8f);float lake=LakeField(u,v);
            float h=td.GetInterpolatedHeight((x+.5f)/512f,(y+.5f)/512f)+BaseY;
            float slope=td.GetSteepness((x+.5f)/512f,(y+.5f)/512f);bool exposed=v>.12f||u>.28f||slope>15f;
            if(lake>.18f){a[y,x,3]=.72f;a[y,x,1]=.28f;}
            else if(f<.075f){if(exposed){a[y,x,3]=.74f;a[y,x,1]=.26f;}else{a[y,x,1]=.82f;a[y,x,3]=.18f;}}
            else if(slope>25f||h>19f){a[y,x,3]=.82f;a[y,x,2]=.18f;}
            else if(f<.15f){a[y,x,1]=exposed?.48f:.74f;a[y,x,3]=exposed?.42f:.16f;a[y,x,0]=.10f;}
            else {float d=Mathf.Clamp01((Mathf.PerlinNoise(u*5f+7,v*5f+9)-.36f)*.62f);a[y,x,0]=.74f-d*.45f;a[y,x,2]=.26f+d*.35f;a[y,x,3]=d*.10f;}
        }
        td.SetAlphamaps(0,0,a);
        var go=Terrain.CreateTerrainGameObject(td);go.name="Terrain_DragonIsle_Reference_1x1";go.transform.SetParent(parent);go.transform.position=new Vector3(-256f,BaseY,-256f);
        var t=go.GetComponent<Terrain>();t.drawInstanced=true;t.heightmapPixelError=1.5f;t.basemapDistance=1500f;return t;
    }

    static Material WaterMaterial()
    {
        return MMUnifiedWorldWaterPass.SharedWaterMaterial();
    }
    static void BuildWater(Transform parent)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Plane);go.name="Ocean + Central Lake Water";go.transform.SetParent(parent);go.transform.position=new Vector3(0,WaterY,0);go.transform.localScale=new Vector3(52f,1f,52f);
        go.GetComponent<MeshRenderer>().sharedMaterial=WaterMaterial();var c=go.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
    }

    static int Hash(int x,int z){unchecked{return x*73856093^z*19349663^0x51f15e;} }
    static Bounds VB(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static void NaturalPlantMaterials(GameObject go,string kind)
    {
        if(kind!="shrub")return; // accepted final-world tree prefabs keep their validated materials
        var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RealisticWorld/Shrub02.mat");
        if(!m)return;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var a=r.sharedMaterials;if(a==null||a.Length==0)a=new Material[1];
            for(int i=0;i<a.Length;i++)a[i]=m;r.sharedMaterials=a;
        }
    }
    static void UprightGroundScale(GameObject go,Terrain terrain,float h)
    {
        Quaternion baseRot=go.transform.rotation,best=baseRot;float bestY=VB(go).size.y;
        foreach(var q in new[]{baseRot*Quaternion.Euler(90,0,0),baseRot*Quaternion.Euler(-90,0,0),baseRot*Quaternion.Euler(0,0,90),baseRot*Quaternion.Euler(0,0,-90)})
        {go.transform.rotation=q;float y=VB(go).size.y;if(y>bestY){bestY=y;best=q;}}
        go.transform.rotation=best;Bounds b=VB(go);if(b.size.y>.01f)go.transform.localScale*=h/b.size.y;
        b=VB(go);float y0=terrain.SampleHeight(new Vector3(b.center.x,0,b.center.z))+terrain.transform.position.y;go.transform.position+=Vector3.up*(y0-b.min.y);
    }
    static void BuildForest(Transform parent,Terrain terrain)
    {
        var root=new GameObject("Forest - Natural Sparse");root.transform.SetParent(parent);
        var fir=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/FinalWorldVegetation/fir_sapling.prefab");
        var broad1=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/FinalWorldVegetation/island_tree_01.prefab");
        var broad2=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/FinalWorldVegetation/island_tree_02.prefab");
        var shrub=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx");
        var placed=new List<Vector2>();uint state=0x51f15e2du;int made=0;
        for(int i=0;i<2600&&made<520;i++)
        {
            state=state*1664525u+1013904223u;float x=-244f+(state&0xffff)/65535f*488f;
            state=state*1664525u+1013904223u;float z=-244f+(state&0xffff)/65535f*488f;
            float u=x/256f,v=z/256f,f=IslandField(u,v),lake=LakeField(u,v);if(f<.17f||lake>.05f)continue;
            float sw=Mathf.Clamp01(1.04f-Mathf.Sqrt((u+.24f)*(u+.24f)*1.18f+(v+.39f)*(v+.39f)*.72f));
            float central=Mathf.Clamp01(.66f-Mathf.Abs(u+.13f)*.9f-Mathf.Abs(v+.08f)*.38f);float forest=Mathf.Max(sw,central)*Mathf.Clamp01(f*3f);
            state=state*1664525u+1013904223u;if((state&0xffff)/65535f>forest*.52f)continue;
            if(placed.Any(p=>Vector2.SqrMagnitude(p-new Vector2(x,z))<30f))continue;
            float nx=(x+256f)/512f,nz=(z+256f)/512f;if(terrain.terrainData.GetSteepness(nx,nz)>32f)continue;
            state=state*1664525u+1013904223u;int pick=(int)(state%9);
            var prefab=pick==0?shrub:(pick<4?fir:((pick&1)==0?broad1:broad2));if(!prefab)continue;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);string kind=prefab==shrub?"shrub":prefab==fir?"fir":"broad";go.name="Natural_"+kind+"_"+made;go.transform.SetParent(root.transform,true);
            go.transform.position=new Vector3(x,0,z);go.transform.rotation=Quaternion.Euler(0,(state&1023)*.3519f,0);go.transform.localScale=Vector3.one;NaturalPlantMaterials(go,kind);
            UprightGroundScale(go,terrain,kind=="shrub"?Mathf.Lerp(.8f,1.8f,(state&255)/255f):Mathf.Lerp(4.5f,9.5f,(state&255)/255f));placed.Add(new Vector2(x,z));made++;
        }
        Debug.Log("DRAGON_ISLE_FOREST natural="+made);
    }

    static void PlaceRock(GameObject prefab,Transform parent,Terrain terrain,Vector3 p,float yaw)
    {
        if(!prefab)return;var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.SetParent(parent,true);float y=terrain.SampleHeight(new Vector3(p.x,0,p.z))+terrain.transform.position.y;go.transform.position=new Vector3(p.x,y,p.z);go.transform.rotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one;
    }
    static void BuildLandmarks(Transform parent,Terrain terrain)
    {
        var root=new GameObject("Landmarks - Reference");root.transform.SetParent(parent);
        var rock=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Rock_Granite_rcCwC.fbx");
        var lake=new GameObject("Central Crater Lake");lake.transform.SetParent(root.transform);lake.transform.position=new Vector3(-18f,WaterY,36f);
        var vampire=new GameObject("Vampire Ruins - SW Reference");vampire.transform.SetParent(root.transform);
        for(int i=0;i<10;i++){float a=i*Mathf.PI*2f/10f;PlaceRock(rock,vampire.transform,terrain,new Vector3(-84f+Mathf.Cos(a)*12f,0,-126f+Mathf.Sin(a)*12f),i*36f);}
        var lair=new GameObject("Dragon Lair - Eastern Ridge");lair.transform.SetParent(root.transform);
        for(int i=0;i<7;i++){float a=i*Mathf.PI*2f/7f;PlaceRock(rock,lair.transform,terrain,new Vector3(54f+Mathf.Cos(a)*9f,0,-2f+Mathf.Sin(a)*9f),i*51f);}
        var ridge=new GameObject("Northern Mountain Spine");ridge.transform.SetParent(root.transform);ridge.transform.position=new Vector3(72f,0,150f);
    }
    static void SetupLight(Transform parent){var go=new GameObject("Sun");go.transform.SetParent(parent);go.transform.rotation=Quaternion.Euler(52,-32,0);var l=go.AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.05f;l.shadows=LightShadows.Soft;}

    static void RenderPreview(Scene scene)
    {
        Directory.CreateDirectory("Preview");var go=new GameObject("__DragonPreview");SceneManager.MoveGameObjectToScene(go,scene);var cam=go.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=285f;cam.transform.position=new Vector3(0,700,0);cam.transform.rotation=Quaternion.Euler(90,0,0);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.22f,.28f);cam.farClipPlane=1200f;
        var rt=new RenderTexture(768,768,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(768,768,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();File.WriteAllBytes("C:/MMUnityPort/Preview/DragonIsle_Reference.png",tex.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);
    }
}
