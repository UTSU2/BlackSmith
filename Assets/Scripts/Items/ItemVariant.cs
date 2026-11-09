using System;

namespace BlackSmith.Items
{
    /// <summary>
    /// 같은 아이템 ID 안에서 인벤토리 항목을 가르는 개별 상태: 등급, 품질, 재료 합산 가격.
    /// 정적 데이터(ItemData)와 분리된 런타임 값이다.
    /// - Quality: 광물은 None, 완성품은 최하품~최상품.
    /// - IngredientValue: 완성품을 만들 때 레시피에 적힌 재료의 구매가 합. 광물은 0.
    ///   같은 완성품이라도 쓴 재료 등급에 따라 가격이 달라지므로 항목을 따로 센다.
    /// </summary>
    public readonly struct ItemVariant : IEquatable<ItemVariant>
    {
        public readonly ItemGrade Grade;
        public readonly ItemQuality Quality;
        public readonly int IngredientValue;

        public ItemVariant(ItemGrade grade, ItemQuality quality = ItemQuality.None, int ingredientValue = 0)
        {
            Grade = grade;
            Quality = quality;
            IngredientValue = ingredientValue;
        }

        public bool Equals(ItemVariant other)
        {
            return Grade == other.Grade && Quality == other.Quality && IngredientValue == other.IngredientValue;
        }

        public override bool Equals(object obj) => obj is ItemVariant other && Equals(other);

        public override int GetHashCode() => (((int)Grade * 397) ^ (int)Quality) * 397 ^ IngredientValue;

        /// <summary>목록에 보여줄 꼬리표. 예: "Common 상품", 광물은 "Common".</summary>
        public string Describe()
        {
            return Quality == ItemQuality.None ? Grade.ToString() : $"{Grade} {ItemQualityInfo.DisplayName(Quality)}";
        }
    }
}
