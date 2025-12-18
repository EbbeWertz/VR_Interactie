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

    [Header("UI Integration")]
    // PAS OP: Verander dit naar UIGroups als je bestand zo heet!
    public Uigroups uiManager; 

    private Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>();
    private Vector3 lastPosition;
    private float lastRadius;

    private Transform lockedRoot = null;

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

        // Check of we moeten updaten
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

        // --- LOCKING LOGIC ---
        // Inside HandleInput(), update the lock logic:
        if (Input.GetKeyDown(lockKey))
        {
            if (lockedRoot != null)
            {
                lockedRoot = null;
            }
            else
            {
                List<GameObject> currentInSphere = GetObjectsInSphere(true); 
                if (currentInSphere.Count > 0)
                {
                    // Use the new method here
                    lockedRoot = FindGroupRoot(currentInSphere[0].transform);
                    Debug.Log("Locked to Group: " + lockedRoot.name);
                }
            }
            UpdateSelectionDisplay();
        }
    }
    
    private void UpdateSphereVisuals() => transform.localScale = Vector3.one * (selectionRadius * 2);

    public void UpdateSelectionDisplay()
    {
        ResetHighlighting();
        
        HashSet<GameObject> objectsInSphere = new HashSet<GameObject>(GetObjectsInSphere(false));
        HashSet<GameObject> completedObjects = new HashSet<GameObject>();
        HashSet<GameObject> missingRequiredObjects = new HashSet<GameObject>();

        foreach (GameObject obj in objectsInSphere)
        {
            ProcessHierarchy(obj, objectsInSphere, completedObjects, missingRequiredObjects);
        }

        if (uiManager != null)
        {
            // Zorg dat deze methode in UIGroups bestaat met 2 lijsten als parameters
            uiManager.UpdateExplorerList(new List<GameObject>(completedObjects), new List<GameObject>(missingRequiredObjects));
        }
    }

    private void ProcessHierarchy(GameObject obj, HashSet<GameObject> inSphere, HashSet<GameObject> completed, HashSet<GameObject> missing)
    {
        Transform current = obj.transform;
        
        while (current != null)
        {
            // Stop processing if we hit a parent that is NOT our locked root
            if (lockedRoot != null && !current.IsChildOf(lockedRoot)) break;

            bool allChildrenInSphere = true;
            List<GameObject> currentMissing = new List<GameObject>();
            
            foreach (Transform child in current)
            {
                if (!inSphere.Contains(child.gameObject))
                {
                    allChildrenInSphere = false;
                    currentMissing.Add(child.gameObject);
                }
            }

            if (!inSphere.Contains(current.gameObject))
            {
                allChildrenInSphere = false;
                currentMissing.Add(current.gameObject);
            }

            if (allChildrenInSphere)
            {
                Color targetColor = (current.parent == null || current == lockedRoot) ? Color.black : Color.green;
                ApplyHighlight(current.gameObject, targetColor);
                completed.Add(current.gameObject);

                foreach (Transform child in current)
                {
                    ApplyHighlight(child.gameObject, targetColor);
                    completed.Add(child.gameObject);
                }
                current = current.parent;
            }
            else
            {
                if (inSphere.Contains(current.gameObject))
                {
                    ApplyHighlight(current.gameObject, Color.green);
                    completed.Add(current.gameObject);
                }

                foreach (GameObject m in currentMissing)
                {
                    ApplyHighlight(m, Color.red);
                    missing.Add(m);
                }
                break;
            }
        }
    }

    private List<GameObject> GetObjectsInSphere(bool ignoreLock)
    {
        List<GameObject> results = new List<GameObject>();
        Collider[] hits = Physics.OverlapSphere(transform.position, selectionRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            if (!ignoreLock && lockedRoot != null)
            {
                if (!hit.transform.IsChildOf(lockedRoot)) continue;
            }

            if (IsMeshCompletelyContained(hit.gameObject, transform.position, selectionRadius))
                results.Add(hit.gameObject);
        }
        return results;
    }

    // Helper to find the top-level parent of a group
    private Transform FindHighestParent(Transform child)
    {
        Transform current = child;
        while (current.parent != null)
        {
            current = current.parent;
        }
        return current;
    }

    private Transform FindGroupRoot(Transform child)
    {
        Transform current = child;
    
        // We climb up the ladder as long as there is a "Grandparent".
        // This ensures we stop at the child of the absolute root.
        while (current.parent != null && current.parent.parent != null)
        {
            current = current.parent;
        }
    
        return current;
    }

    private void ApplyHighlight(GameObject obj, Color color)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;
        if (!originalColors.ContainsKey(obj)) originalColors[obj] = r.material.color;

        if (r.material.color == Color.black) return; 
        if (r.material.color == Color.green && color == Color.red) return;

        r.material.color = color;
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
        float rSq = radius * radius;
        foreach (Vector3 v in mf.sharedMesh.vertices)
        {
            if ((obj.transform.TransformPoint(v) - center).sqrMagnitude > rSq) return false;
        }
        return true;
    }

    private void OnDestroy() => ResetHighlighting();
}