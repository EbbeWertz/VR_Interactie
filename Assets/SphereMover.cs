using UnityEngine;

public class SphereMover : MonoBehaviour
{   
    [Header("Movement Control")]
    [Tooltip("Movement speed in Unity units per second.")]
    public float speed = 10f;

    [Tooltip("Key for moving along the Positive X-axis (Right).")]
    public KeyCode moveRight = KeyCode.D;
    [Tooltip("Key for moving along the Negative X-axis (Left).")]
    public KeyCode moveLeft = KeyCode.Q;

    [Tooltip("Key for moving along the Positive Z-axis (Forward).")]
    public KeyCode moveForward = KeyCode.Z;
    [Tooltip("Key for moving along the Negative Z-axis (Backward).")]
    public KeyCode moveBackward = KeyCode.S;

    [Tooltip("Key for moving along the Positive Y-axis (Up).")]
    public KeyCode moveUp = KeyCode.E;
    [Tooltip("Key for moving along the Negative Y-axis (Down).")]
    public KeyCode moveDown = KeyCode.A;

    [Header("Radius Control")]
    [Tooltip("Rate at which the radius changes per second.")]
    public float scaleSpeed = 5f;

    [Tooltip("Key to increase the sphere's radius.")]
    public KeyCode increaseRadiusKey = KeyCode.O; // Or KeyCode.KeypadPlus
    
    [Tooltip("Key to decrease the sphere's radius.")]
    public KeyCode decreaseRadiusKey = KeyCode.P; // Or KeyCode.KeypadMinus

    [Tooltip("The minimum allowed radius.")]
    public float minRadius = 1f;
    
    private SphereSelector sphereSelector;
    private float currentRadius;

    private void Start()
    {
        // Get a reference to the SphereSelector script to access the radius variable
        sphereSelector = GetComponent<SphereSelector>();
        if (sphereSelector == null)
        {
            Debug.LogError("SphereMover requires a SphereSelector component on the same GameObject!");
            enabled = false;
        }
        
        // Initialize the current radius from the selector
        currentRadius = sphereSelector.selectionRadius;
    }
    private void Update()
    {
        Vector3 moveDirection = Vector3.zero;
        
        // Horizontal Movement (X and Z)
        if (Input.GetKey(moveForward))
            moveDirection += transform.forward;
        if (Input.GetKey(moveBackward))
            moveDirection -= transform.forward;
        if (Input.GetKey(moveRight))
            moveDirection += transform.right;
        if (Input.GetKey(moveLeft))
            moveDirection -= transform.right;

        // Vertical Movement (Y)
        if (Input.GetKey(moveUp))
            moveDirection += Vector3.up;
        if (Input.GetKey(moveDown))
            moveDirection -= Vector3.up;

        // Normalize direction vector to prevent faster diagonal movement
        if (moveDirection.magnitude > 1)
        {
            moveDirection.Normalize();
        }

        // Apply movement using Time.deltaTime for frame rate independence
        transform.position += moveDirection * speed * Time.deltaTime;

        // 2. Handle Radius Scaling
        float scaleChange = 0f;
        
        if (Input.GetKey(increaseRadiusKey))
        {
            scaleChange += scaleSpeed * Time.deltaTime;
        }
        if (Input.GetKey(decreaseRadiusKey))
        {
            scaleChange -= scaleSpeed * Time.deltaTime;
        }

        // Apply scaling change if any key was pressed
        if (scaleChange != 0f)
        {
            // Calculate new radius, clamping it to the minimum value
            currentRadius = Mathf.Max(minRadius, currentRadius + scaleChange);

            // Update the public variable on the SphereSelector script
            sphereSelector.selectionRadius = currentRadius;
            Debug.Log($"Updated Sphere Radius to: {currentRadius}");
            
            // NOTE: Because your SphereSelector has an OnValidate() 
            // that links 'selectionRadius' to 'transform.localScale', 
            // the visual scale and the selection check will update automatically!
        }
    }
}