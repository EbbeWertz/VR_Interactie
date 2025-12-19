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
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, outlineLayer))
        {
            var core = hit.collider.GetComponent<ExplosionCore>();
            if (core != null)
            {
                hoveredPart = null;
                core.RequestCollapse();
                return; 
            }

            var part = hit.collider.GetComponent<OutlineSelectable>()?.owner;
            
            // Only update if selection actually changed
            if (part != selectedPart)
            {
                if (selectedPart != null)
                {
                    selectedPart.isSelected = false;
                    selectedPart.GetComponent<PartOutlineHandler>().UpdateVisuals(false);
                }

                selectedPart = part;

                if (selectedPart != null)
                {
                    selectedPart.isSelected = true;
                    selectedPart.GetComponent<PartOutlineHandler>().UpdateVisuals(true);
                }
            }
        }
    }

    void HandleInputs()
    {
        if (selectedPart == null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            // Before exploding, clear selection visuals so the parent outline vanishes
            var handler = selectedPart.GetComponent<PartOutlineHandler>();
            selectedPart.isSelected = false;
            handler.UpdateVisuals(false); 
            handler.SetColliderActive(false);

            selectedPart.GetComponent<PartExplosionHandler>()?.ToggleExplosion(true);
            
            // Null out selection because the parent is now "intangible"
            selectedPart = null;
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