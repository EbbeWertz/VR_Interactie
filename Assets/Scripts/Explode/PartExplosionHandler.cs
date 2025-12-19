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
    public GameObject coreInstance;

    private Dictionary<Transform, LineRenderer> childLineMap = new Dictionary<Transform, LineRenderer>();
    private LineRenderer parentTether;

    void Awake() => part = GetComponent<SelectablePart>();

    public void ToggleExplosion(bool explode)
    {
        // Strict check: verify real selectable children exist
        bool hasSelectableChildren = false;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<SelectablePart>() != null) { hasSelectableChildren = true; break; }
        }

        if (explode && !hasSelectableChildren) return;

        if (activeRoutine != null) StopCoroutine(activeRoutine);

        if (explode) CreateCoreAndLines();
        else DestroyCoreAndLines();

        activeRoutine = StartCoroutine(Animate(explode));
        part.isExploded = explode;

        var outlineHandler = GetComponent<PartOutlineHandler>();
        if (!explode) outlineHandler.SetColliderActive(true);

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

        // Tether current ball to parent ball
        Transform parentTransform = transform.parent;
        if (parentTransform != null)
        {
            var parentHandler = parentTransform.GetComponent<PartExplosionHandler>();
            if (parentHandler != null && parentHandler.coreInstance != null)
            {
                parentTether = CreateLine("Tether_To_Parent");
            }
        }

        // Lines to children
        childLineMap.Clear();
        foreach (Transform child in transform)
        {
            if (child == coreInstance.transform || !part.originalLocalPositions.ContainsKey(child)) continue;
            childLineMap[child] = CreateLine("Line_" + child.name);
        }
    }

    private LineRenderer CreateLine(string lineName)
    {
        GameObject lineObj = new GameObject(lineName);
        lineObj.transform.SetParent(coreInstance.transform);
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        return lr;
    }

    private void DestroyCoreAndLines()
    {
        if (coreInstance != null)
        {
            childLineMap.Clear();
            parentTether = null;
            Destroy(coreInstance);
            coreInstance = null;
        }
    }

    private void LateUpdate()
    {
        if (!part.isExploded || coreInstance == null) return;

        if (parentTether != null)
        {
            var parentHandler = transform.parent.GetComponent<PartExplosionHandler>();
            if (parentHandler != null && parentHandler.coreInstance != null)
            {
                parentTether.SetPosition(0, coreInstance.transform.position);
                parentTether.SetPosition(1, parentHandler.coreInstance.transform.position);
            }
        }

        foreach (var kvp in childLineMap)
        {
            Transform child = kvp.Key;
            LineRenderer lr = kvp.Value;
            if (child == null || lr == null) continue;

            var childHandler = child.GetComponent<PartExplosionHandler>();
            bool childIsExploded = childHandler != null && childHandler.coreInstance != null;

            if (childIsExploded) lr.enabled = false; // Hide tether to mesh, ball-to-ball tether takes over
            else
            {
                lr.enabled = true;
                lr.SetPosition(0, coreInstance.transform.position);
                lr.SetPosition(1, CalculateBounds(child).center); // Center of mesh bounds
            }
        }
    }

    IEnumerator Animate(bool explode)
    {
        Bounds b = CalculateBounds(transform);
        Vector3 centerWorld = b.center;
        float t = 0;

        Dictionary<Transform, Vector3> targetLocalPositions = new Dictionary<Transform, Vector3>();
        Dictionary<Transform, Vector3> startLocalPositions = new Dictionary<Transform, Vector3>();

        foreach (Transform child in transform)
        {
            if (coreInstance != null && child == coreInstance.transform) continue;
            if (!part.originalLocalPositions.TryGetValue(child, out Vector3 originalLocalPos)) continue;

            startLocalPositions[child] = child.localPosition;

            if (explode)
            {
                Vector3 childWorldCenter = CalculateBounds(child).center;
                Vector3 dir = (childWorldCenter - centerWorld).normalized;
                Vector3 startWorldPos = transform.TransformPoint(originalLocalPos);
                Vector3 targetWorld = startWorldPos + (dir * b.extents.magnitude * explosionStrength);
                targetLocalPositions[child] = transform.InverseTransformPoint(targetWorld);
            }
            else targetLocalPositions[child] = originalLocalPos;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            float factor = Mathf.SmoothStep(0, 1, t / duration);
            foreach (var kvp in targetLocalPositions)
            {
                if (kvp.Key == null) continue;
                kvp.Key.localPosition = Vector3.Lerp(startLocalPositions[kvp.Key], kvp.Value, factor);
            }
            yield return null;
        }
        activeRoutine = null;
    }

    private void UpdateChildrenState(bool explode)
    {
        foreach (Transform child in transform)
        {
            if (coreInstance != null && child == coreInstance.transform) continue;
            var childPart = child.GetComponent<SelectablePart>();
            if (childPart == null) continue;

            var childOutline = child.GetComponent<PartOutlineHandler>();
            if (explode) childOutline?.SetColliderActive(true);
            else
            {
                childPart.isSelected = false;
                childOutline?.UpdateVisuals(false);
                childOutline?.SetColliderActive(false);
                child.GetComponent<PartExplosionHandler>()?.ToggleExplosion(false);
            }
        }
    }

    private Bounds CalculateBounds(Transform obj)
    {
        Renderer[] rs = obj.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(obj.position, Vector3.zero);
        Bounds b = new Bounds();
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