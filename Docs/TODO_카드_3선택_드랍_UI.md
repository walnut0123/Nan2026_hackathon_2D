# TODO: 카드 드랍 3장 중 1장 선택 UI

> 상태: **보류(연출/설계 미정).** 백엔드 계산 로직만 만들어져 있고, 실제로 플레이어에게
> 3장을 보여주고 고르게 하는 UI/드랍 흐름은 아직 없음.
> 이 문서는 다음에 새 세션(새 터미널)에서 이어서 진행할 수 있도록 필요한 맥락을 정리한 것.

## 왜 필요한가

원본 카드 데미지 설계서(v5.0)의 근거:

> "3장 단계에도 보상이 있어야 한다. 5장 완성만 보상하면 무늬·연속 루트는 도달 불가능해진다
> — 한 장씩 교체하는 과정에서 매 단계 데미지가 떨어지므로 플레이어가 그 길을 선택하지
> 않는다."

즉 카드를 "주는 대로 줍는" 지금 방식만으로는, 무늬/연속 조합을 완성해가는 도중에 일시적으로
손해를 보는 교체를 플레이어가 자발적으로 선택하지 않는다. 후보 3장을 보여주고 "이걸 주우면
데미지가 이렇게 바뀐다"를 미리 알려주면 그 판단을 도울 수 있다.

## 이미 준비된 것 (백엔드)

- `CardDamageSystem.PreviewOffer(CardInventory inventory, ItemData card, int playerLevel)`
  → 후보 카드 1장을 획득했다고 가정했을 때의 결과(`OfferPreview`)를 계산. 실제 인벤토리는
  건드리지 않음.
- `CardDamageSystem.PreviewOffers(inventory, candidates, playerLevel)`
  → 후보 여러 장을 한번에 평가해서 데미지 변화율(`DeltaPercent`) 내림차순으로 정렬한 리스트 반환.
- `OfferPreview.Describe()` → UI에 바로 꽂을 수 있는 한 줄 문구 (예: `"클럽 K · 플러시 · +18%"`).
- `CardInventory.FindWeakestSlot()` → 인벤토리가 꽉 찼을 때 어떤 슬롯이 교체 대상이 되는지
  미리 알려줌 (PreviewOffer가 내부적으로 이걸 씀).

즉 "후보 3장 리스트를 만들어서 `PreviewOffers`에 넣기만 하면" 각 후보의 효과 설명 문자열까지
바로 나온다 - UI/드랍 쪽만 만들면 됨.

## 아직 결정 안 된 것 (설계 질문)

1. **트리거 조건** — 모든 몬스터 처치마다 3장을 주나? 특정 정예/보스 처치, 혹은 특정 상자류
   (BreakableDropper)에서만? 지금 `LootTable`은 항목별 독립 확률로 "여러 개가 동시에" 떨어질
   수 있는 구조라, "이 중 하나만 고르라"는 별도 카테고리로 분리해야 함.
2. **제시 방식** — 두 가지 중 택1:
   - (A) 필드에 카드 3장을 흩뿌려 놓고, 하나를 주우면 나머지 둘이 사라짐. 기존 `ItemPickup`
     필드 픽업 방식과 잘 맞고 UI 패널을 새로 안 만들어도 됨.
   - (B) 화면 중앙에 모달 패널을 띄워서 3장을 나란히 보여주고 클릭/키로 선택. 더 명확하지만
     새 UI 패널·입력 처리·게임 일시정지 여부 등을 새로 설계해야 함.
3. **후보 풀 선정** — 52장 전체에서 무작위 3장? 아니면 현재 인벤토리 구성(연속/무늬 진행 중인
   숫자·무늬)에 가중치를 줘서 "완성에 가까운" 카드가 더 잘 나오게 할지.
4. **드랍 소스 연동** — `MonsterDropper`/`BreakableDropper`가 지금 쓰는 `LootTable.GetDrops()`와
   분리된 새 경로가 필요한지, 아니면 `LootEntry`에 "3택1 그룹" 플래그를 추가해서 같은
   `LootTable` 안에서 표현할지.

## 다음에 시작할 때 참고할 파일

- `Assets/Scripts/Player/CardDamageSystem.cs` — `PreviewOffer`/`PreviewOffers`/`OfferPreview`
- `Assets/Scripts/Inventory/CardInventory.cs` — `Acquire`, `FindWeakestSlot`
- `Assets/Scripts/Inventory/LootTable.cs`, `MonsterDropper.cs`, `BreakableDropper.cs` — 현재
  드랍 파이프라인
- `Assets/Scripts/Inventory/ItemPickup.cs` — 필드 픽업 처리, (A) 방식을 택하면 여기에
  "그룹 픽업 시 형제 오브젝트 제거" 로직 추가 필요
