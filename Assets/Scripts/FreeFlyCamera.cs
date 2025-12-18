using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float lookSpeed = 3f;

    float rotationX = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Muis kijken
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed * 100 * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed * 100 * Time.deltaTime;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.parent.Rotate(Vector3.up * mouseX);

        // Bewegen
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 dir = transform.parent.forward * v + transform.parent.right * h;
        transform.parent.position += dir * moveSpeed * Time.deltaTime;
    }
}
