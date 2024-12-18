using UnityEngine;

public class test : MonoBehaviour
{
    [SerializeField] private float speed = 1.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.position = new Vector3(2, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Hello World");
    }
}
