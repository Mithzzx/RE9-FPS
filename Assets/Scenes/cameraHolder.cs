using UnityEngine;

public class CameraHolder : MonoBehaviour
{
    [SerializeField] private Transform camPos;

    private void Update()
    {
        transform.position = camPos.position;
    }
}
