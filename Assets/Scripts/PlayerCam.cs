using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    [Header("Input References")]
    [SerializeField] public InputHandler input;
    
    [Header("Sensitivity")]
    [SerializeField] public float sensX = 10f;
    [SerializeField] public float sensY = 10f;
    
    
    public Transform orientation;
    
    float xRotation;
    float yRotation;
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        float mouseX = input.LookInput.x * sensX * Time.deltaTime;
        float mouseY = input.LookInput.y * sensY * Time.deltaTime;
        
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 75f);
        
        // Rotate the camera and the player's orientation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
        
    }
}
