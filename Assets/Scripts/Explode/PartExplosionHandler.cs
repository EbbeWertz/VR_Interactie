using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SelectablePart))]
public class PartExplosionHandler : MonoBehaviour
{
    public float duration = 0.5f;
    public float explosionStrength = 0.5f;
    public Material lineMaterial;

    public float lineWidth = 0.01f;

    private SelectablePart part;
    private Coroutine activeRoutine;
    private GameObject coreInstance;
    private List<LineRenderer> activeLines = new List<LineRenderer>();

    void Awake() => part = GetComponent<SelectablePart>();

    public void ToggleExplosion(bool explode)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);

        if (explode) CreateCoreAndLines();
        else DestroyCoreAndLines();

        activeRoutine = StartCoroutine(Animate(explode));
        part.isExploded = explode;

        // FIX: When collapsing (explode == false), we MUST re-enable this part's collider 
        // so it can be hovered/selected again.
        var outlineHandler = GetComponent<PartOutlineHandler>();
        if (!explode)
        {
            outlineHandler.SetColliderActive(true);
        }

        outlineHandler.UpdateVisuals(false);
        UpdateChildrenState(explode);
    }

    private void CreateCoreAndLines()
    {
        if (coreInstance != null) return;

        Bounds b = CalculateBounds(transform);
        coreInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreInstance.name = "Core_" + name;
        coreInstance.transform.position = b.center;
        coreInstance.transform.SetParent(transform);
        coreInstance.transform.localScale = Vector3.one * (b.extents.magnitude * 0.2f);
        coreInstance.GetComponent<Renderer>().material.color = Color.red;
        coreInstance.layer = LayerMask.NameToLayer("Outline");

        var coreLogic = coreInstance.AddComponent<ExplosionCore>();
        coreLogic.owner = part;

        // Create a LineRenderer for every child part
        foreach (Transform child in transform)
        {
            if (child == coreInstance.transform || !part.originalLocalPositions.ContainsKey(child)) continue;

            GameObject lineObj = new GameObject("Line_" + child.name);
            lineObj.transform.SetParent(coreInstance.transform); // Clean up automatically with core

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = true;

            activeLines.Add(lr);
        }
    }

    private void DestroyCoreAndLines()
    {
        if (coreInstance != null)
        {
            // Stop the lines from updating immediately
            activeLines.Clear();
            Destroy(coreInstance);
            coreInstance = null;
        }
    }

    private void LateUpdate() // Use LateUpdate to ensure lines follow animated children
    {
        if (part.isExploded && coreInstance != null && activeLines.Count > 0)
        {
            int lineIdx = 0;
            foreach (Transform child in transform)
            {
                // Skip the core and any non-part children
                if (child == coreInstance.transform || !part.originalLocalPositions.ContainsKey(child)) continue;
                if (lineIdx >= activeLines.Count) break;

                activeLines[lineIdx].SetPosition(0, coreInstance.transform.position);
                activeLines[lineIdx].SetPosition(1, child.position);
                lineIdx++;
            }
        }
    }

    IEnumerator Animate(bool explode)
    {
        Bounds b = CalculateBounds(transform);
        Vector3 centerWorld = b.center;
        float t = 0;

        // 1. Pre-calculate the target positions for all children 
        // to avoid the "moving target" bug and improve performance.
        Dictionary<Transform, Vector3> targetLocalPositions = new Dictionary<Transform, Vector3>();
        Dictionary<Transform, Vector3> startLocalPositions = new Dictionary<Transform, Vector3>();

        foreach (Transform child in transform)
        {
            if (coreInstance != null && child == coreInstance.transform) continue;
            if (!part.originalLocalPositions.TryGetValue(child, out Vector3 originalLocalPos)) continue;

            startLocalPositions[child] = child.localPosition;

            if (explode)
            {
                // Calculate direction from the group center to the child's center
                Vector3 childWorldCenter = CalculateBounds(child).center;
                Vector3 dir = (childWorldCenter - centerWorld).normalized;

                // Use explosionStrength to control the distance
                // We calculate the target based on the ORIGINAL position, not current
                Vector3 startWorldPos = transform.TransformPoint(originalLocalPos);
                Vector3 targetWorld = startWorldPos + (dir * b.extents.magnitude * explosionStrength);
                targetLocalPositions[child] = transform.InverseTransformPoint(targetWorld);
            }
            else
            {
                targetLocalPositions[child] = originalLocalPos;
            }
        }

        // 2. Perform the Lerp
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalizedTime = t / duration;
            // Use an easing function for smoother movement
            float factor = Mathf.SmoothStep(0, 1, normalizedTime);

            foreach (var kvp in targetLocalPositions)
            {
                Transform child = kvp.Key;
                if (child == null) continue;

                Vector3 start = startLocalPositions[child];
                Vector3 end = kvp.Value;
                child.localPosition = Vector3.Lerp(start, end, factor);
            }
            yield return null;
        }

        activeRoutine = null;
    }

    private void UpdateChildrenState(bool explode)
    {
        foreach (Transform child in transform)
        {
            // Use ReferenceEquals to safely check against destroyed Unity objects
            if (coreInstance != null && child == coreInstance.transform) continue;

            var childPart = child.GetComponent<SelectablePart>();
            if (childPart == null) continue;

            var childOutline = child.GetComponent<PartOutlineHandler>();
            if (explode)
            {
                childOutline?.SetColliderActive(true);
            }
            else
            {
                childPart.isSelected = false;
                childOutline?.UpdateVisuals(false);
                childOutline?.SetColliderActive(false);
                // Deep collapse
                child.GetComponent<PartExplosionHandler>()?.ToggleExplosion(false);
            }
        }
    }

    private Bounds CalculateBounds(Transform obj)
    {
        Renderer[] rs = obj.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(obj.position, Vector3.zero);
        bool first = true;
        foreach (Renderer r in rs)
        {
            if (coreInstance != null && r.gameObject == coreInstance) continue;
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }
}