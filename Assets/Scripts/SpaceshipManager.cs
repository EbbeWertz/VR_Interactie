using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SpaceshipManager : MonoBehaviour
{
    [Header("Root of the spaceship hierarchy")]
    public Transform spaceshipRoot;

    [Header("Auto-Assigned Parts (read-only)")]
    public List<SpaceshipPart> parts = new List<SpaceshipPart>();

    [Header("Editor Selection Override")]
    public SpaceshipPart selectedPart;

    void OnEnable()
    {
        // ScanHierarchy();
        // UpdateAppearance();
    }

    void OnValidate()
    {
        // ScanHierarchy();
        // UpdateAppearance();
    }

    // ──────────────────────────────────────────────
    // Scan & Attach SpaceshipPart Scripts
    // ──────────────────────────────────────────────
    void ScanHierarchy()
    {
        // parts.Clear();

        // if (spaceshipRoot == null)
        //     return;

        // Transform[] allTransforms = spaceshipRoot.GetComponentsInChildren<Transform>(true);

        // foreach (Transform t in allTransforms)
        // {
        //     if (t == spaceshipRoot) continue;

        //     // Only attach SpaceshipPart if this object or its children have a Renderer
        //     if (t.GetComponentInChildren<Renderer>(true) == null)
        //         continue;

        //     SpaceshipPart part = t.GetComponent<SpaceshipPart>();
        //     if (part == null)
        //     {
        //         part = t.gameObject.AddComponent<SpaceshipPart>();
        //     }

        //     parts.Add(part);
        // }
    }


    // ──────────────────────────────────────────────
    // Update appearances based on editor selection
    // ──────────────────────────────────────────────
    public void UpdateAppearance()
    {
        // Debug.Log("update");
        // if (spaceshipRoot == null || parts.Count == 0)
        //     return;

        // if (selectedPart == null)
        // {
        //     foreach (var p in parts)
        //         p.SetOpaque();
        // }
        // else
        // {
        //     foreach (var p in parts)
        //     {
        //         if (p == selectedPart)
        //             p.SetOutlined();
        //         else
        //             p.SetTransparent();
        //     }
        // }
    }
}
