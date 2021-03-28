using UnityEngine;

public class CameraController : MonoBehaviour {
    private float moveSpeed = 0.25f;
    private float scrollSpeed = 0.67f;

    float horizontalInput;
    float verticalInput;
    float wheelInput;

    void Update() {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        wheelInput = Input.GetAxis("Mouse ScrollWheel");
    }

    void FixedUpdate() {
        if (Input.GetAxisRaw("Horizontal") != 0 || verticalInput != 0) {
            transform.position += moveSpeed * new Vector3(horizontalInput, verticalInput, 0);
        }

        if (Input.GetAxis("Mouse ScrollWheel") != 0) {
            transform.position += scrollSpeed * new Vector3(0, 0, Input.GetAxis("Mouse ScrollWheel"));
        }
    }
}