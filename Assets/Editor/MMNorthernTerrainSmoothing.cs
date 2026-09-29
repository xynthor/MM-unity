using UnityEngine;
using System;

// Gentle terrain-only smoothing of REFERENCE-TRACED north extensions.
// 512m tile boundaries and a 12m coastline buffer remain untouched.
// The coastline mask itself is NEVER blurred or changed.
public static class MMNorthernTerrainSmoothing {
    static readonly float[] Kernel={1f,6f,15f,20f,15f,6f,1f};
    static float Ease(float a,float b,float v){
        float u=Mathf.Clamp01((v-a)/(b-a));return u*u*(3f-2f*u);
    }
    public static float[,] Apply(float[,] source,MMReferenceEdgeProfiles.Mask mask) {
        int n=source.GetLength(0);
        if(n!=513||source.GetLength(1)!=513||mask==null)
            throw new ArgumentException("North reference smoothing needs a 513x513 masked terrain");
        var current=(float[,])source.Clone();
        const int margin=12;
        for(int pass=0;pass<3;pass++){
            var horizontal=new float[n,n];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                float v=0f;
                for(int k=-3;k<=3;k++)
                    v+=Kernel[k+3]*current[z,Mathf.Clamp(x+k,0,n-1)];
                horizontal[z,x]=v/64f;
            }
            var next=(float[,])current.Clone();
            for(int z=margin;z<n-margin;z++)for(int x=margin;x<n-margin;x++){
                int dist=Mathf.Min(Mathf.Min(x,n-1-x),Mathf.Min(z,n-1-z));
                float edgeWeight=Ease(margin,32f,dist);
                float signedMeters=(mask.At(x,z).r-128f)*(512f/(148f*3f));
                float coastWeight=Ease(12f,30f,Mathf.Abs(signedMeters));
                float weight=edgeWeight*coastWeight*.86f;
                if(weight<=0f)continue;
                float v=0f;
                for(int k=-3;k<=3;k++)
                    v+=Kernel[k+3]*horizontal[Mathf.Clamp(z+k,0,n-1),x];
                next[z,x]=Mathf.Lerp(current[z,x],v/64f,weight);
            }
            current=next;
        }
        return current;
    }
}
