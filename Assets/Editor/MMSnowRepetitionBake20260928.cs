using UnityEngine;
using UnityEditor;
using System.IO;

public static class MMSnowRepetitionBake20260928 {
 const string Root="Assets/World/WorldExtensions/Generated/LinkedSnow02/";
 static Texture2D Raw(string name){var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);t.LoadImage(File.ReadAllBytes(Root+"snow_02_"+name+"_2k.jpg"));t.wrapMode=TextureWrapMode.Repeat;return t;}
 static int Hash(int x,int y){unchecked{uint h=(uint)((x&3)*374761393+(y&3)*668265263+137);h=(h^(h>>13))*1274126177;return (int)(h^(h>>16))&0x7fffffff;}}
 static Color Sample(Color32[] p,int n,float x,float y){x=(x-Mathf.Floor(x))*n-.5f;y=(y-Mathf.Floor(y))*n-.5f;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);float fx=x-ix,fy=y-iy;return Color.Lerp(Color.Lerp(p[((iy+n)%n)*n+(ix+n)%n],p[((iy+n)%n)*n+(ix+1+n)%n],fx),Color.Lerp(p[((iy+1+n)%n)*n+(ix+n)%n],p[((iy+1+n)%n)*n+(ix+1+n)%n],fx),fy);}
 public static void Bake(){
  if(File.Exists(Root+"Snow02_SeamlessAlbedoSmoothness.png")&&File.Exists(Root+"Snow02_SeamlessNormal.png"))return;
  var d=Raw("diff");var n=Raw("nor_gl");var r=Raw("rough");var dp=d.GetPixels32();var np=n.GetPixels32();var rp=r.GetPixels32();int size=2048,src=d.width;
  var colour=new Color32[size*size];var normals=new Color32[size*size];
  // Four periodic cells across an 8m atlas. Each physical 2m scan remains at
  // its source scale; smooth neighbouring-cell blends suppress repeated marks.
  for(int y=0;y<size;y++)for(int x=0;x<size;x++){
   float u=x/(float)size*4,v=y/(float)size*4;int cx=Mathf.FloorToInt(u),cy=Mathf.FloorToInt(v);float fx=Mathf.SmoothStep(0,1,u-cx),fy=Mathf.SmoothStep(0,1,v-cy);Color col=Color.clear;Vector3 norm=Vector3.zero;float rough=0;
   for(int j=0;j<2;j++)for(int i=0;i<2;i++){
    int h=Hash(cx+i,cy+j),rot=h&3;float a=u,b=v;
    switch(rot){case 1:a=-v;b=u;break;case 2:a=-u;b=-v;break;case 3:a=v;b=-u;break;}
    a+=((h>>2)&255)/255f;b+=((h>>10)&255)/255f;float w=(i==0?1-fx:fx)*(j==0?1-fy:fy);
    col+=Sample(dp,src,a,b)*w;rough+=Sample(rp,src,a,b).r*w;
    Color nc=Sample(np,src,a,b);Vector3 nv=new Vector3(nc.r*2-1,nc.g*2-1,nc.b*2-1);
    switch(rot){case 1:nv=new Vector3(nv.y,-nv.x,nv.z);break;case 2:nv=new Vector3(-nv.x,-nv.y,nv.z);break;case 3:nv=new Vector3(-nv.y,nv.x,nv.z);break;}norm+=nv*w;
   }
   col.a=1-rough;colour[y*size+x]=col;norm.Normalize();normals[y*size+x]=new Color(norm.x*.5f+.5f,norm.y*.5f+.5f,norm.z*.5f+.5f,1);
  }
  var output=new Texture2D(size,size,TextureFormat.RGBA32,false,true);output.SetPixels32(colour);output.Apply();File.WriteAllBytes(Root+"Snow02_SeamlessAlbedoSmoothness.png",output.EncodeToPNG());output.SetPixels32(normals);output.Apply();File.WriteAllBytes(Root+"Snow02_SeamlessNormal.png",output.EncodeToPNG());
  Object.DestroyImmediate(output);Object.DestroyImmediate(d);Object.DestroyImmediate(n);Object.DestroyImmediate(r);
 }
}
