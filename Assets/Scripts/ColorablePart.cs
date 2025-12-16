using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways] // Runs in Edit mode
public class ColorablePart : MonoBehaviour
{
    [Header("Material Settings")]
    public Color opaqueColor = Color.white;
    public Color transparentColor = new Color(0, 0, 0, 0.5f);
    public Color highlightColor = Color.yellow;

    [Header("Outline Settings")]
    public float outlineWidth = 5.0f;

    private Renderer partRenderer;
    private Material partMaterial;

    public enum PartMode { Opaque, Transparent, Highlighted }
    public PartMode currentMode = PartMode.Opaque;

    void Awake()
    {
        partRenderer = GetComponent<Renderer>();

        if (partRenderer != null)
        {
            // Automatically assign material
            partMaterial = partRenderer.sharedMaterial;

            // Ensure renderer uses this material instance for runtime
            if (Application.isPlaying)
                partRenderer.material = partMaterial;
        }

        ApplyMode(currentMode);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ApplyMode(currentMode);
        }
#endif
    }

    public void ApplyMode(PartMode mode)
    {
        currentMode = mode;

        switch (mode)
        {
            case PartMode.Opaque: SetOpaque(); break;
            case PartMode.Transparent: SetTransparent(); break;
            case PartMode.Highlighted: SetHighlighted(); break;
        }
    }

    private void SetOpaque()
    {
        if (partMaterial == null) return;

        partMaterial.color = opaqueColor;
        SetOutline(false);
        SetTransparency(false);
    }

    private void SetTransparent()
    {
        if (partMaterial == null) return;

        partMaterial.color = transparentColor;
        SetOutline(false);
        SetTransparency(true);
    }

    private void SetHighlighted()
    {
        if (partMaterial == null) return;

        partMaterial.color = highlightColor;
        SetOutline(true);
        SetTransparency(false);
    }

    private void SetOutline(bool enabled)
    {
        if (partMaterial != null && partMaterial.HasProperty("_OutlineWidth"))
        {
            partMaterial.SetFloat("_OutlineWidth", enabled ? outlineWidth : 0f);
        }
    }

    private void SetTransparency(bool transparent)
    {
        if (partMaterial == null) return;

        if (transparent)
        {
            partMaterial.SetFloat("_Mode", 3); // Transparent
            partMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            partMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            partMaterial.SetInt("_ZWrite", 0);
            partMaterial.DisableKeyword("_ALPHATEST_ON");
            partMaterial.EnableKeyword("_ALPHABLEND_ON");
            partMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            Color c = partMaterial.color;
            c.a = transparentColor.a;
            partMaterial.color = c;
        }
        else
        {
            partMaterial.SetFloat("_Mode", 0); // Opaque
            partMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            partMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            partMaterial.SetInt("_ZWrite", 1);
            partMaterial.DisableKeyword("_ALPHABLEND_ON");
            partMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;

            Color c = partMaterial.color;
            c.a = 1f;
            partMaterial.color = c;
        }
    }
}
