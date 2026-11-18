using UnityEngine;

namespace BlackSmith.Items
{
    /// <summary>
    /// 제작품 아이템. 상점에서 판매만 가능하고 구매는 할 수 없다.
    /// 등급은 아이템 데이터에 정해진 defaultGrade 하나뿐이다. 판매가는 제작할 때 정해지는 값이다:
    /// 레시피에 적힌 재료의 구매가 합(ItemVariant.IngredientValue) x 품질 배율. 등급으로 직접 바뀌지 않는다.
    /// fixedSellPrice가 0보다 크면(알 수 없는 제작품) 품질과 관계없이 그 값이다.
    /// 주의: Item Creator 창이 private 필드 이름(fixedSellPrice)으로 값을 기록한다.
    /// </summary>
    [CreateAssetMenu(fileName = "new_crafted", menuName = "BlackSmith/Items/Crafted")]
    public class CraftedItemData : ItemData
    {
        [Tooltip("0이면 재료가 합 × 품질 배율. 0보다 크면 품질과 관계없이 이 값.")]
        [SerializeField, Min(0)] private int fixedSellPrice;

        public override ItemType Type => ItemType.Crafted;

        public int FixedSellPrice => fixedSellPrice;

        public override int GetSellUnitPrice(ItemVariant variant)
        {
            if (fixedSellPrice > 0) return fixedSellPrice;
            return PriceRounding.Round(variant.IngredientValue * ItemQualityInfo.PriceMultiplier(variant.Quality));
        }
    }
}
