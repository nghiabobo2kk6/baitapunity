using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    public float moveSpeed = 0.3f;
    public float moveDuration = 4f;

    private float timer;
    private bool movingRight = true;

    void Update()
    {
        timer += Time.deltaTime;

        float direction = movingRight ? 1f : -1f;

        transform.position += Vector3.right
            * direction
            * moveSpeed
            * Time.deltaTime;

        if (timer >= moveDuration)
        {
            timer = 0f;
            movingRight = !movingRight;
        }
    }
}