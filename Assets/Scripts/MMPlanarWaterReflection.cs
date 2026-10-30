using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Camera-specific planar reflection for the connected sea.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Renderer))]
public sealed class MMPlanarWaterReflection : MonoBehaviour
{
    public int resolution = 768;
    public float reflectionDistance = 900f;
    Camera reflectionCamera;
    RenderTexture reflectionTexture;
    Renderer surface;
    static bool rendering;
    Renderer[] waterSurfaces;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        foreach (var r in FindObjectsByType<MeshRenderer>())
            if (r.name == "Linked connected sea and existing lowland waters 20260929" && !r.GetComponent<MMPlanarWaterReflection>())
                r.gameObject.AddComponent<MMPlanarWaterReflection>();
    }

    void OnEnable() { surface = GetComponent<Renderer>(); Camera.onPreCull += RenderForCamera; }
    void OnDisable()
    {
        Camera.onPreCull -= RenderForCamera;
        Shader.SetGlobalFloat("_MMPlanarAvailable", 0);
        if (reflectionCamera) DestroyObject(reflectionCamera.gameObject);
        if (reflectionTexture) { reflectionTexture.Release(); DestroyObject(reflectionTexture); }
    }
    static void DestroyObject(Object obj) { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
    void RenderForCamera(Camera source)
    {
        if (rendering || !enabled || !surface || !source || source.cameraType == CameraType.Reflection || source.cameraType == CameraType.Preview) return;
        source.depthTextureMode |= DepthTextureMode.Depth;
        if (!reflectionCamera)
        {
            var go = new GameObject("Water Reflection Camera") { hideFlags = HideFlags.HideAndDontSave };
            reflectionCamera = go.AddComponent<Camera>(); reflectionCamera.enabled = false;
        }
        int size = Mathf.Clamp(resolution, 128, 1024);
        if (!reflectionTexture || reflectionTexture.width != size)
        {
            if (reflectionTexture) { reflectionTexture.Release(); DestroyObject(reflectionTexture); }
            reflectionTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGBHalf) { name = "Coastal Reflection", hideFlags = HideFlags.HideAndDontSave };
        }
        float level = surface.bounds.center.y;
        Matrix4x4 mirror = Matrix4x4.identity;
        mirror.m11 = -1; mirror.m13 = 2 * level;
        reflectionCamera.CopyFrom(source);
        reflectionCamera.enabled = false; reflectionCamera.cameraType = CameraType.Reflection;
        reflectionCamera.targetTexture = reflectionTexture;
        reflectionCamera.depthTextureMode = DepthTextureMode.None;
        reflectionCamera.allowMSAA = false;
        reflectionCamera.farClipPlane = Mathf.Min(source.farClipPlane, reflectionDistance);
        reflectionCamera.transform.position = mirror.MultiplyPoint(source.transform.position);
        reflectionCamera.transform.rotation = Quaternion.LookRotation(mirror.MultiplyVector(source.transform.forward), mirror.MultiplyVector(source.transform.up));
        reflectionCamera.worldToCameraMatrix = source.worldToCameraMatrix * mirror;
        Vector3 clipPoint = reflectionCamera.worldToCameraMatrix.MultiplyPoint(new Vector3(0, level + .07f, 0));
        Vector3 clipNormal = reflectionCamera.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
        reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(new Vector4(clipNormal.x, clipNormal.y, clipNormal.z, -Vector3.Dot(clipPoint, clipNormal)));
        bool oldInvert = GL.invertCulling;
        var previousDepth = Shader.GetGlobalTexture("_CameraDepthTexture");
        if (waterSurfaces == null)
            waterSurfaces = System.Array.FindAll(FindObjectsByType<MeshRenderer>(), r => System.Array.Exists(r.sharedMaterials, m => m && m.shader.name == "MMUnity/Linked Natural Water"));
        var hidden = new bool[waterSurfaces.Length];
        try
        {
            rendering = true; Shader.SetGlobalFloat("_MMRenderingReflection", 1);
            // Exclude GrabPass water entirely: rendering it, even clipped, can
            // replace the source camera's named refraction grab with this view.
            for (int i = 0; i < waterSurfaces.Length; i++) if (waterSurfaces[i]) { hidden[i] = waterSurfaces[i].forceRenderingOff; waterSurfaces[i].forceRenderingOff = true; }
            GL.invertCulling = !oldInvert;
            reflectionCamera.Render();
        }
        finally
        {
            GL.invertCulling = oldInvert; Shader.SetGlobalFloat("_MMRenderingReflection", 0); rendering = false;
            for (int i = 0; i < waterSurfaces.Length; i++) if (waterSurfaces[i]) waterSurfaces[i].forceRenderingOff = hidden[i];
            Shader.SetGlobalTexture("_CameraDepthTexture", previousDepth);
        }
        Shader.SetGlobalTexture("_MMPlanarReflection", reflectionTexture);
        Shader.SetGlobalMatrix("_MMPlanarVP", GL.GetGPUProjectionMatrix(reflectionCamera.projectionMatrix, false) * reflectionCamera.worldToCameraMatrix);
        Shader.SetGlobalFloat("_MMPlanarLevel", level);
        Shader.SetGlobalFloat("_MMPlanarAvailable", 1);
    }
}
