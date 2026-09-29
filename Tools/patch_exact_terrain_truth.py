from pathlib import Path
import shutil
p=Path(r"C:\MMUnityPort\Assets\Editor\MMWitcherTerrainPass.cs")
s=p.read_text(encoding="utf-8-sig")
shutil.copy2(p,Path(r"C:\MMUnityPort\Backups\MMWitcherTerrainPass_pre_exact_truth_20260918.cs"))
a=s.index("    static bool DesertZone")
b=s.index("    [MenuItem",a)
new=r'''    static int CanonicalLand(string zone,byte g)
    {
        switch(zone)
        {
            case "NewSorpigal":
            case "CastleIronfist":
            case "MistyIslands":
            case "BootlegBay":
            case "EelInfestedWaters":
            case "SilverCove":
                return Green;
            case "MireOfTheDamned":
                return g==7?Marsh:Mud;
            case "Dragonsand":
                return g==0?Green:Sand;
            case "HermitsIsle":
                return Sand;
            case "ParadiseValley":
                return g==2?Sand:DryAsh;
            case "SweetWater":
                return DryAsh;
            case "Blackshire":
            case "FreeHaven":
                return Mud;
            case "FrozenHighlands":
            case "Kriegspire":
                return g==1?Snow:Mud;
        }
        if(g==0)return Green;if(g==1)return Snow;if(g==2)return Sand;if(g==3)return Volcanic;
        if(g==4)return Mud;if(g==6)return DryAsh;if(g==7)return Marsh;return Green;
    }

    static int ShoreClass(string zone)
    {
        if(zone=="MireOfTheDamned"||zone=="Blackshire"||zone=="FreeHaven")return Mud;
        if(zone=="SweetWater"||zone=="FrozenHighlands"||zone=="Kriegspire")return Rock;
        return Sand;
    }

    public static int ExpectedLayerForCell(string zone,byte[] tile,byte[] grp,byte[] sem,int sx,int sy)
    {
        sx=Mathf.Clamp(sx,0,N-1);sy=Mathf.Clamp(sy,0,N-1);
        byte raw=tile[sy*N+sx],f=sem[raw],g=grp[raw];
        if((f&8)!=0||(g>=8&&g<255))return Road;
        if((f&1)!=0)return Rock;
        if((f&2)!=0||g==5)return ShoreClass(zone);
        return CanonicalLand(zone,g);
    }
'''
s=s[:a]+new+s[b:]
a=s.index("    static void Add(")
b=s.index("    static int Apply(",a)
new2=r'''    static void PaintTruth(float[,,] a,int y,int x,int expected,float steep,float noise)
    {
        int l=a.GetLength(2);
        for(int k=0;k<l;k++)a[y,x,k]=0f;
        if(expected==Road){a[y,x,Road]=1f;return;}
        if(expected==Rock){a[y,x,Rock]=.76f;a[y,x,Sand]=.24f;return;}
        float rock=Mathf.Clamp01(Mathf.InverseLerp(38f,68f,steep))*.12f;
        float secondary=.025f+.035f*noise;
        a[y,x,expected]=1f-rock-secondary;
        a[y,x,Rock]+=rock;
        if(expected==Green)a[y,x,Mud]+=secondary;
        else if(expected==Mud)a[y,x,Green]+=secondary;
        else if(expected==Marsh)a[y,x,Mud]+=secondary;
        else if(expected==Snow)a[y,x,Rock]+=secondary;
        else if(expected==Sand)a[y,x,Rock]+=secondary;
        else if(expected==DryAsh)a[y,x,Rock]+=secondary;
        else if(expected==Volcanic)a[y,x,Rock]+=secondary;
        else a[y,x,expected]+=secondary;
    }

'''
s=s[:a]+new2+s[b:]
# replace Apply loop internals wholesale
old=r'''        int r=td.alphamapResolution,l=layers.Length;var a=new float[r,r,l];
        for(int y=0;y<r;y++)for(int x=0;x<r;x++)
        {
            float sx=(x+.5f)/r*N-.5f,sy=N-1-((y+.5f)/r*N-.5f);int x0=Mathf.FloorToInt(sx),y0=Mathf.FloorToInt(sy);
            float tx=sx-x0,ty=sy-y0;var w=new float[l];
            SourceWeights(z.key,tile,grp,sem,x0,y0,(1-tx)*(1-ty),w);
            SourceWeights(z.key,tile,grp,sem,x0+1,y0,tx*(1-ty),w);
            SourceWeights(z.key,tile,grp,sem,x0,y0+1,(1-tx)*ty,w);
            SourceWeights(z.key,tile,grp,sem,x0+1,y0+1,tx*ty,w);
            int cx=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1),cy=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1);
            float steep=td.GetSteepness(x/(float)Mathf.Max(1,r-1),y/(float)Mathf.Max(1,r-1));
            Organicize(z.key,w,tile,grp,sem,cx,cy,steep);
            for(int k=0;k<l;k++)a[y,x,k]=w[k];
        }'''
new3=r'''        int r=td.alphamapResolution,l=layers.Length;var a=new float[r,r,l];
        for(int y=0;y<r;y++)for(int x=0;x<r;x++)
        {
            int sx=Mathf.Clamp(Mathf.FloorToInt((x+.5f)/r*N),0,N-1);
            int sy=Mathf.Clamp(N-1-Mathf.FloorToInt((y+.5f)/r*N),0,N-1);
            int expected=ExpectedLayerForCell(z.key,tile,grp,sem,sx,sy);
            float steep=td.GetSteepness(x/(float)Mathf.Max(1,r-1),y/(float)Mathf.Max(1,r-1));
            float noise=Mathf.PerlinNoise((sx+17)*.13f,(sy+31)*.13f);
            PaintTruth(a,y,x,expected,steep,noise);
        }'''
if old not in s: raise SystemExit("apply loop needle missing")
s=s.replace(old,new3,1)
p.write_text(s,encoding="utf-8")
print("WITCHER_TERRAIN_EXACT_TRUTH_PATCHED")