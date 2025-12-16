using UnityEngine;

public class SphereMover : MonoBehaviour
{
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
    }
}