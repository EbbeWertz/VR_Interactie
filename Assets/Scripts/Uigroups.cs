using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Uigroups : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Het paneel (Content) waar de items in komen.")]
    public Transform contentParent;
    
    [Tooltip("De prefab voor een item in de lijst.")]
    public GameObject listItemPrefab;

    [Header("Color Settings")]
    public Color completedColor = Color.green;
    public Color missingColor = Color.red;

    private List<GameObject> currentUIItems = new List<GameObject>();

    /// <summary>
    /// Wordt aangeroepen door SphereSelector.
    /// </summary>
    public void UpdateExplorerList(List<GameObject> completedObjects, List<GameObject> missingObjects)
    {
        ClearCurrentList();

        // 1. Voeg voltooide groepen/objecten toe (Groen/Zwart)
        foreach (GameObject obj in completedObjects)
        {
            CreateListItem(obj.name, completedColor, "[OK]");
        }

        // 2. Voeg objecten toe die nog nodig zijn (Rood)
        foreach (GameObject obj in missingObjects)
        {
            CreateListItem(obj.name, missingColor, "[MISSING]");
        }
    }

    private void CreateListItem(string name, Color textColor, string status)
    {
        if (listItemPrefab == null || contentParent == null) return;

        GameObject listItem = Instantiate(listItemPrefab, contentParent);
        currentUIItems.Add(listItem);

        TextMeshProUGUI tmpText = listItem.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = $"{status} {name}";
            tmpText.color = textColor;
        }
    }

    private void ClearCurrentList()
    {
        foreach (GameObject item in currentUIItems)
        {
            Destroy(item);
        }
        currentUIItems.Clear();
    }
}