using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SelectablePartOld : MonoBehaviour
{
    [Header("Outline Materials")]
    public Material hoverOutlineMaterial;
    public Material selectOutlineMaterial;

    public float explodeDuration = 1f;

    [HideInInspector] public GameObject outlineInstance;

    private MeshRenderer outlineRenderer;
    private MeshCollider outlineCollider;
    private Coroutine explodeRoutine;

    private Dictionary<Transform, Vector3> originalLocalPositions = new Dictionary<Transform, Vector3>();

    public bool isSelected { get; private set; } = false;
    public bool isExploded { get; private set; } = false;

    void Awake()
    {
        CacheOriginalPositions();
        CreateOutline();

        // Disable collider by default if this is a child part
        var parentPart = transform.parent?.GetComponent<SelectablePartOld>();
        if (parentPart != null)
        {
            SetOutlineColliderEnabled(false);
        }
    }

    void CacheOriginalPositions()
    {
        foreach (Transform child in transform)
            originalLocalPositions[child] = child.localPosition;
    }

    public void Select()
    {
        isSelected = true;
        SetOutline(selectOutlineMaterial, true);
    }

    public void Deselect()
    {
        isSelected = false;
        SetOutline(false);
    }

    public void SetHover(bool hovering)
    {
        if (!isSelected)
            SetOutline(hovering ? hoverOutlineMaterial : null, hovering);
    }

    void SetOutline(Material mat, bool enabled)
    {
        if (outlineRenderer != null)
        {
            outlineRenderer.material = mat;
            outlineRenderer.enabled = enabled;
        }
    }

    void SetOutline(bool enabled)
    {
        if (outlineRenderer != null)
            outlineRenderer.enabled = enabled;
    }

    public void CreateOutline()
    {
        if (outlineInstance != null) return;

        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
        if (meshFilters.Length == 0) return;

        outlineInstance = new GameObject("Outline");
        outlineInstance.layer = LayerMask.NameToLayer("Outline");
        outlineInstance.transform.SetParent(transform, false);
        outlineInstance.transform.localScale = Vector3.one * 1.05f;

        Mesh combinedMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };

        CombineInstance[] combines = new CombineInstance[meshFilters.Length];
        for (int i = 0; i < meshFilters.Length; i++)
        {
            combines[i].mesh = meshFilters[i].sharedMesh;
            combines[i].transform = transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
        }

        combinedMesh.CombineMeshes(combines, true, true, false);

        var mf = outlineInstance.AddComponent<MeshFilter>();
        mf.sharedMesh = combinedMesh;

        outlineRenderer = outlineInstance.AddComponent<MeshRenderer>();
        outlineRenderer.enabled = false;

        outlineCollider = outlineInstance.AddComponent<MeshCollider>();
        outlineCollider.sharedMesh = combinedMesh;
        outlineCollider.convex = true;

        var selectable = outlineInstance.AddComponent<OutlineSelectableOld>();
        selectable.owner = this;
    }

    public void SetOutlineColliderEnabled(bool enabled)
    {
        if (outlineCollider != null)
            outlineCollider.enabled = enabled;
    }

    public void Explode()
    {
        if (isExploded) return;
        isExploded = true;

        // Disable own collider so we can't select the "group" anymore
        SetOutlineColliderEnabled(false);

        // Enable child colliders so they can be selected individually
        foreach (Transform child in transform)
        {
            var sp = child.GetComponent<SelectablePartOld>();
            if (sp != null) sp.SetOutlineColliderEnabled(true);
        }

        StartExplosion(true);
    }

    public void Collapse()
    {
        if (!isExploded) return;
        isExploded = false;

        // Re-enable own collider
        SetOutlineColliderEnabled(true);

        foreach (Transform child in transform)
        {
            var sp = child.GetComponent<SelectablePartOld>();
            if (sp != null)
            {
                sp.Collapse(); // Recursively collapse
                sp.Deselect();
                sp.SetOutlineColliderEnabled(false); // Hide children from Raycast
            }
        }

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
        int count = transform.childCount;
        if (count == 0) yield break;

        Vector3[] start = new Vector3[count];
        Vector3[] target = new Vector3[count];

        Bounds parentBounds = GetBounds(transform);
        Vector3 parentCenter = parentBounds.center;
        float distance = parentBounds.extents.magnitude;

        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            start[i] = child.localPosition;

            if (explode)
            {
                Bounds childBounds = GetBounds(child);
                Vector3 dir = (childBounds.center - parentCenter).normalized;
                target[i] = start[i] + dir * distance;
            }
            else
            {
                target[i] = GetOriginalLocalPosition(child);
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

    Vector3 GetOriginalLocalPosition(Transform child)
    {
        if (!originalLocalPositions.ContainsKey(child))
            originalLocalPositions[child] = child.localPosition;

        return originalLocalPositions[child];
    }

    Bounds GetBounds(Transform t)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0)
            return new Bounds(t.position, Vector3.zero);

        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs)
            b.Encapsulate(r.bounds);
        return b;
    }
}
