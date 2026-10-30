using UnityEngine;

public class MMBillboardToCamera : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (!cam) return;
        Vector3 d = cam.transform.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(-d.normalized, Vector3.up);
    }
}
