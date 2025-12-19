using UnityEngine;
using System.Collections.Generic;

public class SelectablePart : MonoBehaviour
{
    public bool isSelected { get; set; }
    public bool isExploded { get; set; }
    
    // Store original positions for the Animation script to use
    public Dictionary<Transform, Vector3> originalLocalPositions = new Dictionary<Transform, Vector3>();

    void Awake()
    {
        foreach (Transform child in transform)
        {
            originalLocalPositions[child] = child.localPosition;
        }
    }
}