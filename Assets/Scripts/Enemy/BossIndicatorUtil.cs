using UnityEngine;

// 보스 패턴(BossLineAttack/BossAoeAttack)의 텔레그래프 인디케이터용 스프라이트를 런타임에
// 절차적으로 생성해서 캐싱하는 공용 유틸리티. 씬에 별도 아트 에셋을 준비하지 않아도
// 즉시 붉은 사각형/원형 경고 표시를 만들 수 있도록 하기 위함(보스 캐릭터 자체가 임시이듯,
// 인디케이터도 코드로 즉석 생성하는 임시 비주얼로 처리).
public static class BossIndicatorUtil
{
    private const int CircleTextureSize = 128;

    private static Sprite rectangleSprite;
    private static Sprite filledCircleSprite;
    private static Sprite ringCircleSprite;

    // 피벗을 왼쪽 중앙(0, 0.5)에 둔 1x1 유닛 흰색 사각형.
    // localScale.x/y로 늘리면 피벗(오브젝트 위치)에서 오른쪽으로만 뻗어나가는 직사각형이 된다 -
    // 일직선 공격 인디케이터가 "보스 위치에서 사거리 끝까지" 자라나는 모양을 만들기 위함.
    public static Sprite GetRectangleSprite()
    {
        if (rectangleSprite == null)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();

            rectangleSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4f);
        }
        return rectangleSprite;
    }

    // 속이 꽉 찬 원. localScale(지름)로 크기를 조절한다 - 장판 공격의 "작은 원(차징)"과 투사체 표시용.
    public static Sprite GetFilledCircleSprite()
    {
        if (filledCircleSprite == null)
            filledCircleSprite = CreateCircleSprite(filled: true);
        return filledCircleSprite;
    }

    // 테두리만 있는 원 - 장판 공격의 "큰 원(실제 피격 범위)" 경계 표시용.
    public static Sprite GetRingCircleSprite()
    {
        if (ringCircleSprite == null)
            ringCircleSprite = CreateCircleSprite(filled: false);
        return ringCircleSprite;
    }

    private static Sprite CreateCircleSprite(bool filled)
    {
        int size = CircleTextureSize;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;
        float ringThickness = size * 0.06f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha;
                if (filled)
                {
                    alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                }
                else
                {
                    float edgeDist = Mathf.Abs(dist - radius);
                    alpha = Mathf.Clamp01(1f - (edgeDist - ringThickness * 0.5f) / 1.5f);
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        // pixelsPerUnit = 텍스처 크기 -> localScale 1일 때 지름 1 월드 유닛이 되도록 정규화.
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
