using UnityEngine;

public class CameraHolder : MonoBehaviour
{
    [SerializeField] Transform cameraPosition;
    [SerializeField] bool limitfps = true;
    void Update()
    {
        this.transform.position = cameraPosition.position;
        if (limitfps)
        {
            Application.targetFrameRate = 60;
        }
    }
}
