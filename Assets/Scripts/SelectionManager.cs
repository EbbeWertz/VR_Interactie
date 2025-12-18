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

            // Disable parent collider
            viewedPart.SetOutlineColliderEnabled(false);

            // Enable direct children colliders (they are scattered)
            viewedPart.EnableDirectChildColliders();

            // Ensure deeper descendants are disabled
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

            // Restore colliders after collapse
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
        // Collapse current part
        viewedPart.Collapse();
        exploded = false;

        viewedPart = hoveredPart;
        hoveredPart = null;

        UpdateChildOutlines(viewedPart);
    }

    // Enables outlines for direct children and disables colliders for deeper descendants
    void UpdateChildOutlines(SelectablePart parent)
    {
        foreach (Transform child in parent.transform)
        {
            var sp = child.GetComponent<SelectablePart>();
            if (sp != null)
            {
                sp.CreateOutline();
                sp.SetOutlineColliderEnabled(true); // direct child collider enabled

                // disable colliders for deeper descendants
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
        {
            part.SetOutlineColliderEnabled(false);
        }
    }
}
