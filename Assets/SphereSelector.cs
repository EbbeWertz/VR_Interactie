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
        selectionRadius = transform.localScale.x / 2f;

        if (Vector3.Distance(transform.position, lastPosition) >= movementThreshold)
        {
            UpdateSelectionDisplay();
            lastPosition = transform.position;
        }
    }
    
    private void OnDestroy()
    {
        // Clean up materials when the object is destroyed or the scene stops
        ResetHighlighting();
    }
    
    /// <summary>
    /// Performs the physics check and returns a list of overlapping GameObjects.
    /// </summary>
    public List<GameObject> SelectObjectsInSphere()
    {
        List<GameObject> selectedObjects = new List<GameObject>();
        
        // Physics.OverlapSphere checks for collision overlap
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, selectionRadius); 

        foreach (var hitCollider in hitColliders)
        {
            GameObject obj = hitCollider.gameObject;
            
            // Exclude the selector itself
            if (obj != gameObject) 
            {
                // NOTE: This currently selects objects that *overlap*. 
                // To select only objects *completely inside*, you would add that complex geometry check here.
                selectedObjects.Add(obj);
            }
        }
        return selectedObjects;
    }
        
    /// <summary>
    /// Updates the display by resetting the old selection and highlighting the new one.
    /// </summary>
    public void UpdateSelectionDisplay()
    {
        // 1. Reset old highlighting to restore original materials
        ResetHighlighting();

        // 2. Get the new selection
        List<GameObject> currentSelection = SelectObjectsInSphere();
        LogSelectionToConsole(currentSelection);
        
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