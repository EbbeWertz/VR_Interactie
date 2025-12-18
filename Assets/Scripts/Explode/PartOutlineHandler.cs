using UnityEngine;

[RequireComponent(typeof(SelectablePart))]
public class PartOutlineHandler : MonoBehaviour
{
    public Material hoverMaterial;
    public Material selectMaterial;
    
    private SelectablePart part;
    private MeshRenderer outlineRenderer;
    private MeshCollider outlineCollider;

    void Start()
    {
        part = GetComponent<SelectablePart>();
        CreateOutline();
        
        // Hide outline if child of another part initially
        if (transform.parent?.GetComponent<SelectablePart>() != null)
            SetColliderActive(false);
    }

    public void UpdateVisuals(bool isHovering)
    {
        if (part.isSelected)
        {
            SetOutline(selectMaterial, true);
        }
        else
        {
            SetOutline(hoverMaterial, isHovering);
        }
    }

    public void SetColliderActive(bool active) => outlineCollider.enabled = active;

    private void SetOutline(Material mat, bool enabled)
    {
        if (!outlineRenderer) return;
        outlineRenderer.material = mat;
        outlineRenderer.enabled = enabled;
    }

    private void CreateOutline()
    {
        GameObject outlineInstance = new GameObject("Outline_Mesh");
        outlineInstance.transform.SetParent(transform, false);
        outlineInstance.layer = LayerMask.NameToLayer("Outline");
        outlineInstance.transform.localScale = Vector3.one * 1.05f;

        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
        CombineInstance[] combine = new CombineInstance[filters.Length];
        
        for (int i = 0; i < filters.Length; i++)
        {
            combine[i].mesh = filters[i].sharedMesh;
            combine[i].transform = transform.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;
        }

        Mesh mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.CombineMeshes(combine, true, true);

        outlineInstance.AddComponent<MeshFilter>().sharedMesh = mesh;
        outlineRenderer = outlineInstance.AddComponent<MeshRenderer>();
        outlineRenderer.enabled = false;
        
        outlineCollider = outlineInstance.AddComponent<MeshCollider>();
        outlineCollider.sharedMesh = mesh;
        outlineCollider.convex = true;

        var selectable = outlineInstance.AddComponent<OutlineSelectable>();
        selectable.owner = part;
    }
}