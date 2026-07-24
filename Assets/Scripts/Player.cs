using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private AgentMover AgentMover;

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
    }

    void Update()
    {
        movementInput = movement.action.ReadValue<Vector2>();
        AgentMover.MovementInput = movementInput;
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        // 전체용 Canvas 혹은 프리팹이 등록되지 않았다면 예외 처리
        if (globalCanvasTransform == null || clickEffectPrefab == null)
        {
            Debug.LogWarning("[Player] globalCanvasTransform 또는 clickEffectPrefab이 Inspector에서 누락되었습니다.");
            return;
        }

        // 2D에서는 오소그래픽 카메라 화면 좌표가 바로 월드 좌표로 변환되므로
        // 3D 레이캐스트로 "바닥에 맞았는지" 확인할 필요가 없다 - 클릭 시 곧바로 이펙트 생성
        Vector2 screenPos = pointerPosition.action.ReadValue<Vector2>();
        SpawnUIOverlayEffect(screenPos);
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
