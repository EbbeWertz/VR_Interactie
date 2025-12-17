using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UImanager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Het paneel (Content) waar de items in komen.")]
    public Transform contentParent;
    
    [Tooltip("De prefab voor een item in de lijst.")]
    public GameObject listItemPrefab;

    [Header("Color Settings")]
    public Color completedColor = Color.green;
    public Color missingColor = Color.red;

    [Header("Script References")]
    [Tooltip("Verwijzing naar de SphereGroupsSelector op de sphere.")]
    public SphereGroupsSelector sphereSelector;

    private List<GameObject> currentUIItems = new List<GameObject>();

    private void Start()
    {
        if (sphereSelector == null)
            Debug.LogError("SphereGroupsSelector reference is missing!");
        
        if (contentParent == null || listItemPrefab == null)
            Debug.LogError("UI references missing on SelectionUIManager!");
    }

    /// <summary>
    /// Deze methode wordt nu aangeroepen door SphereGroupsSelector.UpdateSelectionDisplay()
    /// </summary>
    public void UpdateExplorerList(List<GameObject> completedObjects, List<GameObject> missingObjects)
    {
        ClearCurrentList();

        // 1. Voeg voltooide objecten toe (Groen)
        foreach (GameObject obj in completedObjects)
        {
            CreateListItem(obj.name, completedColor, "[OK]");
        }

        // 2. Voeg missende objecten toe (Rood)
        foreach (GameObject obj in missingObjects)
        {
            CreateListItem(obj.name, missingColor, "[MISSING]");
        }

        Debug.Log($"UI geüpdatet: {completedObjects.Count} OK, {missingObjects.Count} MISSING");
    }

    private void CreateListItem(string itemName, Color textColor, string status)
    {
        if (listItemPrefab == null || contentParent == null) return;

        GameObject listItem = Instantiate(listItemPrefab, contentParent);
        currentUIItems.Add(listItem);

        // Zoek naar TextMeshPro component
        TextMeshProUGUI tmpText = listItem.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = $"<b>{status}</b> {itemName}";
            tmpText.color = textColor;
        }
        else
        {
            // Fallback voor gewone UI Text
            Text standardText = listItem.GetComponentInChildren<Text>();
            if (standardText != null)
            {
                standardText.text = $"{status} {itemName}";
                standardText.color = textColor;
            }
        }
    }

    private void ClearCurrentList()
    {
        foreach (GameObject item in currentUIItems)
        {
            if (item != null) Destroy(item);
        }
        currentUIItems.Clear();
    }
}