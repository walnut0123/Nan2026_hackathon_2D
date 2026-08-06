using UnityEngine;

/// <summary>Bridges movement/combat state into the Animator without touching EnemyAIPathMover/Health
/// directly. Speed is derived from frame-to-frame Transform position delta - reads transform.position
/// directly rather than Rigidbody2D.position so it works regardless of what actually moves the enemy
/// (AIPath writes transform.position directly, not through rigidbody velocity/MovePosition).
/// Hit/Die triggers come from Health's events.</summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour
{
    [Tooltip("Speed 파라미터에 적용할 감쇠(부드럽게 변화). 0에 가까울수록 즉각 반응.")]
    [SerializeField] private float speedSmoothing = 0.2f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Health health;

    private Vector2 lastPosition;
    private float smoothedSpeed;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        lastPosition = transform.position;

        if (health != null)
        {
            health.OnDamaged += HandleDamaged;
            health.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDeath -= HandleDeath;
        }
    }

    private void Update()
    {
        Vector2 currentPosition = transform.position;
        Vector2 delta = currentPosition - lastPosition;
        float instantSpeed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, instantSpeed, speedSmoothing);

        animator.SetFloat("Speed", smoothedSpeed);

        if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            spriteRenderer.flipX = delta.x > 0f;

        lastPosition = currentPosition;
    }

    private void HandleDamaged(float amount)
    {
        animator.SetTrigger("Hit");
    }

    private void HandleDeath()
    {
        animator.SetTrigger("Die");

        var chaser = GetComponent<EnemyAIPathMover>();
        if (chaser != null)
            chaser.enabled = false;

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }
}
