using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public Camera cam;
    public SelectablePart rootPart;
    public LayerMask outlineLayer;

    private SelectablePart viewedPart;
    private SelectablePart hoveredPart;
    private bool exploded;

    void Start()
    {
        viewedPart = rootPart;
        UpdateChildOutlines(viewedPart);
    }

    void Update()
    {
        HandleHover();

        if (Input.GetKeyDown(KeyCode.Space))
            ToggleExplode();

        if (Input.GetMouseButtonDown(0) && exploded && hoveredPart != null)
            PromoteHovered();
    }

    void ToggleExplode()
    {
        if (!exploded)
        {
            viewedPart.Explode();
            exploded = true;

            viewedPart.SetOutlineColliderEnabled(false);
            viewedPart.EnableDirectChildColliders();

            foreach (Transform child in viewedPart.transform)
            {
                var sp = child.GetComponent<SelectablePart>();
                if (sp != null)
                    sp.DisableAllDescendantColliders();
            }
        }
        else
        {
            viewedPart.Collapse();
            exploded = false;

            viewedPart.SetOutlineColliderEnabled(true);
            viewedPart.DisableAllDescendantColliders();
        }
    }

    void HandleHover()
    {
        if (!exploded)
            return;

        if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000f, outlineLayer))
        {
            ClearHover();
            return;
        }

        var outline = hit.collider.GetComponent<OutlineSelectable>();
        if (outline == null)
        {
            ClearHover();
            return;
        }

        if (outline.owner != hoveredPart)
        {
            ClearHover();
            hoveredPart = outline.owner;
            hoveredPart.SetOutlined(true);
        }
    }

    void ClearHover()
    {
        if (hoveredPart != null)
            hoveredPart.SetOutlined(false);

        hoveredPart = null;
    }

    void PromoteHovered()
    {
        if (hoveredPart == null)
            return;

        // Disable outline and collider for the selected part
        hoveredPart.SetOutlined(false);
        hoveredPart.SetOutlineColliderEnabled(false);

        // Collapse all other children of the currently viewed part
        foreach (Transform child in viewedPart.transform)
        {
            var sp = child.GetComponent<SelectablePart>();
            if (sp != null && sp != hoveredPart)
                sp.Collapse();
        }

        // The parent itself may collapse if it is not the hovered part
        if (viewedPart != hoveredPart)
            viewedPart.Collapse();

        // Keep the hovered part exploded
        hoveredPart.Explode();

        // The hovered part becomes the new viewedPart
        viewedPart = hoveredPart;
        hoveredPart = null;

        exploded = true; // promoted part stays exploded

        // Update outlines for the direct children of the promoted part only
        foreach (Transform child in viewedPart.transform)
        {
            var sp = child.GetComponent<SelectablePart>();
            if (sp != null)
            {
                sp.CreateOutline();
                sp.SetOutlineColliderEnabled(true);
                sp.DisableAllDescendantColliders();
            }
        }
    }



    void UpdateChildOutlines(SelectablePart parent)
    {
        foreach (Transform child in parent.transform)
        {
            var sp = child.GetComponent<SelectablePart>();
            if (sp != null)
            {
                sp.CreateOutline();
                sp.SetOutlineColliderEnabled(true);
                DisableDescendantColliders(sp, true);
            }
        }
    }

    void DisableDescendantColliders(SelectablePart part, bool includeSelf)
    {
        foreach (Transform child in part.transform)
        {
            var sp = child.GetComponent<SelectablePart>();
            if (sp != null)
            {
                sp.SetOutlineColliderEnabled(false);
                DisableDescendantColliders(sp, false);
            }
        }

        if (includeSelf)
            part.SetOutlineColliderEnabled(false);
    }
}
