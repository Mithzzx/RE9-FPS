using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    [SerializeField] private float sensX;
    [SerializeField] private float sensY;
    
    [SerializeField] private InputHandler input;
    [SerializeField] private Transform orientation;
    
    private float xRotation;
    private float yRotation;
    
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void Update()
    {
        float mouseX = input.LookInput.x * sensX * Time.deltaTime;
        float mouseY = input.LookInput.y * sensY * Time.deltaTime;
        
        xRotation -= mouseY;
        yRotation += mouseX;
        
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation= Quaternion.Euler(0f, yRotation, 0f);
    } 
}
