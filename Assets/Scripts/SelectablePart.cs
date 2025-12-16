using UnityEngine;

public class SelectablePart : MonoBehaviour
{
    [Header("Outline")]
    public Material outlineMaterial; // assign a cyan emissive material
    private GameObject outlineInstance;

    [Header("Transparency")]
    public Material transparentMaterial;

    private Renderer[] renderers;
    private Material[] originalMaterials;

    void Awake()
    {
        // Cache renderers and original materials
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].material;

        // Create runtime outline if material is assigned
        if (outlineMaterial != null)
        {
            CreateOutline();
        }
    }

    private void CreateOutline()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) return; // Only create if this object has a mesh

        outlineInstance = new GameObject("Outline");
        outlineInstance.transform.SetParent(transform);
        outlineInstance.transform.localPosition = Vector3.zero;
        outlineInstance.transform.localRotation = Quaternion.identity;
        outlineInstance.transform.localScale = Vector3.one * 1.05f;

        MeshFilter newMf = outlineInstance.AddComponent<MeshFilter>();
        newMf.mesh = mf.sharedMesh;

        MeshRenderer mr = outlineInstance.AddComponent<MeshRenderer>();
        mr.material = outlineMaterial;

        outlineInstance.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        // Enable/disable outline
        if (outlineInstance != null)
            outlineInstance.SetActive(selected);
    }

    public void SetTransparent(bool transparent)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = transparent ? transparentMaterial : originalMaterials[i];
        }
    }
}
