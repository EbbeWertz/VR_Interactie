using System.Collections.Generic;
using UnityEngine;

public class Subgroups : MonoBehaviour
{
    public float maxDistance = 100f;

    // Bewaar originele kleuren
    private Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>();

    // Tracking van geselecteerde objecten per parent
    private Dictionary<Transform, HashSet<GameObject>> selectedObjectsPerParent = new Dictionary<Transform, HashSet<GameObject>>();

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
            GameObject clickedObject = hit.collider.gameObject;
            Transform clickedParent = clickedObject.transform.parent;

            // Eerst check: click buiten andere parentgroepen?
            ResetOtherParentGroups(clickedParent);

            if (clickedParent == null)
            {
                // Geen parent → gewoon geel
                Highlight(clickedObject, Color.yellow);
                return;
            }

            // Zorg dat er een set bestaat voor deze parent
            if (!selectedObjectsPerParent.ContainsKey(clickedParent))
                selectedObjectsPerParent[clickedParent] = new HashSet<GameObject>();

            HashSet<GameObject> selectedSet = selectedObjectsPerParent[clickedParent];

            // Voeg parent toe indien nog niet aanwezig
            if (!selectedSet.Contains(clickedParent.gameObject))
            {
                selectedSet.Add(clickedParent.gameObject);
                Highlight(clickedParent.gameObject, Color.green);
            }

            // Voeg het aangeklikte object toe
            if (!selectedSet.Contains(clickedObject))
            {
                selectedSet.Add(clickedObject);
                Highlight(clickedObject, Color.green);
            }

            // Controleer of alle siblings + parent geselecteerd zijn
            int totalObjects = clickedParent.childCount + 1; // children + parent
            if (selectedSet.Count >= totalObjects)
            {
                // Alles zwart
                Highlight(clickedParent.gameObject, Color.black);
                for (int i = 0; i < clickedParent.childCount; i++)
                {
                    Highlight(clickedParent.GetChild(i).gameObject, Color.black);
                }

                // Reset tracking voor deze parent
                selectedObjectsPerParent.Remove(clickedParent);
            }
        }
    }

    // Highlight functie
    void Highlight(GameObject obj, Color color)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;

        if (!originalColors.ContainsKey(obj))
            originalColors[obj] = r.material.color;

        r.material.color = color;
    }

    // Reset andere parentgroepen (alles terug naar origineel)
    void ResetOtherParentGroups(Transform currentParent)
    {
        List<Transform> parentsToReset = new List<Transform>();

        foreach (var kvp in selectedObjectsPerParent)
        {
            Transform parent = kvp.Key;
            if (parent != currentParent)
            {
                parentsToReset.Add(parent);
            }
        }

        foreach (Transform parent in parentsToReset)
        {
            HashSet<GameObject> group = selectedObjectsPerParent[parent];

            foreach (GameObject obj in group)
            {
                Renderer r = obj.GetComponent<Renderer>();
                if (r != null && originalColors.ContainsKey(obj))
                    r.material.color = originalColors[obj];
            }

            selectedObjectsPerParent.Remove(parent);
        }
    }

    // Optioneel: reset alles
    public void ResetAll()
    {
        foreach (var kvp in originalColors)
        {
            Renderer r = kvp.Key.GetComponent<Renderer>();
            if (r != null)
                r.material.color = kvp.Value;
        }

        originalColors.Clear();
        selectedObjectsPerParent.Clear();
    }
}
