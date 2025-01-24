using UnityEngine;

public class MeleeMechanics : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private InputHandler input;

    // Update is called once per frame
    void Update()
    {
        if (input.AttackTriggered)
        {
            anim.SetBool("attack", true);
        }
        else
        {
            anim.SetBool("attack", false);
        }
    }
}
