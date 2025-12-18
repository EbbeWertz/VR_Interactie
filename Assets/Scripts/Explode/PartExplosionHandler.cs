using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SelectablePart))]
public class PartExplosionHandler : MonoBehaviour
{
    public float duration = 1f;
    private SelectablePart part;
    private Coroutine activeRoutine;

    void Awake() => part = GetComponent<SelectablePart>();

    public void ToggleExplosion(bool explode)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(Animate(explode));
        
        part.isExploded = explode;
        UpdateChildrenState(explode);
    }

    private void UpdateChildrenState(bool explode)
    {
        foreach (Transform child in transform)
        {
            var childPart = child.GetComponent<SelectablePart>();
            if (childPart == null) continue;

            // When exploded, children become clickable. When collapsed, they don't.
            child.GetComponent<PartOutlineHandler>()?.SetColliderActive(explode);
            
            if (!explode) child.GetComponent<PartExplosionHandler>()?.ToggleExplosion(false);
        }
    }

    IEnumerator Animate(bool explode)
    {
        Bounds b = CalculateBounds(transform);
        float t = 0;

        while (t < duration)
        {
            t += Time.deltaTime;
            float factor = Mathf.SmoothStep(0, 1, t / duration);

            foreach (Transform child in transform)
            {
                if (!part.originalLocalPositions.ContainsKey(child)) continue;
                
                Vector3 start = child.localPosition;
                Vector3 target = part.originalLocalPositions[child];

                if (explode)
                {
                    Vector3 dir = (CalculateBounds(child).center - b.center).normalized;
                    target = part.originalLocalPositions[child] + (dir * b.extents.magnitude);
                }

                child.localPosition = Vector3.Lerp(start, target, factor);
            }
            yield return null;
        }
    }

    private Bounds CalculateBounds(Transform obj)
    {
        Renderer[] rs = obj.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(obj.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}