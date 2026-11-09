using System;

namespace BlackSmith.Items
{
    /// <summary>
    /// 아이템 등급. 재료와 완성품이 같은 등급 이름을 쓴다. (docs/GameDesign.md §5)
    /// 값은 에셋에 정수로 저장되므로 순서를 바꾸지 않는다. (Epic → Unique 이름 변경은 값 3을 그대로 쓴다)
    /// </summary>
    public enum ItemGrade
    {
        Common,
        Uncommon,
        Rare,
        Unique,
        Legendary,
    }

    public static class ItemGradeInfo
    {
        /// <summary>등급 개수.</summary>
        public static readonly int Count = Enum.GetValues(typeof(ItemGrade)).Length;

        // Common을 1배로 둔 재료 가격 배율. ItemGrade 순서와 같다.
        private static readonly decimal[] PriceMultipliers = { 1m, 1.2m, 1.5m, 1.8m, 2m };

        /// <summary>재료 가격 배율. Common 1, Uncommon 1.2, Rare 1.5, Unique 1.8, Legendary 2.</summary>
        public static decimal PriceMultiplier(ItemGrade grade)
        {
            int index = (int)grade;
            return index >= 0 && index < PriceMultipliers.Length ? PriceMultipliers[index] : 1m;
        }

        /// <summary>Common 기준 가격에 등급 배율을 곱해 반올림한 가격.</summary>
        public static int ScalePrice(int commonPrice, ItemGrade grade)
        {
            return PriceRounding.Round(commonPrice * PriceMultiplier(grade));
        }
    }

    /// <summary>가격 반올림 규칙. Mathf.RoundToInt는 .5를 짝수로 보내므로 쓰지 않고 항상 올린다(0.5 → 1).</summary>
    public static class PriceRounding
    {
        public static int Round(decimal value)
        {
            decimal rounded = decimal.Round(value, 0, MidpointRounding.AwayFromZero);
            if (rounded > int.MaxValue) return int.MaxValue;
            if (rounded < 0) return 0;
            return (int)rounded;
        }
    }
}
