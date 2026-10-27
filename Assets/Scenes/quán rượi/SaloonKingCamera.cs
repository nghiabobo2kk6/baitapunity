
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SaloonKingCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform king;
    [SerializeField] private SpriteRenderer background;

    [Header("Camera Settings")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float offsetX = 0f;

    private Camera cam;
    private float fixedY;
    private float fixedZ;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (king == null || background == null || cam == null)
            return;

        Bounds bounds = background.bounds;

        float halfWidth = cam.orthographicSize * cam.aspect;

        float minCameraX = bounds.min.x + halfWidth;
        float maxCameraX = bounds.max.x - halfWidth;

        float targetX = king.position.x + offsetX;

        // If the background is narrower than the camera view,
        // keep the camera centered on the background.
        if (minCameraX > maxCameraX)
        {
            targetX = bounds.center.x;
        }
        else
        {
            targetX = Mathf.Clamp(
                targetX,
                minCameraX,
                maxCameraX
            );
        }

        Vector3 targetPosition = new Vector3(
            targetX,
            fixedY,
            fixedZ
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
