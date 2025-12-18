using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public Camera cam;
    public LayerMask outlineLayer;

    private SelectablePart hoveredPart;
    private SelectablePart selectedPart;

    void Update()
    {
        HandleHover();
        if (Input.GetMouseButtonDown(0)) HandleSelection();
        HandleInputs();
    }

    void HandleHover()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, outlineLayer))
        {
            var part = hit.collider.GetComponent<OutlineSelectable>()?.owner;
            if (part != hoveredPart)
            {
                hoveredPart?.GetComponent<PartOutlineHandler>().UpdateVisuals(false);
                hoveredPart = part;
                hoveredPart?.GetComponent<PartOutlineHandler>().UpdateVisuals(true);
            }
        }
        else if (hoveredPart != null)
        {
            hoveredPart.GetComponent<PartOutlineHandler>().UpdateVisuals(false);
            hoveredPart = null;
        }
    }

    void HandleSelection()
    {
        if (selectedPart != null) 
            selectedPart.isSelected = false;

        selectedPart = hoveredPart;
        
        if (selectedPart != null)
        {
            selectedPart.isSelected = true;
            selectedPart.GetComponent<PartOutlineHandler>().UpdateVisuals(true);
        }
    }

    void HandleInputs()
    {
        if (selectedPart == null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            selectedPart.GetComponent<PartExplosionHandler>()?.ToggleExplosion(true);
            selectedPart.GetComponent<PartOutlineHandler>()?.SetColliderActive(false);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            var target = selectedPart.isExploded ? selectedPart : selectedPart.transform.parent?.GetComponent<SelectablePart>();
            if (target != null)
            {
                target.GetComponent<PartExplosionHandler>()?.ToggleExplosion(false);
                target.GetComponent<PartOutlineHandler>()?.SetColliderActive(true);
            }
        }
    }
}