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

        if (Input.GetKeyDown(lockKey))
        {
            if (lockedRoot != null) lockedRoot = null;
            else
            {
                List<GameObject> currentInSphere = GetObjectsInSphere(true); 
                if (currentInSphere.Count > 0)
                {
                    lockedRoot = FindGroupRoot(currentInSphere[0].transform);
                }
            }
            UpdateSelectionDisplay();
        }
    }
    
    private void UpdateSphereVisuals() => transform.localScale = Vector3.one * (selectionRadius * 2);

    public void UpdateSelectionDisplay()
    {
        ResetHighlighting();
        
        List<GameObject> allDetected = GetObjectsInSphere(false);
        if (allDetected.Count == 0)
        {
            if (uiManager != null) uiManager.UpdateExplorerList(new List<GameObject>(), new List<GameObject>());
            return;
        }

        HashSet<GameObject> objectsInSphere = new HashSet<GameObject>(allDetected);
        List<GameObject> completedForUI = new List<GameObject>();
        List<GameObject> missingForUI = new List<GameObject>();

        // 1. Pak het eerste object als referentiepunt voor de huidige actieve groep
        Transform firstObj = allDetected[0].transform;
        
        // 2. Vind het hoogste niveau dat VOLLEDIG compleet is
        Transform activeLevel = GetHighestCompleteLevel(firstObj, objectsInSphere);

        // 3. Als dit niveau compleet is, maar de parent erboven NIET, 
        //    dan tonen we de siblings van dit niveau.
        Transform parentOfLevel = activeLevel.parent;

        if (parentOfLevel != null)
        {
            foreach (Transform child in parentOfLevel)
            {
                if (IsTransformComplete(child, objectsInSphere))
                {
                    completedForUI.Add(child.gameObject);
                    ApplyHighlightToHierarchy(child.gameObject, Color.green);
                }
                else
                {
                    missingForUI.Add(child.gameObject);
                    ApplyHighlightToHierarchy(child.gameObject, Color.red);
                    
                    // Specifiek voor de objecten die WEL in de sphere zitten maar wiens groep incompleet is:
                    // Die kleuren we groen bovenop het rood van de groep.
                    HighlightDetectedSubObjects(child, objectsInSphere);
                }
            }
        }
        else
        {
            // We zitten op de absolute root
            completedForUI.Add(activeLevel.gameObject);
            ApplyHighlightToHierarchy(activeLevel.gameObject, Color.black);
        }

        if (uiManager != null)
        {
            uiManager.UpdateExplorerList(completedForUI, missingForUI);
        }
    }

    // Klimt omhoog zolang de VOLLEDIGE parent-groep compleet is
    private Transform GetHighestCompleteLevel(Transform current, HashSet<GameObject> inSphere)
    {
        if (current.parent == null) return current;
        if (lockedRoot != null && current == lockedRoot) return current;

        // Check of ALLE kinderen van de parent in de sphere zitten
        if (IsTransformComplete(current.parent, inSphere))
        {
            return GetHighestCompleteLevel(current.parent, inSphere);
        }
        
        // Als de parent niet compleet is, is dit huidige object (of zijn groep) het actieve niveau
        return current;
    }

    private bool IsTransformComplete(Transform target, HashSet<GameObject> inSphere)
    {
        Renderer[] childRenderers = target.GetComponentsInChildren<Renderer>();
        if (childRenderers.Length == 0) return inSphere.Contains(target.gameObject);

        foreach (Renderer r in childRenderers)
        {
            if (!inSphere.Contains(r.gameObject)) return false;
        }
        return true;
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

    private List<GameObject> GetObjectsInSphere(bool ignoreLock)
    {
        List<GameObject> results = new List<GameObject>();
        Collider[] hits = Physics.OverlapSphere(transform.position, selectionRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (!ignoreLock && lockedRoot != null && !hit.transform.IsChildOf(lockedRoot)) continue;

            if (IsMeshCompletelyContained(hit.gameObject, transform.position, selectionRadius))
                results.Add(hit.gameObject);
        }
        return results;
    }

    private Transform FindGroupRoot(Transform child)
    {
        Transform current = child;
        while (current.parent != null && current.parent.parent != null)
            current = current.parent;
        return current;
    }

    private void ApplyHighlight(GameObject obj, Color color)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;
        if (!originalColors.ContainsKey(obj)) originalColors[obj] = r.material.color;
        
        // Prioriteit: Zwart > Groen > Rood
        if (color == Color.black) { r.material.color = color; return; }
        if (color == Color.green && r.material.color != Color.black) { r.material.color = color; return; }
        if (color == Color.red && r.material.color != Color.black && r.material.color != Color.green) { r.material.color = color; }
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