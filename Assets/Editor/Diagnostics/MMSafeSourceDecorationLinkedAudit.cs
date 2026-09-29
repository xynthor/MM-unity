using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMSafeSourceDecorationLinkedAudit
{
    static readonly string[] Keys={"SweetWater","Kriegspire","FrozenHighlands","SilverCove","EelInfestedWaters","ParadiseValley","Blackshire","FreeHaven","BootlegBay","MistyIslands","HermitsIsle","Dragonsand","MireOfTheDamned","CastleIronfist","NewSorpigal"};
    static readonly string[] Roots={"Sweet Water - LINKED REFERENCE","Kriegspire - LINKED REFERENCE","White Cap / Frozen Highlands - LINKED REFERENCE","Silver Cove - LINKED REFERENCE","Eel Infested Waters - LINKED REFERENCE","Paradise Valley - LINKED REFERENCE","Blackshire - LINKED REFERENCE","Free Haven - LINKED REFERENCE","Bootleg Bay - LINKED REFERENCE","Misty Islands - LINKED REFERENCE","Hermits Isle - LINKED REFERENCE","Dragonsand - LINKED REFERENCE","Mire of the Damned - LINKED REFERENCE","Castle Ironfist - LINKED ADJUSTABLE","New Sorpigal - LINKED REFERENCE"};
    static readonly int[] Expected={36,225,244,93,38,143,158,23,4,12,203,290,90,73,38};

    [MenuItem("MMUnity/Source Decorations 3D SAFE/Audit Linked Enroth")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/SourceDecoration3D/Production");
        var sc=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
        var world=sc.GetRootGameObjects().FirstOrDefault(g=>g.name=="ENROTH - LINKED SOURCE REGIONS");
        if(!world)throw new System.Exception("WORLD_ROOT_MISSING");
        var preview=world.transform.Find("EDITOR LAYOUT - 15 MM6 REGIONS + DRAGON ISLE");
        if(!preview)throw new System.Exception("PREVIEW_ROOT_MISSING");

        var rows=new List<string>{"zone,expected3d,linked3d,billboards,mm6_quad_meshes,source_sprite_roots,source3d_roots,error_shader_renderers,status"};
        int total3d=0,totalBB=0,totalQuad=0,totalOld=0,totalErr=0,fail=0,pass=0;
        for(int i=0;i<Keys.Length;i++)
        {
            var r=preview.Find(Roots[i]);
            if(!r){rows.Add($"{Keys[i]},{Expected[i]},0,999,999,999,0,999,FAIL_ROOT_MISSING");fail++;continue;}
            var d3=r.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Source Decorations 3D - OneToOne");
            int count=d3?d3.childCount:0;
            int bb=r.GetComponentsInChildren<MMBillboardToCamera>(true).Length;
            int quad=r.GetComponentsInChildren<MeshFilter>(true).Count(m=>m.sharedMesh&&m.sharedMesh.name=="MM6_SourceSpriteQuad");
            int old=r.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Source Sprites - OneToOne");
            int roots3=r.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Source Decorations 3D - OneToOne");
            int err=r.GetComponentsInChildren<Renderer>(true).Count(rr=>rr.sharedMaterials.Any(m=>m&&(!m.shader||m.shader.name=="Hidden/InternalErrorShader")));
            bool ok=count==Expected[i]&&bb==0&&quad==0&&old==0&&roots3==1&&err==0;
            rows.Add($"{Keys[i]},{Expected[i]},{count},{bb},{quad},{old},{roots3},{err},{(ok?"PASS":"FAIL")}");
            total3d+=count;totalBB+=bb;totalQuad+=quad;totalOld+=old;totalErr+=err;
            if(ok)pass++;else fail++;
        }

        var dragon=preview.Find("Dragon Isle - LINKED REFERENCE");
        int dragonBB=dragon?dragon.GetComponentsInChildren<MMBillboardToCamera>(true).Length:999;
        int dragonQuad=dragon?dragon.GetComponentsInChildren<MeshFilter>(true).Count(m=>m.sharedMesh&&m.sharedMesh.name=="MM6_SourceSpriteQuad"):999;
        int dragonOld=dragon?dragon.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Source Sprites - OneToOne"):999;
        bool dragonOK=dragon&&dragonBB==0&&dragonQuad==0&&dragonOld==0;
        if(!dragonOK)fail++;

        File.WriteAllLines("Validation/SourceDecoration3D/Production/linked_audit.csv",rows);
        File.WriteAllText("Validation/SourceDecoration3D/Production/linked_audit.txt",
            $"status={(fail==0?"PASS":"FAIL")}\nregions_passed={pass}/{Keys.Length}\nlinked_3d_total={total3d}\nexpected_3d_total=1670\nbillboards_total={totalBB}\nmm6_quad_total={totalQuad}\nold_sprite_roots_total={totalOld}\nerror_shader_renderers_total={totalErr}\ndragon_isle_sprite_free={dragonOK}\n");
        Debug.Log($"SAFE_SOURCE_3D_LINKED_AUDIT status={(fail==0?"PASS":"FAIL")} pass={pass}/{Keys.Length} total3d={total3d}");
    }
}
