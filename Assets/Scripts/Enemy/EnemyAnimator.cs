using UnityEngine;

/// <summary>Bridges movement/combat state into the Animator without touching EnemyChaser/Health
/// directly. Speed is derived from frame-to-frame Rigidbody2D position delta (kinematic bodies
/// don't auto-update .velocity when moved via MovePosition), and Hit/Die triggers come from
/// Health's events.</summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour
{
    [Tooltip("Speed 파라미터에 적용할 감쇠(부드럽게 변화). 0에 가까울수록 즉각 반응.")]
    [SerializeField] private float speedSmoothing = 0.2f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Health health;

    private Vector2 lastPosition;
    private float smoothedSpeed;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        lastPosition = rb != null ? rb.position : (Vector2)transform.position;

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
        Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 delta = currentPosition - lastPosition;
        float instantSpeed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, instantSpeed, speedSmoothing);

        animator.SetFloat("Speed", smoothedSpeed);

        if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            spriteRenderer.flipX = delta.x > 0f;

        lastPosition = currentPosition;
    }

    private void HandleDamaged(int amount)
    {
        animator.SetTrigger("Hit");
    }

    private void HandleDeath()
    {
        animator.SetTrigger("Die");

        var chaser = GetComponent<EnemyChaser>();
        if (chaser != null)
            chaser.enabled = false;

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }
}
