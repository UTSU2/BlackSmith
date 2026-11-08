using UnityEngine;

namespace BlackSmith.Items
{
    /// <summary>광물 아이템. 상점에서 구매와 판매가 모두 가능하다.</summary>
    [CreateAssetMenu(fileName = "new_mineral", menuName = "BlackSmith/Items/Mineral")]
    public class MineralItemData : ItemData
    {
        [Header("구매")]
        [Tooltip("Common 등급 기준 구매가. 다른 등급은 등급 배율(ItemGradeInfo)을 곱해 자동 계산한다.")]
        [SerializeField, Min(0)] private int buyPrice;

        public override ItemType Type => ItemType.Mineral;
        public override bool CanBuy => true;

        /// <summary>Common 기준 구매가.</summary>
        public int CommonBuyPrice => buyPrice;

        public override int GetBuyPrice(ItemGrade grade) => ItemGradeInfo.ScalePrice(buyPrice, grade);

        protected override void OnValidate()
        {
            base.OnValidate();
            if (buyPrice < 0) buyPrice = 0;
        }
    }
}
