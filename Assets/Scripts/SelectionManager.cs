using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public Transform rootObject; 
    public Camera cam;

    private SelectablePart selectedPart;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TrySelect();
        }
    }

    void TrySelect()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Transform hitTransform = hit.collider.transform;

            // Find nearest selectable parent (including self)
            SelectablePart selectable = hitTransform.GetComponentInParent<SelectablePart>();

            if (selectable != null)
            {
                SelectPart(selectable);
            }
        }
    }

    void SelectPart(SelectablePart part)
    {
        // Clear previous selection
        if (selectedPart != null)
            selectedPart.SetSelected(false);

        selectedPart = part;

        if (selectedPart != null)
            selectedPart.SetSelected(true);

        UpdateTransparency();
    }

    void UpdateTransparency()
    {
        // Make all selectable parts transparent except the selected hierarchy
        foreach (SelectablePart sp in rootObject.GetComponentsInChildren<SelectablePart>())
        {
            bool isSelectedOrChild = selectedPart != null && sp.transform.IsChildOf(selectedPart.transform);
            sp.SetTransparent(!isSelectedOrChild);
        }
    }
}
