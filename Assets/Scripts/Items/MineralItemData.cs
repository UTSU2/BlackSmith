using UnityEngine;

namespace BlackSmith.Items
{
    /// <summary>광물 아이템. 상점에서 구매와 판매가 모두 가능하다.</summary>
    [CreateAssetMenu(fileName = "new_mineral", menuName = "BlackSmith/Items/Mineral")]
    public class MineralItemData : ItemData
    {
        [Header("구매")]
        [SerializeField, Min(0)] private int buyPrice;

        public override ItemType Type => ItemType.Mineral;
        public override bool CanBuy => true;
        public override int BuyPrice => buyPrice;

        protected override void OnValidate()
        {
            base.OnValidate();
            if (buyPrice < 0) buyPrice = 0;
        }
    }
}
