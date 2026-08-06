using UnityEngine;

// "Breakable" 태그 오브젝트(항아리 등)가 속한 방이 클리어(RoomController.OnRoomActivated)되면
// 외곽선을 켜서 "이제 이걸 클릭해서 부술 수 있다"는 걸 플레이어에게 시각적으로 알려준다.
// 외곽선용 자식 SpriteRenderer는 Awake에서 이 컴포넌트가 직접 만들어 붙이므로, 새 breakable을
// 배치할 때 자식 오브젝트를 손으로 준비할 필요 없이 이 컴포넌트 하나만 붙이면 된다.
//
// 주의: Sprite-Outline.shader는 스프라이트 알파가 0인 "바깥쪽" 텍셀을 칠해서 외곽선을 그리는데,
// SpriteRenderer의 메쉬가 "Tight"(텍스처 임포터의 Mesh Type)이면 알파 실루엣 바깥쪽에는 메쉬
// 자체가 없어서(=프래그먼트 셰이더가 아예 실행되지 않아서) 외곽선이 군데군데 끊겨 보인다 -
// 특히 이 항아리처럼 작고 디테일이 많은 스프라이트는 Tight 메쉬의 폴리곤 근사(테셀레이션)가
// 픽셀 단위 실루엣을 정확히 못 따라가 더 심하게 나타난다. 이 컴포넌트를 붙일 스프라이트는
// 반드시 텍스처 임포터에서 Mesh Type = Full Rect로 설정할 것(Sprite Extrude도 outlineWidth
// 이상으로 - 단, 스프라이트시트를 공유하는 텍스처라면 이웃 스프라이트를 침범하지 않을 만큼만).
[RequireComponent(typeof(SpriteRenderer))]
public class BreakableOutlineOnRoomClear : MonoBehaviour
{
    [Tooltip("이 오브젝트가 속한 방. 비워두면 부모에서 자동으로 찾는다.")]
    [SerializeField] private RoomController room;

    [Tooltip("Assets/Materials/Sprite-Outline.mat (Custom/2D/Sprite-Outline 셰이더)")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Color outlineColor = new Color(1f, 0.95f, 0.4f, 1f);
    [Tooltip("외곽선 두께(스프라이트 텍셀 기준, 보통 1)")]
    [SerializeField] private float outlineWidth = 0.6f;

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer outlineRenderer;

    private void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();

        if (room == null)
            room = FindRoomController();

        CreateOutlineRenderer();
    }

    // GetComponentInParent만으로는 못 찾는 경우가 많다 - 예를 들어 Rooms/Map_3/Heart_Queen_Palace
    // 아래에 배치된 breakable은 RoomController가 형제 분기인 Rooms/Map_3/Logic에 있어서 부모 체인에
    // 없다. 이 프로젝트는 맵 하나(Rooms/Map_N)당 RoomController가 하나뿐이므로, "Rooms"의 바로
    // 아래 자식(Map_N)까지 거슬러 올라가 그 안에서 다시 찾으면 항상 정확히 찾을 수 있다.
    private RoomController FindRoomController()
    {
        var direct = GetComponentInParent<RoomController>();
        if (direct != null)
            return direct;

        Transform mapRoot = transform;
        while (mapRoot.parent != null && mapRoot.parent.name != "Rooms")
            mapRoot = mapRoot.parent;

        return mapRoot.GetComponentInChildren<RoomController>(true);
    }

    private void Start()
    {
        if (room != null)
            room.OnRoomActivated += HandleRoomActivated;
        else
            Debug.LogWarning($"[BreakableOutlineOnRoomClear] {name}: RoomController를 찾지 못해 외곽선이 켜지지 않습니다.");
    }

    private void OnDestroy()
    {
        if (room != null)
            room.OnRoomActivated -= HandleRoomActivated;
    }

    private void CreateOutlineRenderer()
    {
        var outlineGo = new GameObject("Outline");
        outlineGo.transform.SetParent(transform, false);

        outlineRenderer = outlineGo.AddComponent<SpriteRenderer>();
        outlineRenderer.sprite = sourceRenderer.sprite;
        outlineRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        outlineRenderer.sortingOrder = sourceRenderer.sortingOrder - 1;

        if (outlineMaterial != null)
        {
            var instance = new Material(outlineMaterial);
            instance.SetColor("_OutlineColor", outlineColor);
            instance.SetFloat("_OutlineWidth", outlineWidth);
            outlineRenderer.sharedMaterial = instance;
        }

        outlineGo.SetActive(false);
    }

    private void HandleRoomActivated()
    {
        SetOutlineVisible(true);
    }

    /// <summary>Breakable은 부서지면 오브젝트째로 사라지므로 별도 처리가 필요 없지만, 상자
    /// (TreasureBoxReward)처럼 방 클리어 후에도 계속 남아있는 오브젝트는 상호작용으로 연 뒤
    /// "이제 열 수 있다" 표시인 외곽선을 직접 꺼줘야 한다 - 그래서 외부에서 호출 가능하게 열어둔다.</summary>
    public void SetOutlineVisible(bool visible)
    {
        if (outlineRenderer != null)
            outlineRenderer.gameObject.SetActive(visible);
    }
}
