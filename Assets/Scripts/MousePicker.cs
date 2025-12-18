using UnityEngine;

public class MousePicker : MonoBehaviour
{
    public LayerMask selectableLayer;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayer))
            {
                Debug.Log("Geselecteerd: " + hit.collider.name);

                // Optioneel: highlight
                Highlight(hit.collider.gameObject);
            }
        }
    }

    void Highlight(GameObject obj)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = Color.yellow;
        }
    }
}
