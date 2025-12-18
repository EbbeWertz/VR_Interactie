using UnityEngine;
using System.Collections.Generic;

public class SphereGroupsSelector : MonoBehaviour
{
    [Header("Movement & Scaling")]
    public float moveSpeed = 10f;
    public float scaleSpeed = 5f;
    public float minRadius = 0.5f;

    [Header("Selection Settings")]
    public float selectionRadius = 3f;
    public float movementThreshold = 0.1f;
    public KeyCode lockKey = KeyCode.Space;

    [Header("Selected")]
    public bool enableVibration = true;

    [Header("UI Integration")]
    public Uigroups uiManager; 

    private Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>();
    private Vector3 lastPosition;
    private float lastRadius;
    private List<GameObject> lastCompletedList = new List<GameObject>();

    private void Start()
    {
        lastPosition = transform.position;
        lastRadius = selectionRadius;
        UpdateSphereVisuals();
        UpdateSelectionDisplay();
    }

    private void Update()
    {
        HandleInput();

        if (Vector3.Distance(transform.position, lastPosition) >= movementThreshold || Mathf.Abs(selectionRadius - lastRadius) > 0.01f)
        {
            lastPosition = transform.position;
            lastRadius = selectionRadius;
            UpdateSphereVisuals();
            UpdateSelectionDisplay();
        }
    }

    private void HandleInput()
    {
        Vector3 moveDir = Vector3.zero;
        if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.W)) moveDir += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) moveDir += Vector3.back;
        if (Input.GetKey(KeyCode.D)) moveDir += Vector3.right;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.A)) moveDir += Vector3.left;
        if (Input.GetKey(KeyCode.E)) moveDir += Vector3.up;
        if (Input.GetKey(KeyCode.F)) moveDir += Vector3.down;

        transform.position += moveDir.normalized * moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.O)) selectionRadius += scaleSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.P)) selectionRadius -= scaleSpeed * Time.deltaTime;
        selectionRadius = Mathf.Max(minRadius, selectionRadius);

        // --- SELECTIE VIA SPATIEBALK ---
        if (Input.GetKeyDown(lockKey))
        {
            if (lastCompletedList.Count > 0)
            {
                TriggerSelection(lastCompletedList[0]);
            }
        }
    }

    private void TriggerSelection(GameObject target)
    {
        // Multimodale Feedback: Trilling
        if (enableVibration)
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
            // Voor PC/Editor met controllers zou je hier de InputSystem Haptics kunnen aanroepen
        }

        Debug.Log("<color=orange>Selectie bevestigd voor: " + target.name + "</color>");
    }
    
    private void UpdateSphereVisuals() => transform.localScale = Vector3.one * (selectionRadius * 2);

    public void UpdateSelectionDisplay()
    {
        ResetHighlighting();
        
        List<GameObject> allDetected = GetObjectsInSphere();
        if (allDetected.Count == 0)
        {
            lastCompletedList.Clear();
            if (uiManager != null) uiManager.UpdateExplorerList(new List<GameObject>(), new List<GameObject>());
            return;
        }

        HashSet<GameObject> objectsInSphere = new HashSet<GameObject>(allDetected);
        List<GameObject> completedForUI = new List<GameObject>();
        List<GameObject> missingForUI = new List<GameObject>();

        // Focus bepalen op basis van de hiërarchie
        Transform firstObj = allDetected[0].transform;
        Transform activeLevel = GetHighestCompleteLevel(firstObj, objectsInSphere);
        Transform parentOfLevel = activeLevel.parent;

        if (parentOfLevel != null)
        {
            foreach (Transform child in parentOfLevel)
            {
                if (IsTransformComplete(child, objectsInSphere))
                {
                    completedForUI.Add(child.gameObject);
                    ApplyHighlightToHierarchy(child.gameObject, Color.black);
                }
                else
                {
                    missingForUI.Add(child.gameObject);
                    ApplyHighlightToHierarchy(child.gameObject, Color.red);
                    HighlightDetectedSubObjects(child, objectsInSphere);
                }
            }
        }
        else
        {
            completedForUI.Add(activeLevel.gameObject);
            ApplyHighlightToHierarchy(activeLevel.gameObject, Color.black);
        }

        // Update de interne lijst voor de spatiebalk-actie
        lastCompletedList = new List<GameObject>(completedForUI);

        if (uiManager != null)
        {
            uiManager.UpdateExplorerList(completedForUI, missingForUI);
        }
    }

    private Transform GetHighestCompleteLevel(Transform current, HashSet<GameObject> inSphere)
    {
        if (current.parent == null) return current;
        if (IsTransformComplete(current.parent, inSphere))
        {
            return GetHighestCompleteLevel(current.parent, inSphere);
        }
        return current;
    }

    private bool IsTransformComplete(Transform target, HashSet<GameObject> inSphere)
    {
        Renderer[] childRenderers = target.GetComponentsInChildren<Renderer>();
        if (childRenderers.Length == 0) return inSphere.Contains(target.gameObject);

        int renderersFound = 0;
        foreach (Renderer r in childRenderers)
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            renderersFound++;
            if (!inSphere.Contains(r.gameObject)) return false;
        }
        return renderersFound > 0;
    }

    private void HighlightDetectedSubObjects(Transform root, HashSet<GameObject> inSphere)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            if (inSphere.Contains(r.gameObject))
            {
                ApplyHighlight(r.gameObject, Color.green);
            }
        }
    }

    private void ApplyHighlightToHierarchy(GameObject root, Color col)
    {
        Renderer r = root.GetComponent<Renderer>();
        if (r != null) ApplyHighlight(root, col);
        foreach (Renderer childRend in root.GetComponentsInChildren<Renderer>())
        {
            ApplyHighlight(childRend.gameObject, col);
        }
    }

    private List<GameObject> GetObjectsInSphere()
    {
        List<GameObject> results = new List<GameObject>();
        Collider[] hits = Physics.OverlapSphere(transform.position, selectionRadius + 0.05f);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (IsMeshCompletelyContained(hit.gameObject, transform.position, selectionRadius))
                results.Add(hit.gameObject);
        }
        return results;
    }

    private void ApplyHighlight(GameObject obj, Color color)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;
        if (!originalColors.ContainsKey(obj)) originalColors[obj] = r.material.color;
        
        if (color == Color.black) { r.material.color = Color.black; }
        else if (color == Color.green) { if (r.material.color != Color.black) r.material.color = Color.green; }
        else if (color == Color.red) { if (r.material.color != Color.black && r.material.color != Color.green) r.material.color = Color.red; }
    }

    public void ResetHighlighting()
    {
        foreach (var pair in originalColors)
        {
            if (pair.Key != null)
            {
                Renderer r = pair.Key.GetComponent<Renderer>();
                if (r != null) r.material.color = pair.Value;
            }
        }
        originalColors.Clear();
    }

    private bool IsMeshCompletelyContained(GameObject obj, Vector3 center, float radius)
    {
        MeshFilter mf = obj.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return false;
        float rSq = (radius * 1.01f) * (radius * 1.01f);
        foreach (Vector3 v in mf.sharedMesh.vertices)
        {
            if ((obj.transform.TransformPoint(v) - center).sqrMagnitude > rSq) return false;
        }
        return true;
    }

    private void OnDestroy() => ResetHighlighting();
}