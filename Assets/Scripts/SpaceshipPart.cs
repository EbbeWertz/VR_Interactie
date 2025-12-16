using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SpaceshipPartMode
{
    Opaque,
    Transparent,
    Selected
}

public class SpaceshipPart : MonoBehaviour
{
    [Header("Appearance Settings")]
    public Color opaqueColor = Color.white;
    public Color transparentColor = new Color(1, 1, 1, 0.25f);
    public float outlineColor = 1f;
    public float outlineWidth = 4f;

    [Header("Editor Mode (Preview)")]
    public SpaceshipPartMode editorMode = SpaceshipPartMode.Opaque;

    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();

        ApplyMode(editorMode);
    }

    // Apply the mode to this part or its children if no renderer
    public void ApplyMode(SpaceshipPartMode mode)
    {
        if (rend != null)
        {
            Material mat = rend.sharedMaterial; // edit material directly

            switch (mode)
            {
                case SpaceshipPartMode.Opaque:
                    mat.color = opaqueColor;
                    mat.SetFloat("_Surface", 0);
                    mat.DisableKeyword("_OUTLINE_ON");
                    mat.SetFloat("_OutlineWidth", 0);
                    mat.renderQueue = 2000;
                    break;

                case SpaceshipPartMode.Transparent:
                    mat.color = transparentColor;
                    mat.SetFloat("_Surface", 1);
                    mat.DisableKeyword("_OUTLINE_ON");
                    mat.SetFloat("_OutlineWidth", 0);
                    mat.renderQueue = 3000;
                    break;

                case SpaceshipPartMode.Selected:
                    mat.color = opaqueColor;
                    mat.SetFloat("_Surface", 0);
                    mat.EnableKeyword("_OUTLINE_ON");
                    mat.SetFloat("_OutlineWidth", outlineWidth);
                    mat.SetColor("_OutlineColor", Color.white);
                    mat.renderQueue = 2000;
                    break;
            }
        }
        else
        {
            // No renderer → propagate to children
            foreach (Transform child in transform)
            {
                SpaceshipPart childPart = child.GetComponent<SpaceshipPart>();
                if (childPart != null)
                    childPart.ApplyMode(mode);
            }
        }
    }

    public void SetOpaque() => ApplyMode(SpaceshipPartMode.Opaque);
    public void SetTransparent() => ApplyMode(SpaceshipPartMode.Transparent);
    public void SetSelected() => ApplyMode(SpaceshipPartMode.Selected);
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpaceshipPart))]
public class SpaceshipPartEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpaceshipPart part = (SpaceshipPart)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);

        SpaceshipPartMode newMode = (SpaceshipPartMode)EditorGUILayout.EnumPopup("Mode", part.editorMode);

        if (newMode != part.editorMode)
        {
            Undo.RecordObject(part, "Change Spaceship Part Mode");
            part.editorMode = newMode;
            part.ApplyMode(newMode);
            EditorUtility.SetDirty(part);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Opaque")) part.SetOpaque();
        if (GUILayout.Button("Transparent")) part.SetTransparent();
        if (GUILayout.Button("Selected")) part.SetSelected();
        EditorGUILayout.EndHorizontal();
    }
}
#endif
