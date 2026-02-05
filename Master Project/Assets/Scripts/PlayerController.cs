using UnityEngine;

public class PlayerController : MonoBehaviour
{
    float currentSpeed;
    [SerializeField] float speed;
    [SerializeField] float sensitivity;
    [SerializeField] float speedMultiplier;
    Vector3 movementDirection = Vector3.zero;
    float mouseX;
    float mouseY;
    float horizontal, vertical;
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        currentSpeed = speed;
    }
    private void Update()
    {
        mouseX += Input.GetAxis("Mouse X") * sensitivity;
        mouseX = Mathf.Repeat(mouseX, 360);
        mouseY -= Input.GetAxis("Mouse Y") * sensitivity;
        mouseY = Mathf.Clamp(mouseY, -90, 90);

        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        movementDirection = transform.forward * vertical + transform.right * horizontal;
        if (Input.GetKey(KeyCode.Space))
        {
            movementDirection += Vector3.up;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            movementDirection -= Vector3.up;
        }

        currentSpeed = Input.GetKey(KeyCode.LeftShift) ? speed * speedMultiplier : speed;

        transform.SetLocalPositionAndRotation(transform.position + currentSpeed * Time.deltaTime * movementDirection.normalized, Quaternion.Euler(mouseY, mouseX, 0));
    }
}
