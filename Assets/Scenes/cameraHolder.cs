using UnityEngine;

public class cameraHolder : MonoBehaviour
{
    [SerializeField] private Transform camPos;

    private void Update()
    {
        transform.position = camPos.position;
    }
}
