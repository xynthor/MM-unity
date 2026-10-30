using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Runtime-only Frozen Highlands northern floe rendering refinement.
// The original saved Mesh asset and Enroth scene are never modified.
// No change to floe centers, neighboring regions or coastal water.
public static class MMNorthernFloeRimPolish
{
    static readonly string[] ApprovedNames =
    {
        "FrozenHighlands_NorthFracturedIce"
    };

    public static int AppliedRegionCount { get; private set; }
    public static int ProcessedFloeCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyToEnroth()
    {
        AppliedRegionCount = 0;
        ProcessedFloeCount = 0;
        if (SceneManager.GetActiveScene().name != "Enroth")
            return;

        var filters = UnityEngine.Object.FindObjectsByType<MeshFilter>(
            FindObjectsInactive.Exclude).Where(f => f && f.sharedMesh).ToArray();
        foreach (string name in ApprovedNames)
        {
            var filter = filters.FirstOrDefault(f =>
                f.sharedMesh && f.sharedMesh.name == name &&
                f.name == "Nonoverlapping Fractured Snow Floe Mesh");
            if (!filter || filter.GetComponent<MeshCollider>())
                continue;

            Mesh original = filter.sharedMesh;
            int count;
            Mesh revised;
            try
            {
                revised = BuildFracturedRim(original, out count);
            }
            catch (Exception e)
            {
                Debug.LogWarning("North floe polish skipped " + name + ": " + e.Message);
                continue;
            }

            if (count < 3 ||
                revised.vertexCount <= original.vertexCount ||
                Vector3.Distance(revised.bounds.center, original.bounds.center) > 5f)
            {
                UnityEngine.Object.Destroy(revised);
                continue;
            }

            revised.hideFlags = HideFlags.DontSave;
            filter.sharedMesh = revised;
            AppliedRegionCount++;
            ProcessedFloeCount += count;
        }
    }

    static Mesh BuildFracturedRim(Mesh source, out int numberOfFloes)
    {
        Vector3[] old = source.vertices;
        int[] all = source.triangles;
        int[] parent = Enumerable.Range(0, old.Length).ToArray();
        Func<int, int> find = null;
        find = i => parent[i] == i ? i : (parent[i] = find(parent[i]));
        for (int k = 0; k < all.Length; k += 3)
        {
            int root = find(all[k]);
            for (int j = 1; j < 3; j++)
                parent[find(all[k + j])] = root;
        }

        var groups = Enumerable.Range(0, old.Length)
            .GroupBy(i => find(i)).OrderBy(g => g.Min()).ToArray();
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tops = new List<int>();
        var sides = new List<int>();

        int floe = 0;
        foreach (var group in groups)
        {
            int[] ids = group.OrderBy(i => i).ToArray();
            int n = (ids.Length - 1) / 3;
            if (n < 3 || ids.Length != 3 * n + 1)
                throw new InvalidOperationException("Unrecognized approved ice layout");

            Vector2 center = Vector2.zero;
            for (int i = 0; i < n; i++)
                center += new Vector2(old[ids[i]].x, old[ids[i]].z);
            center /= n;

            float radius = 0f;
            for (int i = 0; i < n; i++)
                radius += (new Vector2(old[ids[i]].x, old[ids[i]].z) - center).magnitude;
            radius /= n;
            int count = n * 2;
            var outlines = new Vector2[count];
            var edgeHeight = new float[count];

            // Chip the long geometrically straight polygon edges into
            // irregular frost-cleaved faces. Keep the original footprint and
            // each floe's centroid instead of generating new floe positions.
            for (int i = 0; i < n; i++)
            {
                Vector3 a = old[ids[i]], b = old[ids[(i + 1) % n]];
                Vector2 corner = new Vector2(a.x, a.z);
                outlines[2 * i] = center + (corner - center) *
                    (1f + .024f * Mathf.Sin(floe * 4.1f + i * 2.9f));
                edgeHeight[2 * i] = .26f + .1f *
                    Mathf.Sin(i * 1.88f + floe * 1.47f);

                Vector2 midpoint = new Vector2(
                    (a.x + b.x) * .5f, (a.z + b.z) * .5f);
                float inset = .07f * radius *
                    (.6f + .4f * Mathf.Sin(i * 3.31f + floe * 1.7f));
                outlines[2 * i + 1] = midpoint +
                    (center - midpoint).normalized * inset;
                edgeHeight[2 * i + 1] = .19f +
                    Mathf.Max(0f, .16f * Mathf.Sin(i * 2.47f + floe));
            }

            int outer = vertices.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = outlines[i];
                vertices.Add(new Vector3(p.x, edgeHeight[i], p.y));
                uvs.Add(p / 10f);
            }

            int bevel = vertices.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = center + (outlines[i] - center) * .75f;
                float y = .68f + .19f *
                    Mathf.Sin(i * 1.83f + floe * 1.93f);
                vertices.Add(new Vector3(p.x, y, p.y));
                uvs.Add(p / 10f);
            }

            int summit = vertices.Count;
            Vector2 top = center + new Vector2(
                Mathf.Cos(floe * 2.399f), Mathf.Sin(floe * 2.399f)) *
                radius * .08f;
            vertices.Add(new Vector3(top.x,
                1.1f + .19f * Mathf.Sin(floe * 1.4f), top.y));
            uvs.Add(top / 10f);

            for (int i = 0; i < count; i++)
            {
                int a = outer + i, b = outer + (i + 1) % count;
                int c = bevel + i, d = bevel + (i + 1) % count;
                tops.Add(a); tops.Add(c); tops.Add(b);
                tops.Add(b); tops.Add(c); tops.Add(d);
                tops.Add(summit); tops.Add(d); tops.Add(c);
            }

            int subsurface = vertices.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = center + (outlines[i] - center) * .91f;
                vertices.Add(new Vector3(p.x,
                    -.9f - .22f * Mathf.Sin(i * 1.77f + floe), p.y));
                uvs.Add(new Vector2(i / (float)count, .35f));
            }

            int bottom = vertices.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = center + (outlines[i] - center) * .75f;
                vertices.Add(new Vector3(p.x,
                    -3.3f - .7f * Mathf.Sin(i * 2.03f + floe), p.y));
                uvs.Add(new Vector2(i / (float)count, 0f));
            }

            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                int a = outer + i, b = outer + j;
                int c = subsurface + i, d = subsurface + j;
                int e = bottom + i, f = bottom + j;
                sides.Add(a); sides.Add(b); sides.Add(c);
                sides.Add(b); sides.Add(d); sides.Add(c);
                sides.Add(c); sides.Add(d); sides.Add(e);
                sides.Add(d); sides.Add(f); sides.Add(e);
            }
            floe++;
        }

        Mesh mesh = new Mesh { name = source.name + "_FracturedRimsRuntime" };
        mesh.indexFormat = vertices.Count > 65000 ?
            IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(tops, 0);
        mesh.SetTriangles(sides, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        numberOfFloes = floe;
        return mesh;
    }
}
