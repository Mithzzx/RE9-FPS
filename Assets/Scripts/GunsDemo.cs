using UnityEngine;

public class GunsDemo : MonoBehaviour
{
    [Header("Keys")]
    [SerializeField] private KeyCode nextKey = KeyCode.Period;
    [SerializeField] private KeyCode previousKey = KeyCode.Comma;
    
    [Header("Guns")]
    [SerializeField] private GameObject[] guns;
    [SerializeField] private GameObject[] arms;
    [SerializeField] private GameObject[] bulletHoles;
    
    int currentGunIndex = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < guns.Length; i++)
        {
            if (guns[i].activeSelf)
            {
                currentGunIndex = i;
                return;
            }
        }
        currentGunIndex = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(nextKey))
        {
            guns[currentGunIndex].SetActive(false);
            arms[currentGunIndex].SetActive(false);
            currentGunIndex++;
            if (currentGunIndex >= guns.Length) currentGunIndex = 0;
            guns[currentGunIndex].SetActive(true);
            arms[currentGunIndex].SetActive(true);
        }
        else if (Input.GetKeyDown(previousKey))
        {
            guns[currentGunIndex].SetActive(false);
            arms[currentGunIndex].SetActive(false);
            currentGunIndex--;
            if (currentGunIndex < 0) currentGunIndex = guns.Length - 1;
            guns[currentGunIndex].SetActive(true);
            arms[currentGunIndex].SetActive(true);
        }
    }
    
    public GameObject GetBulletHole(string tag)
    {
        if (tag == "Enemy")
        {
            return bulletHoles[1];
        }
        return bulletHoles[0];
    }
}
