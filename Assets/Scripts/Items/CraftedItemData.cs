using UnityEngine;

namespace BlackSmith.Items
{
    /// <summary>
    /// 제작품 아이템. 상점에서 판매만 가능하고 구매는 할 수 없다.
    /// TODO: 제작품 전용 기본 정보(제작 규칙 등)는 규칙이 확정되면 이 클래스에 필드를 추가한다.
    /// </summary>
    [CreateAssetMenu(fileName = "new_crafted", menuName = "BlackSmith/Items/Crafted")]
    public class CraftedItemData : ItemData
    {
        public override ItemType Type => ItemType.Crafted;
    }
}
