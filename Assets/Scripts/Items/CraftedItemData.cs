using UnityEngine;

namespace BlackSmith.Items
{
    /// <summary>
    /// 제작품 아이템. 상점에서 판매만 가능하고 구매는 할 수 없다.
    /// 등급은 레시피에 정해진 defaultGrade 하나뿐이다. 판매가는 제작할 때 정해지는 값이다:
    /// 레시피에 적힌 재료의 구매가 합(ItemVariant.IngredientValue) x 품질 배율. 등급으로 직접 바뀌지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "new_crafted", menuName = "BlackSmith/Items/Crafted")]
    public class CraftedItemData : ItemData
    {
        public override ItemType Type => ItemType.Crafted;

        public override int GetSellUnitPrice(ItemVariant variant)
        {
            return PriceRounding.Round(variant.IngredientValue * ItemQualityInfo.PriceMultiplier(variant.Quality));
        }
    }
}
