using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private AgentMover AgentMover;
    private CardAutoAttack cardAutoAttack;

    private Vector2 movementInput;

    [SerializeField]
    private InputActionReference movement, shoot, pointerPosition;

    [Header("클릭 이펙트 및 UI 확장성 설정")]
    [Tooltip("씬에 배치된 '전체용 UI Canvas'의 RectTransform을 드래그해서 넣어주세요.")]
    [SerializeField] private RectTransform globalCanvasTransform;

    [Tooltip("Canvas 컴포넌트가 제거된, 일반 RectTransform 기반의 이펙트 UI 조각 프리팹")]
    [SerializeField] private GameObject clickEffectPrefab;

    [Tooltip("이펙트가 화면에 유지될 시간 (1초 권장)")]
    [SerializeField] private float effectDestroyTime = 1.0f;

    [Header("타겟 락온 VFX")]
    [Tooltip("적을 클릭해서 우선 타겟으로 지정했을 때 그 적 위에 표시할 락온 이펙트 프리팹")]
    [SerializeField] private GameObject targetLockVfxPrefab;

    private void OnEnable()
    {
        if (shoot != null && shoot.action != null)
        {
            shoot.action.performed += OnShootPerformed;
        }
    }

    private void OnDisable()
    {
        if (shoot != null && shoot.action != null)
        {
            shoot.action.performed -= OnShootPerformed;
        }
    }

    void Awake()
    {
        AgentMover = GetComponent<AgentMover>();
        cardAutoAttack = GetComponent<CardAutoAttack>();
    }

    void Update()
    {
        movementInput = movement.action.ReadValue<Vector2>();
        AgentMover.MovementInput = movementInput;
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        Vector2 screenPos = pointerPosition.action.ReadValue<Vector2>();

        // 클릭한 화면 위치에 적이 있으면 그 적을 우선 타겟으로 지정 (위험도 높은 적 수동 집중공격용).
        // 적이 없는 빈 곳을 클릭하면 우선 타겟을 해제하고 자동 타겟팅으로 되돌린다.
        TrySetClickedEnemyAsTarget(screenPos);

        // 전체용 Canvas 혹은 프리팹이 등록되지 않았다면 예외 처리
        if (globalCanvasTransform == null || clickEffectPrefab == null)
        {
            Debug.LogWarning("[Player] globalCanvasTransform 또는 clickEffectPrefab이 Inspector에서 누락되었습니다.");
            return;
        }

        // 2D에서는 오소그래픽 카메라 화면 좌표가 바로 월드 좌표로 변환되므로
        // 3D 레이캐스트로 "바닥에 맞았는지" 확인할 필요가 없다 - 클릭 시 곧바로 이펙트 생성
        SpawnUIOverlayEffect(screenPos);
    }

    /// <summary>클릭한 화면 좌표를 월드 좌표로 변환해서 그 지점에 있는 콜라이더를 찾고, "Enemy" 태그면
    /// CardAutoAttack의 우선 타겟으로 지정한다. 아무것도 없으면(빈 땅 클릭) 우선 타겟을 해제한다.</summary>
    private void TrySetClickedEnemyAsTarget(Vector2 screenPosition)
    {
        if (cardAutoAttack == null || Camera.main == null)
            return;

        float camDistance = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, camDistance));
        worldPos.z = 0f;

        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        bool clickedEnemy = hit != null && hit.CompareTag("Enemy");

        cardAutoAttack.SetManualTarget(clickedEnemy ? hit.transform : null);

        if (clickedEnemy && targetLockVfxPrefab != null)
            Instantiate(targetLockVfxPrefab, hit.transform.position, Quaternion.identity, hit.transform);
    }

    private void SpawnUIOverlayEffect(Vector2 screenPosition)
    {
        // 핵심: 무거운 Canvas를 또 만드는 것이 아니라, 기존 globalCanvasTransform의 자식(Parent)으로 인스턴스화합니다.
        GameObject effectInstance = Instantiate(clickEffectPrefab, globalCanvasTransform);

        // 생성된 UI 조각의 위치를 조절하기 위해 RectTransform 컴포넌트를 가져옴
        RectTransform rect = effectInstance.GetComponent<RectTransform>();
        if (rect != null)
        {
            // 스크린 터치 좌표를 전체용 캔버스의 로컬 배치의 절대적 월드(스크린) 스페이스 좌표로 즉시 일치시킵니다.
            rect.position = screenPosition;
        }

        // 1초 뒤 자동 파괴
        Destroy(effectInstance, effectDestroyTime);
    }
}
