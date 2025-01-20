using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    [SerializeField] private float sensX;
    [SerializeField] private float sensY;
    
    [Header("Fps")]
    [SerializeField] private int fps = 60;
    
    [Header("References")]
    [SerializeField] private InputHandler input;
    [SerializeField] private Transform orientation;
    
    public float xRotation;
    public float yRotation;
    
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Application.targetFrameRate = fps;
    }
    
    private void Update()
    {
        float mouseX = input.LookInput.x * sensX * Time.deltaTime;
        float mouseY = input.LookInput.y * sensY * Time.deltaTime;
        
        xRotation -= mouseY;
        yRotation += mouseX;
        
        xRotation = Mathf.Clamp(xRotation, -90f, 80f);
        
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation= Quaternion.Euler(0f, yRotation, 0f);
    } 
}
