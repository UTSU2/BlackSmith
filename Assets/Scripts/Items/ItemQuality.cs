namespace BlackSmith.Items
{
    /// <summary>
    /// 완성품 품질. 미니게임 점수(0~40)로 정해지며 등급과 별개다. (docs/GameDesign.md §5)
    /// None은 품질이 없는 아이템(광물)과 품질 필드가 없던 옛 저장 데이터용이다 (JsonUtility 기본값 0).
    /// 값은 저장되므로 순서를 바꾸지 않는다.
    /// </summary>
    public enum ItemQuality
    {
        None,
        Lowest,   // 최하품 0~9점
        Low,      // 하품 10~19점
        Medium,   // 중품 20~29점
        High,     // 상품 30~39점
        Highest,  // 최상품 40점
    }

    public static class ItemQualityInfo
    {
        /// <summary>판매가 배율. None은 품질을 적용하지 않으므로 1.</summary>
        public static decimal PriceMultiplier(ItemQuality quality)
        {
            switch (quality)
            {
                case ItemQuality.Lowest: return 0.7m;
                case ItemQuality.Low: return 1m;
                case ItemQuality.Medium: return 1.2m;
                case ItemQuality.High: return 1.5m;
                case ItemQuality.Highest: return 2m;
                default: return 1m;
            }
        }

        public static string DisplayName(ItemQuality quality)
        {
            switch (quality)
            {
                case ItemQuality.Lowest: return "최하품";
                case ItemQuality.Low: return "하품";
                case ItemQuality.Medium: return "중품";
                case ItemQuality.High: return "상품";
                case ItemQuality.Highest: return "최상품";
                default: return "";
            }
        }

        /// <summary>합계 점수(0~40)를 버림한 값으로 품질 구간을 정한다. 9.9점은 9로 보아 최하품이다.</summary>
        public static ItemQuality FromScore(decimal score)
        {
            int floored = (int)decimal.Floor(score);
            if (floored >= 40) return ItemQuality.Highest;
            if (floored >= 30) return ItemQuality.High;
            if (floored >= 20) return ItemQuality.Medium;
            if (floored >= 10) return ItemQuality.Low;
            return ItemQuality.Lowest;
        }
    }
}
