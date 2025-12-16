using UnityEngine;
using UnityEngine.UI;
using TMPro; // Use this if you are using TextMeshPro
using System.Collections.Generic;

public class SelectionUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Content panel inside the Scroll View where items will be placed.")]
    public Transform contentParent;
    
    [Tooltip("The prefab (Text/Button) for displaying a single selected object's name.")]
    public GameObject listItemPrefab;

    [Header("Script References")]
    [Tooltip("Reference to the SphereSelector script on the sphere GameObject.")]
    public SphereSelector sphereSelector;

    // A list to keep track of the currently instantiated UI items
    private List<GameObject> currentUIItems = new List<GameObject>();

    private void Start()
    {
        // Safety check to ensure the essential references are assigned
        if (sphereSelector == null)
        {
            Debug.LogError("SphereSelector reference is missing on the UI Manager!");
        }
        if (contentParent == null || listItemPrefab == null)
        {
            Debug.LogError("Content Parent or List Item Prefab references are missing!");
        }

        // We can call the initial update here, but it should ideally be called after the sphere moves.
        // We will call it manually for the first time.
        // UpdateExplorerList();
    }

    /// <summary>
    /// Public method to be called by the SphereSelector whenever the selection changes.
    /// </summary>
    public void UpdateExplorerList()
    {
        // 1. Get the current list of selected GameObjects
        List<GameObject> selectedObjects = sphereSelector.SelectObjectsInSphere();
        
        // 2. Clean up the previous list
        ClearCurrentList();

        // 3. Populate the new list
        foreach (GameObject obj in selectedObjects)
        {
            GameObject listItem = Instantiate(listItemPrefab, contentParent);
            currentUIItems.Add(listItem);

            // Attempt to find a Text component (or TextMeshPro) to set the name
            TextMeshProUGUI tmpText = listItem.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = obj.name;
            }
            else
            {
                // Fallback for standard Text component (less common now)
                Text standardText = listItem.GetComponent<Text>();
                if (standardText != null)
                {
                    standardText.text = obj.name;
                }
                else
                {
                    Debug.LogWarning($"List Item Prefab is missing a Text or TextMeshPro component! Could not display name for {obj.name}");
                }
            }
        }
        
        Debug.Log($"UI Explorer populated with {selectedObjects.Count} items.");
    }

    /// <summary>
    /// Destroys all currently displayed list items.
    /// </summary>
    private void ClearCurrentList()
    {
        foreach (GameObject item in currentUIItems)
        {
            Destroy(item);
        }
        currentUIItems.Clear();
    }
}