using System;
using System.Collections.Generic;
using UnityEngine;

// 데모용 미니맵. 방/복도를 대충 그린 회색 사각형(MinimapContent의 자식)으로 이루어진 "전체 지도"
// 위에서, 플레이어가 있는 방의 중심으로 아이콘을 옮기고 그 방이 뷰포트(이 컴포넌트가 붙은
// RectTransform, RectMask2D로 잘라냄) 가운데에 오도록 콘텐츠를 반대 방향으로 패닝한다.
// 실제 방/복도 아트로 교체될 때까지는 회색 사각형이 그 자리를 대신한다.
public class MinimapController : MonoBehaviour
{
    [Serializable]
    private class RoomMapEntry
    {
        public RoomController room;
        public RectTransform minimapRect;
    }

    [Tooltip("전체 지도를 담는 컨테이너. 플레이어 위치에 맞춰 이 콘텐츠 자체를 패닝한다.")]
    [SerializeField] private RectTransform content;

    [Tooltip("미니맵에 표시할 플레이어 아이콘.")]
    [SerializeField] private RectTransform playerIcon;

    [Tooltip("각 방과, 미니맵 위에서 그 방을 표현하는 사각형(RectTransform)의 매칭 목록.")]
    [SerializeField] private List<RoomMapEntry> roomEntries = new List<RoomMapEntry>();

    private void Start()
    {
        foreach (var entry in roomEntries)
        {
            if (entry.room == null || entry.minimapRect == null)
                continue;

            entry.room.OnPlayerEnteredRoom += () => MoveToRoom(entry);

            // lockOnStart 방(예: Map1)은 자신의 Start()에서 이미 NotifyPlayerEntered를 호출했을 수
            // 있고, 스크립트 실행 순서상 그게 이 Start()보다 먼저 실행되면 위 구독 이전에 이벤트가
            // 지나가버려 아이콘이 시작 방으로 이동하지 못한다. 이미 Idle을 벗어난(=입장 처리가 끝난)
            // 방이 있으면 이벤트를 기다리지 않고 지금 바로 위치를 맞춘다.
            if (entry.room.State != RoomState.Idle)
                MoveToRoom(entry);
        }
    }

    private void MoveToRoom(RoomMapEntry entry)
    {
        Vector2 roomPos = entry.minimapRect.anchoredPosition;
        playerIcon.anchoredPosition = roomPos;
        content.anchoredPosition = -roomPos;
    }
}
