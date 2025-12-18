using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public Camera cam;
    public SelectablePart rootPart;

    private SelectablePart viewedPart;
    private SelectablePart hoveredPart;

    private bool exploded;

    void Start()
    {
        viewedPart = rootPart;
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
            viewedPart.ExplodeSphere();
            exploded = true;
        }
        else
        {
            viewedPart.Collapse();
            exploded = false;
        }
    }

    void HandleHover()
    {
        if (!exploded)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            ClearHover();
            return;
        }

        OutlineSelectable outline =
            hit.collider.GetComponent<OutlineSelectable>();

        if (outline == null)
        {
            ClearHover();
            return;
        }

        SelectablePart sp = outline.owner;

        if (sp != hoveredPart &&
            sp.transform.parent == viewedPart.transform)
        {
            ClearHover();
            hoveredPart = sp;
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
        viewedPart.Collapse();
        exploded = false;

        viewedPart = hoveredPart;
        ClearHover();
    }
}
