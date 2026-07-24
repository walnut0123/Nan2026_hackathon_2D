using UnityEngine;

/// <summary>Smoothly follows the player on the XY plane, keeping the camera's own Z (its
/// distance from the 2D scene). Finds the player via PlayerInventory at Start, matching the
/// rest of the codebase's convention of locating the player by component rather than manual
/// scene wiring.</summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float smoothTime = 0.15f;

    private Transform target;
    private Vector3 velocity;

    private void Start()
    {
        var playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
            target = playerInventory.transform;
        else
            Debug.LogWarning("[CameraFollow] No PlayerInventory found in scene; camera will not follow.");
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }
}
