using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform camTransform;

    void Start() => camTransform = Camera.main.transform;

    void LateUpdate()
    {
        // Make the UI face the camera but keep it upright
        transform.LookAt(transform.position + camTransform.forward);
    }
}