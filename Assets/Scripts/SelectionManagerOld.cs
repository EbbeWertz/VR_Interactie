using UnityEngine;

public class SelectionManagerOld : MonoBehaviour
{
    public Camera cam;
    public SelectablePartOld rootPart;
    public LayerMask outlineLayer;

    private SelectablePartOld hoveredPart;
    private SelectablePartOld selectedPart;

    void Update()
    {
        HandleHover();

        if (Input.GetMouseButtonDown(0))
            HandleSelection();

        if (selectedPart != null)
        {
            if (Input.GetKeyDown(KeyCode.E))
                selectedPart.Explode();

            if (Input.GetKeyDown(KeyCode.C))
            {
                if (selectedPart.isExploded)
                    selectedPart.Collapse();
                else if (selectedPart.transform.parent != null)
                {
                    var parentPart = selectedPart.transform.parent.GetComponent<SelectablePartOld>();
                    if (parentPart != null)
                        parentPart.Collapse();
                }
            }
        }
    }

    void HandleHover()
    {
        if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000f, outlineLayer))
        {
            ClearHover();
            return;
        }

        var outline = hit.collider.GetComponent<OutlineSelectableOld>();
        if (outline == null)
        {
            ClearHover();
            return;
        }

        if (outline.owner != hoveredPart)
        {
            ClearHover();
            hoveredPart = outline.owner;
            hoveredPart.SetHover(true);
        }
    }

    void ClearHover()
    {
        if (hoveredPart != null)
            hoveredPart.SetHover(false);

        hoveredPart = null;
    }

    void HandleSelection()
    {
        if (hoveredPart == null) return;

        if (selectedPart != null)
            selectedPart.Deselect();

        selectedPart = hoveredPart;
        selectedPart.Select();
    }
}
