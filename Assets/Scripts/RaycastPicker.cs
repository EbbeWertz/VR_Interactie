using System.Collections.Generic;
using UnityEngine;

public class RaycastPicker : MonoBehaviour
{
    public float maxDistance = 100f;

    GameObject currentSelection;
    Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>();

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryPick();
        }
    }

    void TryPick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            ClearHighlights(); // reset vorige selectie

            currentSelection = hit.collider.gameObject;

            // Highlight zelf
            Highlight(currentSelection, Color.yellow);

            // Highlight siblings (rood bv)
            SelectSiblings(currentSelection);
        }
    }

    void Highlight(GameObject obj, Color highlightColor)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;

        // Bewaar originele kleur als nog niet opgeslagen
        if (!originalColors.ContainsKey(obj))
            originalColors[obj] = r.material.color;

        r.material.color = highlightColor;
    }

    void ClearHighlights()
    {
        foreach (var kvp in originalColors)
        {
            Renderer r = kvp.Key.GetComponent<Renderer>();
            if (r != null)
                r.material.color = kvp.Value;
        }

        originalColors.Clear();
    }

    void SelectSiblings(GameObject obj)
    {
        Transform parent = obj.transform.parent;
        if (parent == null) return;

        Highlight(parent.gameObject, Color.red);

        foreach (Transform sibling in parent)
        {
            if (sibling.gameObject == obj) continue; // skip zelf
            Highlight(sibling.gameObject, Color.red);
        }
    }
}
