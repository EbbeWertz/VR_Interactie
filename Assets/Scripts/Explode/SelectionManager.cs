using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SelectionManager : MonoBehaviour
{
    [Header("VR References")]
    public Transform rightHandAnchor;
    public LayerMask outlineLayer;
    public float maxRayDistance = 10.0f;

    [Header("UI Hints")]
    public GameObject hintPrefab;
    public Vector3 handOffset = new Vector3(0, 0.15f, 0);
    private GameObject activeHint;

    [Header("Laser Visuals")]
    public Color laserColorDefault = Color.white;
    public Color laserColorHover = Color.green;
    public Color laserColorDisabled = Color.gray; 
    public float laserWidth = 0.01f;

    [Header("Haptics Settings")]
    public float hoverVibrationDuration = 0.05f;
    public float selectVibrationDuration = 0.15f;
    [Range(0, 1)] public float vibrationAmplitude = 0.5f;

    private SelectablePart hoveredPart;
    private SelectablePart selectedPart;
    private LineRenderer laserLine;

    void Awake()
    {
        SetupLaser();
        if (hintPrefab != null)
        {
            activeHint = Instantiate(hintPrefab, rightHandAnchor);
            activeHint.transform.localPosition = handOffset;
            activeHint.SetActive(false);
        }
    }

    void Update()
    {
        HandleHoverAndLaser();
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)) HandleSelection();
        HandleInputs();
        if (activeHint != null && activeHint.activeSelf) UpdateHintRotation();
    }

    private void SetupLaser()
    {
        laserLine = GetComponent<LineRenderer>();
        laserLine.positionCount = 2;
        laserLine.startWidth = laserWidth;
        laserLine.endWidth = laserWidth;
        laserLine.material = new Material(Shader.Find("Sprites/Default"));
    }

    // --- Strict Check: Only true if there's a nested SelectablePart ---
    private bool IsActuallyExplodable(Transform t)
    {
        if (t == null) return false;
        foreach (Transform child in t)
        {
            if (child.GetComponent<SelectablePart>() != null) return true;
        }
        return false;
    }

    void HandleHoverAndLaser()
    {
        Ray ray = new Ray(rightHandAnchor.position, rightHandAnchor.forward);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, outlineLayer);

        laserLine.SetPosition(0, rightHandAnchor.position);

        if (hitSomething)
        {
            laserLine.SetPosition(1, hit.point);
            var part = hit.collider.GetComponent<OutlineSelectable>()?.owner;

            bool canExplode = part != null && IsActuallyExplodable(part.transform);
            laserLine.startColor = laserLine.endColor = canExplode ? laserColorHover : laserColorDisabled;

            if (part != hoveredPart)
            {
                hoveredPart?.GetComponent<PartOutlineHandler>().UpdateVisuals(false);
                hoveredPart = part;
                if (hoveredPart != null)
                {
                    hoveredPart.GetComponent<PartOutlineHandler>().UpdateVisuals(true);
                    TriggerHaptic(hoverVibrationDuration);
                }
            }
        }
        else
        {
            laserLine.SetPosition(1, rightHandAnchor.position + (rightHandAnchor.forward * maxRayDistance));
            laserLine.startColor = laserLine.endColor = laserColorDefault;
            if (hoveredPart != null)
            {
                hoveredPart.GetComponent<PartOutlineHandler>().UpdateVisuals(false);
                hoveredPart = null;
            }
        }
    }

    void HandleSelection()
    {
        Ray ray = new Ray(rightHandAnchor.position, rightHandAnchor.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, outlineLayer))
        {
            TriggerHaptic(selectVibrationDuration);

            var core = hit.collider.GetComponent<ExplosionCore>();
            if (core != null) { core.RequestCollapse(); return; }

            var part = hit.collider.GetComponent<OutlineSelectable>()?.owner;
            if (part != selectedPart)
            {
                if (selectedPart != null)
                {
                    selectedPart.isSelected = false;
                    selectedPart.GetComponent<PartOutlineHandler>().UpdateVisuals(false);
                }

                selectedPart = part;
                if (selectedPart != null)
                {
                    selectedPart.isSelected = true;
                    selectedPart.GetComponent<PartOutlineHandler>().UpdateVisuals(true);
                    activeHint?.SetActive(IsActuallyExplodable(selectedPart.transform));
                }
            }
        }
    }

    void HandleInputs()
    {
        if (selectedPart == null) return;

        if (!IsActuallyExplodable(selectedPart.transform))
        {
            if (activeHint.activeSelf) activeHint.SetActive(false);
            return;
        }

        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            var handler = selectedPart.GetComponent<PartOutlineHandler>();
            selectedPart.isSelected = false;
            handler.UpdateVisuals(false);
            handler.SetColliderActive(false);

            selectedPart.GetComponent<PartExplosionHandler>()?.ToggleExplosion(true);
            activeHint?.SetActive(false);
            selectedPart = null;
        }
    }

    private void UpdateHintRotation() => activeHint.transform.LookAt(activeHint.transform.position + Camera.main.transform.forward);
    private void TriggerHaptic(float duration) { OVRInput.SetControllerVibration(vibrationAmplitude, vibrationAmplitude, OVRInput.Controller.RTouch); Invoke(nameof(StopHaptic), duration); }
    private void StopHaptic() => OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
}