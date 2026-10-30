using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMWorldExtensionBuilder
{
    const float SIZE=512f, TERRAIN_Y=-24f, TERRAIN_H=320f, WATER=.10f;
    const string OUT_SCENES="Assets/Scenes/";
    const string OUT_WORLD="Assets/World/WorldExtensions/Generated/";
    const string WATER_MAT="Assets/Materials/Terrain/UnifiedEnrothWater.mat";

    enum Mode { North, West, South, Archipelago }
    enum Theme { OceanCoast, SnowMountains, SnowCoast, GreenCliffs, HermitCliffs, SoutheastIslands, OpenOcean }

    sealed class Spec {
        public string id, scene, source;
        public Mode mode; public Theme theme;
        public int col,row;
    }

    static readonly string[] NorthExtensionNames={"SweetWater_North","Kriegspire_North","FrozenHighlands_North","SilverCove_North","EelInfestedWaters_North"};
    static readonly string[] SouthExtensionNames={"HermitsIsle_South","Dragonsand_South","MireOfTheDamned_South","CastleIronfist_South"};

    static readonly Spec[] SPECS={
        new Spec{id="Sweet Water North",scene="SweetWater_North",source="Assets/Scenes/Regions/SweetWater.unity",mode=Mode.North,theme=Theme.SnowMountains,col=0,row=3},
        new Spec{id="Kriegspire North",scene="Kriegspire_North",source="Assets/Scenes/Regions/Kriegspire.unity",mode=Mode.North,theme=Theme.SnowMountains,col=1,row=3},
        new Spec{id="Frozen Highlands North",scene="FrozenHighlands_North",source="Assets/Scenes/Regions/FrozenHighlands.unity",mode=Mode.North,theme=Theme.SnowCoast,col=2,row=3},
        new Spec{id="Silver Cove North",scene="SilverCove_North",source="Assets/Scenes/Regions/SilverCove.unity",mode=Mode.North,theme=Theme.SnowCoast,col=3,row=3},
        new Spec{id="Eel Infested North",scene="EelInfestedWaters_North",source="Assets/Scenes/Regions/EelInfestedWaters.unity",mode=Mode.North,theme=Theme.OpenOcean,col=4,row=3},
        new Spec{id="Paradise Valley West",scene="ParadiseValley_West",source="Assets/Scenes/Regions/ParadiseValley.unity",mode=Mode.West,theme=Theme.GreenCliffs,col=-1,row=1},
        new Spec{id="Hermits Isle West",scene="HermitsIsle_West",source="Assets/Scenes/Regions/HermitsIsle.unity",mode=Mode.West,theme=Theme.OpenOcean,col=-1,row=0},
        new Spec{id="Southwest Ocean",scene="SouthwestOcean",source="Assets/Scenes/Extensions/HermitsIsle_West.unity",mode=Mode.South,theme=Theme.OpenOcean,col=-1,row=-1},
        new Spec{id="Hermits Isle South",scene="HermitsIsle_South",source="Assets/Scenes/Regions/HermitsIsle.unity",mode=Mode.South,theme=Theme.OpenOcean,col=0,row=-1},
        new Spec{id="Dragonsand South",scene="Dragonsand_South",source="Assets/Scenes/Regions/Dragonsand.unity",mode=Mode.South,theme=Theme.OpenOcean,col=1,row=-1},
        new Spec{id="Mire of the Damned South",scene="MireOfTheDamned_South",source="Assets/Scenes/Regions/MireOfTheDamned.unity",mode=Mode.South,theme=Theme.OpenOcean,col=2,row=-1},
        new Spec{id="Castle Ironfist South",scene="CastleIronfist_South",source="Assets/Scenes/Regions/CastleIronfist.unity",mode=Mode.South,theme=Theme.OpenOcean,col=3,row=-1},
        new Spec{id="Archipelago of the Ancients",scene="ArchipelagoOfTheAncients",source="Assets/Scenes/Regions/NewSorpigal.unity",mode=Mode.Archipelago,theme=Theme.SoutheastIslands,col=4,row=-1},
    };

    sealed class RefData {
        public int hm,am,layers;
        public float[,] heights;
        public float[,,] alpha;
        public TerrainLayer[] terrainLayers;
        public DetailPrototype[] details;
    }

    static float Smooth(float a,float b,float x){
        float t=Mathf.Clamp01((x-a)/(b-a));
        return t*t*(3f-2f*t);
    }
    static float N(float x,float z,float scale,float ox,float oz){
        return Mathf.PerlinNoise((x+ox)*scale,(z+oz)*scale);
    }
    static float Gauss(float x,float z,float cx,float cz,float rx,float rz){
        float u=(x-cx)/rx,v=(z-cz)/rz;
        return Mathf.Exp(-2f*(u*u+v*v));
    }
    static float Island(float x,float z,float cx,float cz,float rx,float rz,float ang,float height,float seed){
        float c=Mathf.Cos(ang),s=Mathf.Sin(ang);
        float dx=x-cx,dz=z-cz;
        float u=(dx*c+dz*s)/rx,v=(-dx*s+dz*c)/rz;
        float a=Mathf.Atan2(v,u),r=Mathf.Sqrt(u*u+v*v);
        float warp=1f+.18f*Mathf.Sin(3f*a+seed)+.11f*Mathf.Sin(5f*a-seed*.7f)
                   +.08f*Mathf.Sin(7f*a+seed*.3f)
                   +.10f*(N(x,z,.028f,seed*37f,seed*91f)-.5f);
        float core=1f-Smooth(.56f*warp,1.08f*warp,r);
        float shelf=(1f-Smooth(.90f*warp,1.22f*warp,r))*.18f;
        return height*Mathf.Max(0f,core+shelf);
    }
    static RefData LoadReference(string scenePath){
        var sc=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
        var t=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!t)throw new Exception("Reference terrain missing: "+scenePath);
        var td=t.terrainData;
        var r=new RefData{
            hm=td.heightmapResolution,am=td.alphamapResolution,layers=td.alphamapLayers,
            heights=td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution),
            alpha=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight),
            terrainLayers=td.terrainLayers.ToArray(),
            details=td.detailPrototypes.ToArray()
        };
        return r;
    }

    static float RefHeight(RefData r,Spec s,int x,int z){
        int n=r.hm;
        if(s.mode==Mode.North){
            int nix=Mathf.Clamp(x,0,n-1);
            float nu=z/(float)(n-1); // 0 at seam/south
            float ne=r.heights[n-1,nix],ninside=r.heights[Mathf.Max(0,n-9),nix];
            return ne+(ne-ninside)*Mathf.Min(1f,nu*8f)*.30f;
        }
        if(s.mode==Mode.West){
            int wiz=Mathf.Clamp(z,0,n-1);
            float wu=1f-x/(float)(n-1); // 0 at seam/east
            float we=r.heights[wiz,0],winside=r.heights[wiz,Mathf.Min(8,n-1)];
            return we+(we-winside)*Mathf.Min(1f,wu*8f)*.30f;
        }
        // Archipelago tile is south of New Sorpigal; north edge matches source south edge.
        int aix=Mathf.Clamp(x,0,n-1);
        float au=1f-z/(float)(n-1); // 0 at north seam
        float ae=r.heights[0,aix],ainside=r.heights[Mathf.Min(8,n-1),aix];
        return ae+(ae-ainside)*Mathf.Min(1f,au*8f)*.20f;
    }

    static float WorldFromNorm(float h)=>TERRAIN_Y+h*TERRAIN_H;
    static float NormFromWorld(float y)=>Mathf.Clamp01((y-TERRAIN_Y)/TERRAIN_H);

    static float NorthWorld(Spec s,float lx,float lz,float seamY){
        if(s.theme!=Theme.OpenOcean)throw new Exception("Reference coastline mask required: "+s.scene);
        return Mathf.Lerp(seamY,-6.7f,Smooth(.004f,.11f,(lz+256f)/512f));
    }
    static float WestWorld(Spec s,float lx,float lz,float seamY){
        if(s.theme!=Theme.OpenOcean)throw new Exception("Reference coastline mask required: "+s.scene);
        return Mathf.Lerp(seamY,-6.7f,Smooth(.004f,.11f,(256f-lx)/512f));
    }

    static float SouthWorld(float lz,float seamWorld){
        // Each southern cell continues only the source's exact south border;
        // the remaining area is ocean. No invented inland geology.
        float u=(256f-lz)/512f;
        float fade=Smooth(.002f,.13f,u);
        return Mathf.Lerp(seamWorld,-6.7f,fade);
    }

    // Reference-traced island contours: world-unit polygons, ordered around each shoreline.
    // Layout matches Archipelago of the Ancients in the supplied map: four organic island
    // groups, an ochre/sandy southern-central island, and dispersed offshore islets.
    // New Sorpigal is one full 512m tile north; the northern 170m here remains open sea.
    static readonly Vector2[][] AncientIslandContours = {
        new [] { // Western island: long crooked northern finger, branching southwest arm
            new Vector2(-58,114),new Vector2(-49,104),new Vector2(-50,88),
            new Vector2(-64,72),new Vector2(-62,54),new Vector2(-73,38),
            new Vector2(-87,20),new Vector2(-109,4),new Vector2(-118,-15),
            new Vector2(-144,-26),new Vector2(-153,-42),new Vector2(-139,-49),
            new Vector2(-120,-34),new Vector2(-96,-28),new Vector2(-77,-9),
            new Vector2(-55,-11),new Vector2(-36,-22),new Vector2(-13,-19),
            new Vector2(0,-7),new Vector2(-14,8),new Vector2(-35,18),
            new Vector2(-50,41),new Vector2(-44,64),new Vector2(-58,80)
        },
        new [] { // Northern serpentine forest island; north-facing hooked spur
            new Vector2(-25,119),new Vector2(-5,123),new Vector2(10,106),
            new Vector2(31,93),new Vector2(56,90),new Vector2(75,78),
            new Vector2(108,83),new Vector2(117,72),new Vector2(96,64),
            new Vector2(76,68),new Vector2(58,56),new Vector2(49,37),
            new Vector2(64,17),new Vector2(47,8),new Vector2(31,27),
            new Vector2(11,37),new Vector2(-8,53),new Vector2(-10,79),
            new Vector2(-31,97)
        },
        new [] { // Eastern rocky crescent, tapering to a south-reaching finger
            new Vector2(103,62),new Vector2(123,72),new Vector2(143,64),
            new Vector2(158,44),new Vector2(154,25),new Vector2(139,17),
            new Vector2(128,27),new Vector2(123,7),new Vector2(112,-18),
            new Vector2(96,-28),new Vector2(74,-21),new Vector2(86,-3),
            new Vector2(82,21),new Vector2(91,44)
        },
        new [] { // Central, relatively pale/sandy island with southern curved tail
            new Vector2(47,3),new Vector2(68,13),new Vector2(88,10),
            new Vector2(112,-13),new Vector2(110,-34),new Vector2(91,-50),
            new Vector2(99,-70),new Vector2(88,-106),new Vector2(74,-91),
            new Vector2(61,-65),new Vector2(44,-75),new Vector2(26,-56),
            new Vector2(22,-34),new Vector2(36,-16)
        },
        new [] { // Southern broad wooded island, branching east into a thin cape
            new Vector2(-81,-33),new Vector2(-56,-32),new Vector2(-39,-21),
            new Vector2(-17,-32),new Vector2(10,-41),new Vector2(12,-62),
            new Vector2(28,-79),new Vector2(55,-97),new Vector2(72,-130),
            new Vector2(53,-134),new Vector2(36,-119),new Vector2(17,-112),
            new Vector2(-9,-99),new Vector2(-34,-106),new Vector2(-55,-91),
            new Vector2(-77,-87),new Vector2(-104,-89),new Vector2(-108,-75),
            new Vector2(-87,-57)
        }
    };
    static readonly Vector3[] AncientIslets = { // x,z,radius; scattered around the main group
        new Vector3(-190,133,5),new Vector3(-175,119,3),new Vector3(-151,91,5),
        new Vector3(-181,64,3),new Vector3(-169,35,4),new Vector3(-182,10,3),
        new Vector3(-196,-16,4),new Vector3(-173,-37,3),new Vector3(-183,-70,5),
        new Vector3(-153,-107,3),new Vector3(-125,-131,5),new Vector3(-148,-158,4),
        new Vector3(-89,-165,4),new Vector3(-66,-170,3),new Vector3(-35,-159,4),
        new Vector3(22,-169,4),new Vector3(57,-180,5),new Vector3(93,-148,3),
        new Vector3(151,-110,4),new Vector3(168,-57,3),new Vector3(181,-13,4),
        new Vector3(188,42,4),new Vector3(170,101,3),new Vector3(126,142,4),
        new Vector3(74,159,3),new Vector3(-95,153,3),new Vector3(-206,-133,3)
    };
    static Vector2[] SmoothClosedShore(Vector2[] poly){
        var smooth=new List<Vector2>(poly.Length*4);
        int n=poly.Length;
        for(int i=0;i<n;i++){
            Vector2 p0=poly[(i+n-1)%n],p1=poly[i],p2=poly[(i+1)%n],p3=poly[(i+2)%n];
            for(int j=0;j<4;j++){
                float t=j*.25f,t2=t*t,t3=t2*t;
                smooth.Add(.5f*((2f*p1)+(-p0+p2)*t
                    +(2f*p0-5f*p1+4f*p2-p3)*t2
                    +(-p0+3f*p1-3f*p2+p3)*t3));
            }
        }
        return smooth.ToArray();
    }
    static readonly Vector2[][] CurvedAncientShore=AncientIslandContours.Select(SmoothClosedShore).ToArray();
    // Distinct island-center offsets widen open-water channels without stretching
    // every shoreline into generic ellipses. Main group is moved 50m further west.
    static readonly Vector2[] AncientSpreading={
        new Vector2(35f,10f), new Vector2(-2f,17f), new Vector2(59f,16f),
        new Vector2(-13f,-1f), new Vector2(-8f,-26f)
    };

    static float ShoreSignedDistance(Vector2 p,Vector2[] polygon) {
        bool inside=false; float dsq=float.MaxValue;
        for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++) {
            Vector2 a=polygon[j],b=polygon[i],v=b-a;
            float t=Mathf.Clamp01(Vector2.Dot(p-a,v)/Mathf.Max(v.sqrMagnitude,.0001f));
            dsq=Mathf.Min(dsq,(p-(a+t*v)).sqrMagnitude);
            if(((a.y>p.y)!=(b.y>p.y)) &&
                p.x < (b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
        }
        return (inside?1f:-1f)*Mathf.Sqrt(dsq);
    }
    static float ArchipelagoWorld(float lx,float lz,float seamWorld){
        // Hard-lock New Sorpigal's south boundary, then descend to open sea
        // within 38m, well north of the northernmost offshore islets.
        float fromNorth=(256f-lz)/512f;
        float baseSea=Mathf.Lerp(seamWorld,-6.7f,Smooth(0f,.075f,fromNorth));
        float ax=(lx+145f)/.84f,az=lz/1.06f;
        Vector2 p=new Vector2(ax,az);
        float raised=0f;
        for(int i=0;i<AncientIslandContours.Length;i++){
            float d=ShoreSignedDistance(p-AncientSpreading[i],CurvedAncientShore[i]);
            float erosion=4.0f*(N(lx,lz,.038f,173f+i*43f,611f)-.5f)
                        +2.0f*(N(lx,lz,.092f,903f+i*29f,211f)-.5f);
            float coast=Smooth(-14f,16f,d+erosion); // ~30m submerged shelf into gentle shore
            float inner=Smooth(7f,28f,d);
            float crest=3.5f+3.5f*N(lx,lz,.025f,512f+i*77f,128f);
            // Central sandy island is lower; forested and rocky islands have local ridges.
            if(i==3)crest*=.35f;
            raised=Mathf.Max(raised,coast*(14.4f+crest*inner));
        }
        for(int i=0;i<AncientIslets.Length;i++){
            var q=AncientIslets[i];
            // Keep the far-west reference islets inside this 512m tile; never clip
            // them across the neighboring ocean-only cell.
            float ix=Mathf.Clamp(q.x*.85f-142f,-243f,245f);
            float iz=q.y*1.06f;
            float dx=(lx-ix)/q.z,dz=(lz-iz)/q.z;
            float a=Mathf.Atan2(dz,dx),r=Mathf.Sqrt(dx*dx+dz*dz);
            float wav=1f+.17f*Mathf.Sin(a*3.0f+i*1.7f)+.10f*Mathf.Sin(a*5f-i*.6f);
            float d=(wav-r)*q.z;
            raised=Mathf.Max(raised,Smooth(-3f,3f,d)*13.5f);
        }
        // Never turn a separate island into a land bridge across the west tile
        // boundary; the adjacent Castle Ironfist South square remains ocean.
        raised*=Smooth(-255f,-241f,lx);
        return baseSea+raised;
    }
    static float[,] BuildHeights(RefData r,Spec s,MMReferenceEdgeProfiles.Mask map,out float seamMaxError,out float landPct){
        int n=513;var h=new float[n,n];int land=0;
        seamMaxError=0f;
        for(int z=0;z<n;z++)for(int x=0;x<n;x++){
            float lx=-256f+x*(512f/(n-1)),lz=-256f+z*(512f/(n-1));
            float refNorm=RefHeight(r,s,x,z), seamWorld=WorldFromNorm(refNorm);
            float y=map!=null?MMReferenceEdgeProfiles.Height(map,x,z,seamWorld,s.col,s.row,s.mode==Mode.North):
                    s.mode==Mode.North?NorthWorld(s,lx,lz,seamWorld):
                    s.mode==Mode.West?WestWorld(s,lx,lz,seamWorld):
                    s.mode==Mode.South?SouthWorld(lz,seamWorld):
                    ArchipelagoWorld(lx,lz,seamWorld);
            h[z,x]=NormFromWorld(y);
            if(y>WATER+.45f)land++;
        }
        // Hard-lock the exact shared seam to eliminate cracks.
        if(s.mode==Mode.North){
            for(int x=0;x<n;x++){
                float e=r.heights[r.hm-1,Mathf.RoundToInt(x*(r.hm-1f)/(n-1f))];
                seamMaxError=Mathf.Max(seamMaxError,Mathf.Abs(h[0,x]-e)*TERRAIN_H);h[0,x]=e;
            }
        } else if(s.mode==Mode.West){
            for(int z=0;z<n;z++){
                float e=r.heights[Mathf.RoundToInt(z*(r.hm-1f)/(n-1f)),0];
                seamMaxError=Mathf.Max(seamMaxError,Mathf.Abs(h[z,n-1]-e)*TERRAIN_H);h[z,n-1]=e;
            }
        } else {
            for(int x=0;x<n;x++){
                float e=r.heights[0,Mathf.RoundToInt(x*(r.hm-1f)/(n-1f))];
                seamMaxError=Mathf.Max(seamMaxError,Mathf.Abs(h[n-1,x]-e)*TERRAIN_H);h[n-1,x]=e;
            }
        }
        landPct=land*100f/(n*n);
        return h;
    }

    static int Layer(TerrainLayer[] a,string exact,string contains){
        int i=Array.FindIndex(a,l=>l&&l.name.Equals(exact,StringComparison.OrdinalIgnoreCase));
        if(i>=0)return i;
        return Array.FindIndex(a,l=>l&&l.name.IndexOf(contains,StringComparison.OrdinalIgnoreCase)>=0);
    }

    static float Slope(float[,] h,int x,int z){
        int n=h.GetLength(0);
        float dx=(h[z,Mathf.Min(n-1,x+1)]-h[z,Mathf.Max(0,x-1)])*TERRAIN_H*.5f;
        float dz=(h[Mathf.Min(n-1,z+1),x]-h[Mathf.Max(0,z-1),x])*TERRAIN_H*.5f;
        return Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg;
    }

    static float[,,] BuildAlpha(RefData r,Spec s,float[,] hm,MMReferenceEdgeProfiles.Mask map){
        int a=512,L=r.terrainLayers.Length;var outa=new float[a,a,L];
        int green=Layer(r.terrainLayers,"Realistic_Green","Green");
        int light=Layer(r.terrainLayers,"Realistic_LightGreen","LightGreen");
        int sand=Layer(r.terrainLayers,"Realistic_Desert","Desert");
        int volc=Layer(r.terrainLayers,"Realistic_Volcanic","Volcanic");
        int snow=Layer(r.terrainLayers,"Realistic_Snow","Snow");
        int arid=Layer(r.terrainLayers,"Realistic_Arid","Arid");
        int stone=Layer(r.terrainLayers,"Realistic_RoadOverlay","Road");
        if(green<0||sand<0||volc<0||arid<0)throw new Exception("Expected edge terrain layers missing in "+s.source);

        for(int z=0;z<a;z++)for(int x=0;x<a;x++){
            int hx=Mathf.RoundToInt(x*512f/(a-1)),hz=Mathf.RoundToInt(z*512f/(a-1));
            float y=WorldFromNorm(hm[hz,hx]),sl=Slope(hm,hx,hz);
            float lx=-256f+(x+.5f)*512f/a,lz=-256f+(z+.5f)*512f/a;
            float beach=(1f-Smooth(.4f,5.5f,y))*(1f-Smooth(10f,27f,sl));
            float steep=Smooth(17f,40f,sl);
            float hi=Smooth(18f,52f,y);
            float g=.38f*(1f-steep)*(1f-beach);
            float lg=.25f*(1f-steep)*(1f-beach);
            float sd=beach*.95f+.03f;
            float vo=.10f+.77f*steep;
            float sn=0f,ar=.18f*(1f-steep),st=.03f*steep;
            if(s.theme==Theme.SnowMountains||s.theme==Theme.SnowCoast){
                sn=hi*(.45f+.48f*(1f-steep));g*=1f-.75f*hi;lg*=1f-.65f*hi;ar*=.35f;
                vo+=.28f*steep;
            } else if(s.theme==Theme.HermitCliffs){
                vo+=.25f*steep+.15f*hi;ar+=.14f*(1f-steep);g*=.72f;
            } else if(s.theme==Theme.SoutheastIslands){
                // Source image: wooded western/northern/southern islands, pale central island.
                float pale=Gauss((lx+145f)/.84f+13f,lz/1.06f+1f,47f,-41f,43f,45f);
                g*=2.8f*(1f-.88f*pale); lg*=1.6f*(1f-.66f*pale);
                sd+=1.00f*pale+1.6f*(1f-Smooth(.9f,8f,y))*(1f-.5f*steep);
                ar*=.66f;vo*=.58f;
            } else if(s.theme==Theme.OpenOcean){
                g=lg=vo=sn=ar=st=0f;sd=1f;
            } else if(s.theme==Theme.OceanCoast){
                g*=.75f;lg*=.65f;ar*=.75f;
            }

            if(map!=null)MMReferenceEdgeProfiles.Biome(map,hx,hz,s.mode==Mode.North,sl,
                ref g,ref lg,ref sd,ref vo,ref sn,ref ar);
            var wt=new float[L];
            wt[green]=Mathf.Max(0,g); if(light>=0)wt[light]=Mathf.Max(0,lg);
            wt[sand]=Mathf.Max(0,sd); wt[volc]=Mathf.Max(0,vo);
            if(snow>=0)wt[snow]=Mathf.Max(0,sn); wt[arid]=Mathf.Max(0,ar);
            if(stone>=0)wt[stone]=Mathf.Max(0,st);
            float sum=wt.Sum()+.0001f;for(int k=0;k<L;k++)wt[k]/=sum;

            float seam=0f;int sx=0,sz=0;
            if(s.mode==Mode.North){seam=1f-Smooth(0f,.035f,(z+.5f)/a);sx=x;sz=r.am-1;}
            else if(s.mode==Mode.West){seam=1f-Smooth(0f,.14f,1f-(x+.5f)/a);sx=0;sz=z;}
            else {seam=1f-Smooth(0f,.065f,1f-(z+.5f)/a);sx=x;sz=0;}
            if(seam>0){
                int rx=Mathf.Clamp(Mathf.RoundToInt(sx*(r.am-1f)/(a-1f)),0,r.am-1);
                int rz=Mathf.Clamp(Mathf.RoundToInt(sz*(r.am-1f)/(a-1f)),0,r.am-1);
                for(int k=0;k<L;k++)wt[k]=Mathf.Lerp(wt[k],r.alpha[rz,rx,k],seam);
                sum=wt.Sum()+.0001f;for(int k=0;k<L;k++)wt[k]/=sum;
            }
            for(int k=0;k<L;k++)outa[z,x,k]=wt[k];
        }
        return outa;
    }
    static GameObject CreateWater(Transform parent){
        var go=GameObject.CreatePrimitive(PrimitiveType.Plane);go.name="Internal Water - Smooth";
        go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,WATER,0);
        go.transform.localScale=new Vector3(51.2f,1,51.2f);
        var col=go.GetComponent<Collider>();if(col)UnityEngine.Object.DestroyImmediate(col);
        var mat=AssetDatabase.LoadAssetAtPath<Material>(WATER_MAT);if(!mat)throw new Exception("Unified water missing");
        go.GetComponent<Renderer>().sharedMaterial=mat;return go;
    }

    static void BuildOne(Spec s,List<string> report){
        var r=LoadReference(s.source);
        if(r.hm<257||r.am<256)throw new Exception("Reference resolution too low: "+s.source);
        Directory.CreateDirectory(OUT_WORLD);
        string assetPath=OUT_WORLD+s.scene+"Terrain.asset";
        // Preserve existing TerrainData GUID: linked-world clones share this exact asset.
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(assetPath);
        if(!td){
            td=new TerrainData();td.name=s.scene+"Terrain";td.heightmapResolution=513;
            td.size=new Vector3(SIZE,TERRAIN_H,SIZE);td.alphamapResolution=512;td.baseMapResolution=512;
            td.terrainLayers=r.terrainLayers;AssetDatabase.CreateAsset(td,assetPath);
        } else {
            if(td.heightmapResolution!=513||td.alphamapResolution!=512)
                throw new Exception("Unexpected existing TerrainData resolution: "+assetPath);
            td.terrainLayers=r.terrainLayers;
        }

        var map=MMReferenceEdgeProfiles.Load(s.scene);
        var hm=BuildHeights(r,s,map,out float seamError,out float landPct);
        if(s.mode==Mode.North&&map!=null)
            hm=MMNorthernTerrainSmoothing.Apply(hm,map);
        td.SetHeights(0,0,hm);
        var am=BuildAlpha(r,s,hm,map);td.SetAlphamaps(0,0,am);
        if(r.details.Length>0)td.detailPrototypes=r.details;
        EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();

        var sc=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        sc.name=s.scene;
        var root=new GameObject(s.id+" - Open World Extension");
        var tg=Terrain.CreateTerrainGameObject(td);tg.name="Terrain_"+s.scene;tg.transform.SetParent(root.transform,false);
        tg.transform.localPosition=new Vector3(-256f,TERRAIN_Y,-256f);
        var terrain=tg.GetComponent<Terrain>();terrain.drawInstanced=true;terrain.heightmapPixelError=5;terrain.basemapDistance=1000;
        CreateWater(root.transform);

        var sun=new GameObject("Sun");sun.transform.SetParent(root.transform,false);
        var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.95f;
        sun.transform.rotation=Quaternion.Euler(52f,328f,0f);

        string scenePath=OUT_SCENES+s.scene+".unity";
        EditorSceneManager.MarkSceneDirty(sc);
        if(!EditorSceneManager.SaveScene(sc,scenePath))throw new IOException("Could not save "+scenePath);
        MMStandaloneRegionRig.EnsureScene(scenePath);
        report.Add($"{s.scene},{s.col},{s.row},{s.mode},{s.theme},{landPct:F1},{seamError:F4},{assetPath}");
    }

    static TerrainData EdgeData(string scene){
        return AssetDatabase.LoadAssetAtPath<TerrainData>(OUT_WORLD+scene+"Terrain.asset");
    }

    static void StitchVertical(string leftScene,string rightScene,List<string> report,int band=64){
        var a=EdgeData(leftScene);var b=EdgeData(rightScene);if(!a||!b)throw new Exception("Missing edge terrain for stitch");
        int n=a.heightmapResolution;
        var ah=a.GetHeights(0,0,n,n);var bh=b.GetHeights(0,0,n,n);
        float before=0,derivAfter=0;
        for(int z=0;z<n;z++){
            before=Mathf.Max(before,Mathf.Abs(ah[z,n-1]-bh[z,0])*TERRAIN_H);
            float lv=ah[z,n-1-band],rv=bh[z,band];
            for(int q=0;q<=band*2;q++){
                float t=q/(float)(band*2),u=t*t*(3f-2f*t),v=Mathf.Lerp(lv,rv,u);
                if(q<=band)ah[z,n-1-band+q]=v;
                if(q>=band)bh[z,q-band]=v;
            }
            derivAfter=Mathf.Max(derivAfter,Mathf.Abs((ah[z,n-1]-ah[z,n-2])-(bh[z,1]-bh[z,0]))*TERRAIN_H);
        }
        a.SetHeights(0,0,ah);b.SetHeights(0,0,bh);
        int m=a.alphamapResolution,L=Mathf.Min(a.alphamapLayers,b.alphamapLayers),ab=Mathf.Min(48,band);
        var aa=a.GetAlphamaps(0,0,m,m);var ba=b.GetAlphamaps(0,0,m,m);
        for(int z=0;z<m;z++){
            var l=new float[L];var r=new float[L];
            for(int k=0;k<L;k++){l[k]=aa[z,m-1-ab,k];r[k]=ba[z,ab,k];}
            for(int q=0;q<=ab*2;q++){
                float t=q/(float)(ab*2),u=t*t*(3f-2f*t),sum=0;
                var v=new float[L];for(int k=0;k<L;k++){v[k]=Mathf.Lerp(l[k],r[k],u);sum+=v[k];}
                if(sum<=0)sum=1;
                for(int k=0;k<L;k++){v[k]/=sum;if(q<=ab)aa[z,m-1-ab+q,k]=v[k];if(q>=ab)ba[z,q-ab,k]=v[k];}
            }
        }
        a.SetAlphamaps(0,0,aa);b.SetAlphamaps(0,0,ba);EditorUtility.SetDirty(a);EditorUtility.SetDirty(b);
        report.Add($"stitch_vertical,{leftScene},{rightScene},before_m={before:F4},band_m={band},derivative_jump_after_m={derivAfter:F4}");
    }

    static void StitchHorizontal(string southScene,string northScene,List<string> report,int band=64){
        var a=EdgeData(southScene);var b=EdgeData(northScene);if(!a||!b)throw new Exception("Missing edge terrain for stitch");
        int n=a.heightmapResolution;
        var ah=a.GetHeights(0,0,n,n);var bh=b.GetHeights(0,0,n,n);
        float before=0,derivAfter=0;
        for(int x=0;x<n;x++){
            before=Mathf.Max(before,Mathf.Abs(ah[n-1,x]-bh[0,x])*TERRAIN_H);
            float sv=ah[n-1-band,x],nv=bh[band,x];
            for(int q=0;q<=band*2;q++){
                float t=q/(float)(band*2),u=t*t*(3f-2f*t),v=Mathf.Lerp(sv,nv,u);
                if(q<=band)ah[n-1-band+q,x]=v;
                if(q>=band)bh[q-band,x]=v;
            }
            derivAfter=Mathf.Max(derivAfter,Mathf.Abs((ah[n-1,x]-ah[n-2,x])-(bh[1,x]-bh[0,x]))*TERRAIN_H);
        }
        a.SetHeights(0,0,ah);b.SetHeights(0,0,bh);
        int m=a.alphamapResolution,L=Mathf.Min(a.alphamapLayers,b.alphamapLayers),ab=Mathf.Min(48,band);
        var aa=a.GetAlphamaps(0,0,m,m);var ba=b.GetAlphamaps(0,0,m,m);
        for(int x=0;x<m;x++){
            var s0=new float[L];var n0=new float[L];
            for(int k=0;k<L;k++){s0[k]=aa[m-1-ab,x,k];n0[k]=ba[ab,x,k];}
            for(int q=0;q<=ab*2;q++){
                float t=q/(float)(ab*2),u=t*t*(3f-2f*t),sum=0;
                var v=new float[L];for(int k=0;k<L;k++){v[k]=Mathf.Lerp(s0[k],n0[k],u);sum+=v[k];}
                if(sum<=0)sum=1;
                for(int k=0;k<L;k++){v[k]/=sum;if(q<=ab)aa[m-1-ab+q,x,k]=v[k];if(q>=ab)ba[q-ab,x,k]=v[k];}
            }
        }
        a.SetAlphamaps(0,0,aa);b.SetAlphamaps(0,0,ba);EditorUtility.SetDirty(a);EditorUtility.SetDirty(b);
        report.Add($"stitch_horizontal,{southScene},{northScene},before_m={before:F4},band_m={band},derivative_jump_after_m={derivAfter:F4}");
    }

    static void StitchEdgeTiles(List<string> report){
        for(int i=0;i<4;i++)StitchVertical(NorthExtensionNames[i],NorthExtensionNames[i+1],report,8);
        StitchHorizontal("HermitsIsle_West","ParadiseValley_West",report,24);
        for(int i=0;i<3;i++)StitchVertical(SouthExtensionNames[i],SouthExtensionNames[i+1],report,8);
        StitchVertical("CastleIronfist_South","ArchipelagoOfTheAncients",report,8);
        StitchVertical("SouthwestOcean","HermitsIsle_South",report,8);
        StitchHorizontal("SouthwestOcean","HermitsIsle_West",report,8);
        MMReferenceNorthSeamLock.Apply(report);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("MMUnity/World/Build Southwest Ocean Only")]
    public static void BuildOceanCornersOnly(){
        Directory.CreateDirectory("Validation/EdgeGrid20260923");
        var report=new List<string>{"scene,col,row,mode,theme,land_pct,seam_error_before_lock_m,terrain_asset"};
        BuildOne(SPECS.First(s=>s.scene=="SouthwestOcean"),report);
        StitchVertical("SouthwestOcean","HermitsIsle_South",report,8);
        StitchHorizontal("SouthwestOcean","HermitsIsle_West",report,8);
        File.WriteAllLines("Validation/EdgeGrid20260923/southwest_ocean.csv",report);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("ENROTH_SW_OCEAN_DONE count=1");
    }

    [MenuItem("MMUnity/World/Rebuild Southeast Archipelago From Reference")]
    public static void BuildSoutheastOnly(){
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var report=new List<string>{"scene,col,row,mode,theme,land_pct,seam_error_before_lock_m,terrain_asset"};
        BuildOne(SPECS.Last(),report);
        StitchVertical("CastleIronfist_South","ArchipelagoOfTheAncients",report,8);
        MMWorldExtensionSeamSmoother.ApplyAll();
        File.WriteAllLines("Validation/CactusDragonAudit/archipelago_reference_rebuild.csv",report);
        AssetDatabase.SaveAssets();
        Debug.Log("ARCHIPELAGO_REFERENCE_REBUILD_DONE");
    }

    [MenuItem("MMUnity/World/Relock Southeast Archipelago Seam Only")]
    public static void RelockSoutheastOnly(){
        Directory.CreateDirectory("Validation/EdgeGrid20260923");
        var report=new List<string>();
        StitchVertical("CastleIronfist_South","ArchipelagoOfTheAncients",report,8);
        File.WriteAllLines("Validation/EdgeGrid20260923/archipelago_seam_relock.csv",report);
        AssetDatabase.SaveAssets();
        Debug.Log("ARCHIPELAGO_SOUTH3_SEAM_RELOCK_DONE");
    }

    [MenuItem("MMUnity/World/Rebuild Silver Cove North Only")]
    public static void BuildSilverNorthOnly(){
        Directory.CreateDirectory("Validation/EdgeGrid20260923");
        var report=new List<string>{"scene,col,row,mode,theme,land_pct,seam_error_before_lock_m,terrain_asset"};
        BuildOne(SPECS.Single(s=>s.scene=="SilverCove_North"),report);
        StitchVertical("FrozenHighlands_North","SilverCove_North",report,8);
        StitchVertical("SilverCove_North","EelInfestedWaters_North",report,8);
        MMReferenceNorthSeamLock.Apply(report);
        MMReferenceTiledGround.ApplyOne("SilverCove_North");
        MMWorldExtensionSeamSmoother.ApplyAll();
        File.WriteAllLines("Validation/EdgeGrid20260923/silver_north_reference_rebuild.csv",report);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("SILVER_NORTH_REFERENCE_DONE");
    }

    [MenuItem("MMUnity/World/Rebuild Paradise Valley West Coast Only")]
    public static void BuildParadiseWestOnly(){
        Directory.CreateDirectory("Validation/EdgeGrid20260923");
        var report=new List<string>{"scene,col,row,mode,theme,land_pct,seam_error_before_lock_m,terrain_asset"};
        BuildOne(SPECS.Single(s=>s.scene=="ParadiseValley_West"),report);
        StitchHorizontal("HermitsIsle_West","ParadiseValley_West",report,10);
        MMReferenceTiledGround.ApplyOne("ParadiseValley_West");
        MMWorldExtensionSeamSmoother.ApplyAll();
        File.WriteAllLines("Validation/EdgeGrid20260923/visible_paradise_west_rebuild.csv",report);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("PARADISE_VISIBLE_WEST_COAST_REBUILD_DONE");
    }

    [MenuItem("MMUnity/World/Repaint North With Tiled Terrain Layers")]
    public static void RepaintNorthTiledDetail(){
        Directory.CreateDirectory("Validation/EdgeGrid20260923");
        var report=new List<string>();
        foreach(var s in SPECS.Where(s=>s.mode==Mode.North&&s.scene!="EelInfestedWaters_North")){
            var source=LoadReference(s.source);
            var path=OUT_WORLD+s.scene+"Terrain.asset";
            var td=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if(!td)throw new Exception("Existing northern terrain missing: "+path);
            var mask=MMReferenceEdgeProfiles.Load(s.scene);
            var heights=td.GetHeights(0,0,513,513);
            // Rebuild biome weights from the reference mask, then restore detailed tiled ground.
            td.terrainLayers=source.terrainLayers;
            td.SetAlphamaps(0,0,BuildAlpha(source,s,heights,mask));
            EditorUtility.SetDirty(td);
            report.Add(s.scene+",mask_snow_rock_splat=YES,heights_unchanged=YES");
        }
        AssetDatabase.SaveAssets();
            foreach(var n in NorthExtensionNames.Take(4))MMReferenceTiledGround.ApplyOne(n);
        File.WriteAllLines("Validation/EdgeGrid20260923/north_photo_texture_repaint.csv",report);
        Debug.Log("NORTH_TILED_MASK_DETAIL_REPAINTED count="+report.Count);
    }

    [MenuItem("MMUnity/World/Rebuild Smoothed North and Western Coast")]
    public static void BuildWestNorthOnly(){
        Directory.CreateDirectory("Validation/EdgeGrid20260923");
        var report=new List<string>{"scene,col,row,mode,theme,land_pct,seam_error_before_lock_m,terrain_asset"};
        foreach(var s in SPECS.Where(s=>(s.mode==Mode.North&&s.theme!=Theme.OpenOcean)||s.scene=="ParadiseValley_West"))
            BuildOne(s,report);
        for(int i=0;i<4;i++)
            StitchVertical(NorthExtensionNames[i],NorthExtensionNames[i+1],report,8);
        StitchHorizontal("HermitsIsle_West","ParadiseValley_West",report,10);
        MMReferenceNorthSeamLock.Apply(report);
        foreach(var n in NorthExtensionNames.Take(4))MMReferenceTiledGround.ApplyOne(n);
        MMReferenceTiledGround.ApplyOne("ParadiseValley_West");
        MMWorldExtensionSeamSmoother.ApplyAll();
        File.WriteAllLines("Validation/EdgeGrid20260923/west_north_smooth_rebuild.csv",report);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("WEST_NORTH_SMOOTH_REFERENCE_REBUILD_DONE count=5");
    }

    [MenuItem("MMUnity/World/Build World Extension Tiles")]
    public static void Build(){
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var report=new List<string>{"scene,col,row,mode,theme,land_pct,seam_error_before_lock_m,terrain_asset"};
        foreach(var s in SPECS)BuildOne(s,report);
        StitchEdgeTiles(report);
        foreach(var n in NorthExtensionNames.Take(4))MMReferenceTiledGround.ApplyOne(n);
        MMReferenceTiledGround.ApplyOne("ParadiseValley_West");
        MMWorldExtensionSeamSmoother.ApplyAll();
        File.WriteAllLines("Validation/CactusDragonAudit/world_extension_tiles.csv",report);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("WORLD_EXTENSION_TILES_DONE count="+SPECS.Length);
    }
}
