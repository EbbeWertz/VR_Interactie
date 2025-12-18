using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SelectablePart : MonoBehaviour
{
    public Material outlineMaterial;
    public float explodeDuration = 1f;

    [HideInInspector] public GameObject outlineInstance;

    private MeshRenderer outlineRenderer;
    private Dictionary<Transform, Vector3> originalLocalPositions = new Dictionary<Transform, Vector3>();
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

    // Always generate an outline
    public void CreateOutline()
    {
        if (outlineInstance != null)
            return;

        // Grab all MeshFilters in children, no special rules
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);

        if (meshFilters.Length == 0 || outlineMaterial == null)
            return;

        outlineInstance = new GameObject("Outline");
        outlineInstance.layer = LayerMask.NameToLayer("Outline");
        outlineInstance.transform.SetParent(transform, false);
        outlineInstance.transform.localScale = Vector3.one * 1.05f;

        Mesh combinedMesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        CombineInstance[] combines = new CombineInstance[meshFilters.Length];
        for (int i = 0; i < meshFilters.Length; i++)
        {
            combines[i].mesh = meshFilters[i].sharedMesh;
            combines[i].transform = transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
        }

        combinedMesh.CombineMeshes(combines, true, true, false);

        var mfOutline = outlineInstance.AddComponent<MeshFilter>();
        mfOutline.sharedMesh = combinedMesh;

        outlineRenderer = outlineInstance.AddComponent<MeshRenderer>();
        outlineRenderer.material = outlineMaterial;
        outlineRenderer.enabled = false;

        var collider = outlineInstance.AddComponent<MeshCollider>();
        collider.sharedMesh = combinedMesh;
        collider.convex = true;

        var selectable = outlineInstance.AddComponent<OutlineSelectable>();
        selectable.owner = this;
    }

    public void DestroyOutline()
    {
        if (outlineInstance != null)
            Destroy(outlineInstance);

        outlineInstance = null;
        outlineRenderer = null;
    }

    public void SetOutlined(bool value)
    {
        if (outlineRenderer != null)
            outlineRenderer.enabled = value;
    }

    // ---------------- Explosion ----------------

    public void Explode()
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
        Bounds parentBounds = GetBounds(transform);
        Vector3 parentCenter = parentBounds.center;

        int count = transform.childCount;
        if (count == 0)
            yield break;

        Vector3[] start = new Vector3[count];
        Vector3[] target = new Vector3[count];

        float distance = parentBounds.extents.magnitude;

        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            start[i] = child.localPosition;

            if (explode)
            {
                Bounds cb = GetBounds(child);
                Vector3 dir = (cb.center - parentCenter).normalized;
                target[i] = start[i] + dir * distance;
            }
            else
            {
                target[i] = originalLocalPositions[child];
            }
        }

        float t = 0f;
        while (t < explodeDuration)
        {
            float eased = Mathf.SmoothStep(0f, 1f, t / explodeDuration);
            for (int i = 0; i < count; i++)
                transform.GetChild(i).localPosition = Vector3.Lerp(start[i], target[i], eased);

            t += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < count; i++)
            transform.GetChild(i).localPosition = target[i];

        explodeRoutine = null;
    }


    public void SetOutlineColliderEnabled(bool value)
    {
        if (outlineInstance == null) return;

        var col = outlineInstance.GetComponent<MeshCollider>();
        if (col != null)
            col.enabled = value;
    }

    // Enable only for direct children
    public void EnableDirectChildColliders()
    {
        foreach (Transform child in transform)
        {
            var sp = child.GetComponent<SelectablePart>();
            if (sp != null)
                sp.SetOutlineColliderEnabled(true);
        }
    }

    // Disable all descendants recursively
    public void DisableAllDescendantColliders()
    {
        foreach (Transform child in transform)
        {
            var sp = child.GetComponent<SelectablePart>();
            if (sp != null)
            {
                sp.SetOutlineColliderEnabled(false);
                sp.DisableAllDescendantColliders();
            }
        }
    }



    Bounds GetBounds(Transform t)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs)
            b.Encapsulate(r.bounds);
        return b;
    }
}
