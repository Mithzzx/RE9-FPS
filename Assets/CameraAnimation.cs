using System.Collections;
using Player;
using UnityEngine;

public class CameraAnimation : MonoBehaviour
{
    [SerializeField] MovementController movementController;
    [SerializeField] InputController inputController;
    [SerializeField] GameObject head;
    [SerializeField] GameObject stand;
    private bool currentCrouchState ;
    [SerializeField] float coolDown = 0.5f;
    [SerializeField] float transitionDuration = 0.5f;
    
    void Update()
    {
        if (inputController.Crouch())
        {
            currentCrouchState = true;
            transform.position = head.transform.position;
        }
        else
        {
            //Change position to stand immediately if player is moving
            if (currentCrouchState && (inputController.Movement().x > 0.01f || inputController.Movement().y > 0.01f))
            {
                transform.position = stand.transform.position;
                currentCrouchState = false;
            }
            else
            {
                if (currentCrouchState)
                {
                    transform.position = head.transform.position;
                    movementController.canMove = false;
                    StartCoroutine(ChangePositionAfterCooldown());
                    StartCoroutine(EnableAfterCooldownCoroutine());
                }
            }
        }
    }

    private IEnumerator ChangePositionAfterCooldown()
    {
        yield return new WaitForSeconds(coolDown);
        SmoothMove(transform.position, stand.transform.position);
        currentCrouchState = false;
    }

    public void SmoothMove(Vector3 startPosition, Vector3 endPosition)
    {
        StartCoroutine(SmoothMoveCoroutine(startPosition, endPosition));
    }

    private IEnumerator SmoothMoveCoroutine(Vector3 startPosition, Vector3 endPosition)
    {
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / transitionDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
    }
    private IEnumerator EnableAfterCooldownCoroutine()
    {
        yield return new WaitForSeconds(coolDown+transitionDuration+1f);
        movementController.canMove = true;
    }
}