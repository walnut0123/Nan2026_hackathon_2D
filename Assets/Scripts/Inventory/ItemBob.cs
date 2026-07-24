using UnityEngine;

public class ItemBob : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float bobSpeed = 2f;

    private Vector3 basePosition;

    private void Start()
    {
        basePosition = transform.localPosition;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = basePosition + new Vector3(0f, offset, 0f);
    }
}
