using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

public class SphereSelector : MonoBehaviour
{
    [Header("Selection Properties")]
    [Tooltip("The radius of the sphere used for the selection check.")]
    public float selectionRadius = 3f; 
    
    [Tooltip("The material to apply to selected objects.")]
    public Material selectionMaterial;
    
    [Tooltip("The minimum distance the sphere must move before re-checking selection.")]
    public float movementThreshold = 0.1f; 

    [Tooltip("The minimum scale of the sphere must grow before re-checking selection.")]
    public float scaleThreshold = 0.1f; 

    [Header("UI Integration")]
    [Tooltip("Reference to the Selection UI Manager for updating the list.")]
    public SelectionUIManager uiManager;

    // Internal tracking variables
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Vector3 lastPosition;
    
    private void Start()
    {
        lastPosition = transform.position;
        transform.localScale = Vector3.one * (selectionRadius * 2);

        UpdateSelectionDisplay(); 
    }

    private void Update()
    {
        bool updateNeeded = false;

        if (Vector3.Distance(transform.position, lastPosition) >= movementThreshold)
        {
            updateNeeded = true;
            lastPosition = transform.position;
        }

        if (Mathf.Abs((transform.localScale.x / 2) - selectionRadius) >= scaleThreshold)
        {
            updateNeeded = true;
            transform.localScale = Vector3.one * (selectionRadius * 2);
        }

        if (updateNeeded)
        {
            UpdateSelectionDisplay();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up materials when the object is destroyed or the scene stops
        ResetHighlighting();
    }
    
    /// <summary>
    /// Performs the physics check and returns a list of GameObjects COMPLETELY inside the sphere,
    /// using a vertex check for precision.
    /// </summary>
    public List<GameObject> SelectObjectsInSphere()
    {
        List<GameObject> selectedObjects = new List<GameObject>();
        Vector3 sphereCenter = transform.position;

        // Physics.OverlapSphere checks for collision overlap
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, selectionRadius); 

        foreach (var hitCollider in hitColliders)
        {
            GameObject obj = hitCollider.gameObject;
            
            // Exclude the selector itself
            if (obj == gameObject) 
            {
                continue;
            }

            if (IsMeshCompletelyContained(obj, sphereCenter, selectionRadius))
            {
                selectedObjects.Add(obj);
            }
        }
        return selectedObjects;
    }
        
    /// <summary>
    /// Updates the display by resetting the old selection, highlighting the new one, and updating the UI.
    /// </summary>
    public void UpdateSelectionDisplay()
    {
        // 1. Reset old highlighting to restore original materials
        ResetHighlighting();

        // 2. Get the new selection
        List<GameObject> currentSelection = SelectObjectsInSphere();
        LogSelectionToConsole(currentSelection);
        
        if (uiManager != null)
        {
            uiManager.UpdateExplorerList();
        }

        // 3. Highlight the selected objects
        foreach (GameObject obj in currentSelection)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null && selectionMaterial != null)
            {
                // Store the original materials
                originalMaterials.Add(renderer, renderer.sharedMaterials);
                
                // Replace all materials with the selection material
                Material[] newMats = Enumerable.Repeat(selectionMaterial, renderer.sharedMaterials.Length).ToArray();
                renderer.sharedMaterials = newMats;
            }
        }
    }

    /// <summary>
    /// Checks if all vertices of a Mesh are fully inside the selection sphere.
    /// This is an expensive check and should only be run sparingly.
    /// </summary>
    private bool IsMeshCompletelyContained(GameObject obj, Vector3 sphereCenter, float sphereRadius)
    {
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            // If there's no MeshFilter, we fall back to a less precise Bounding Box check.
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                // Fallback check: Checks the 8 corners of the AABB.
                return IsBoundsCompletelyContained(collider.bounds, sphereCenter, sphereRadius);
            }
            return false;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Transform objectTransform = obj.transform;
        float radiusSquared = sphereRadius * sphereRadius;

        // Iterate through ALL vertices of the mesh
        for (int i = 0; i < vertices.Length; i++)
        {
            // Convert the local vertex position to World Space
            Vector3 worldVertex = objectTransform.TransformPoint(vertices[i]);
            
            // Calculate the squared distance from the vertex to the sphere center
            float distanceSquared = (worldVertex - sphereCenter).sqrMagnitude;
            
            // If ANY vertex is outside the sphere, the mesh is NOT contained
            if (distanceSquared > radiusSquared)
            {
                return false;
            }
        }
        
        // If all vertices are within the radius, the mesh is completely contained.
        return true;
    }

    /// <summary>
    /// Checks if a Collider's Axis-Aligned Bounding Box (AABB) is fully inside the selection sphere.
    /// Used as a fallback check when mesh data is unavailable.
    /// </summary>
    private bool IsBoundsCompletelyContained(Bounds bounds, Vector3 sphereCenter, float sphereRadius)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        float radiusSquared = sphereRadius * sphereRadius;
        
        // Loop through all 8 corners: (min.x, min.y, min.z) to (max.x, max.y, max.z)
        for (int x = 0; x < 2; x++)
        {
            float currentX = (x == 0) ? min.x : max.x;
            for (int y = 0; y < 2; y++)
            {
                float currentY = (y == 0) ? min.y : max.y;
                for (int z = 0; z < 2; z++)
                {
                    float currentZ = (z == 0) ? min.z : max.z;
                    
                    Vector3 corner = new Vector3(currentX, currentY, currentZ);
                    
                    // If the distance of ANY corner to the sphere center is GREATER than the radius, 
                    // the bounds are NOT completely contained.
                    if ((corner - sphereCenter).sqrMagnitude > radiusSquared)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    // --- Reset Method ---
    public void ResetHighlighting()
    {
        // Restore all original materials
        foreach (var pair in originalMaterials)
        {
            Renderer renderer = pair.Key;
            Material[] materials = pair.Value;
            
            if (renderer != null)
            {
                renderer.sharedMaterials = materials;
            }
        }
        
        // Clear the storage dictionary
        originalMaterials.Clear();
    }

    private void OnValidate()
    {
        // This method runs automatically in the editor whenever a value is changed in the Inspector.
        // It ensures the visual scale immediately matches the radius you type in.
        
        // Sphere scale is diameter (radius * 2)
        transform.localScale = Vector3.one * (selectionRadius * 2);
    }

    private void LogSelectionToConsole(List<GameObject> selection)
    {   
        if (selection.Count > 0)
        {
            Debug.Log("--- SPHERE SELECTION REPORT ---");
            Debug.Log($"Total Objects Found: {selection.Count}");
            Debug.Log("List of Selected Element Names:");
            
            foreach (GameObject obj in selection)
            {
                Debug.Log($"  - {obj.name}"); 
            }
            Debug.Log("-------------------------------");
        }
    }
}