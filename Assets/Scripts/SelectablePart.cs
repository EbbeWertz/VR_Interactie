using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SelectablePart : MonoBehaviour
{
    public Material outlineMaterial;
    public float explodeDuration = 1f;

    private GameObject outlineInstance;
    private MeshRenderer outlineRenderer;

    private Dictionary<Transform, Vector3> originalLocalPositions =
        new Dictionary<Transform, Vector3>();

    private Coroutine explodeRoutine;

    void Awake()
    {
        CacheOriginalPositions();
        CreateOutline();
    }

    void CacheOriginalPositions()
    {
        foreach (Transform child in transform)
            originalLocalPositions[child] = child.localPosition;
    }

    void CreateOutline()
    {
        MeshFilter[] childMeshFilters = GetComponentsInChildren<MeshFilter>();
        if (childMeshFilters.Length == 0 || outlineMaterial == null)
            return;

        outlineInstance = new GameObject("Outline");
        outlineInstance.transform.SetParent(transform);
        outlineInstance.transform.localPosition = Vector3.zero;
        outlineInstance.transform.localRotation = Quaternion.identity;
        outlineInstance.transform.localScale = Vector3.one * 1.05f;

        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        CombineInstance[] combines = new CombineInstance[childMeshFilters.Length];

        int combineIndex = 0;
        foreach (var mf in childMeshFilters)
        {
            if (mf.sharedMesh == null)
                continue;

            combines[combineIndex].mesh = mf.sharedMesh;
            combines[combineIndex].transform =
                transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;

            combineIndex++;
        }

        combinedMesh.CombineMeshes(combines, true, true);

        var mfOutline = outlineInstance.AddComponent<MeshFilter>();
        mfOutline.sharedMesh = combinedMesh;

        outlineRenderer = outlineInstance.AddComponent<MeshRenderer>();
        outlineRenderer.material = outlineMaterial;
        outlineRenderer.enabled = false;

        var meshCollider = outlineInstance.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = combinedMesh;
        meshCollider.convex = false;

        var selectable = outlineInstance.AddComponent<OutlineSelectable>();
        selectable.owner = this;
    }

    public void SetOutlined(bool value)
    {
        if (outlineRenderer != null)
            outlineRenderer.enabled = value;
    }

    // ---------------- Animated Explosion ----------------

    public void ExplodeSphere()
    {
        StartExplosion(true);
    }

    public void Collapse()
    {
        StartExplosion(false);
    }

    void StartExplosion(bool explode)
    {
        if (explodeRoutine != null)
            StopCoroutine(explodeRoutine);

        explodeRoutine = StartCoroutine(AnimateExplosion(explode));
    }

    IEnumerator AnimateExplosion(bool explode)
    {
        Bounds b = GetBounds(transform);
        float radius = Mathf.Max(b.size.x, b.size.y, b.size.z) * 0.5f;

        int count = transform.childCount;
        if (count == 0)
            yield break;

        Vector3[] startPositions = new Vector3[count];
        Vector3[] targetPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            if (!originalLocalPositions.ContainsKey(child))
                continue;

            startPositions[i] = child.localPosition;

            if (explode)
            {
                Vector3 dir = GetPointOnSphere(i, count);
                targetPositions[i] =
                    originalLocalPositions[child] + dir * radius;
            }
            else
            {
                targetPositions[i] = originalLocalPositions[child];
            }
        }

        float t = 0f;

        while (t < explodeDuration)
        {
            float normalized = t / explodeDuration;
            float eased = Mathf.SmoothStep(0f, 1f, normalized);

            for (int i = 0; i < count; i++)
            {
                Transform child = transform.GetChild(i);
                child.localPosition =
                    Vector3.Lerp(startPositions[i], targetPositions[i], eased);
            }

            t += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < count; i++)
            transform.GetChild(i).localPosition = targetPositions[i];

        explodeRoutine = null;
    }

    Bounds GetBounds(Transform t)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs)
            b.Encapsulate(r.bounds);
        return b;
    }

    Vector3 GetPointOnSphere(int index, int count)
    {
        float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
        float y = 1f - (index / (float)(count - 1)) * 2f;
        float r = Mathf.Sqrt(1f - y * y);
        float theta = goldenAngle * index;
        return new Vector3(Mathf.Cos(theta) * r, y, Mathf.Sin(theta) * r);
    }
}
