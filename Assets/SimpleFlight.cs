using UnityEngine;

public class SimpleFlight : MonoBehaviour
{
    public float speed = 3.0f;
    public Transform centerEyeAnchor; // Drag 'CenterEyeAnchor' here in Inspector

    void Update()
    {
        // 1. Get the joystick input (PrimaryThumbstick is usually Left Hand)
        Vector2 joystickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        // 2. Calculate direction based on where the headset is looking
        Vector3 moveDirection = (centerEyeAnchor.forward * joystickInput.y) + (centerEyeAnchor.right * joystickInput.x);

        // 3. Move the entire Rig
        transform.position += moveDirection * speed * Time.deltaTime;
    }
}