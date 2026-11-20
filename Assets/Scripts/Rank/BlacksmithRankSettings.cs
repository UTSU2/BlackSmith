using System;
using System.Collections.Generic;
using UnityEngine;
using BlackSmith.Items;

namespace BlackSmith.Rank
{
    /// <summary>
    /// 대장장이 등급 설정(정적 데이터): 등급 이름과 승급에 필요한 개수.
    /// 필요 개수 -1은 미정이며 이때는 승급할 수 없다. 값은 사용자가 에셋에서 채운다.
    /// </summary>
    [CreateAssetMenu(fileName = "BlacksmithRankSettings", menuName = "BlackSmith/Blacksmith Rank Settings")]
    public class BlacksmithRankSettings : ScriptableObject
    {
        public const string ResourcePath = "Rank/BlacksmithRankSettings";

        [Serializable]
        public class RankEntry
        {
            public string displayName;
            [Tooltip("이 등급에 오르려면 필요한 도감 해금 수. -1 = 미정(승급 불가).")]
            public int requiredCodex = -1;
            [Tooltip("이 등급에 오르려면 필요한 master 숙련도 수. -1 = 미정(승급 불가).")]
            public int requiredMasters = -1;
            [Tooltip("승급 팝업에 보이는 혜택 문구. 비어 있으면 혜택 줄을 숨긴다(미정).")]
            [TextArea] public string benefitText;
            [Tooltip("이 등급부터 판매 가격에 곱하는 배율(상점 판매, 광물·제작품 모두). 1 = 추가 없음.")]
            public float sellPriceMultiplier = 1f;
        }

        [Tooltip("BlacksmithRank 순서(견습, 초급, 숙련, 장인, 명장). 견습의 필요 개수는 쓰지 않는다.")]
        [SerializeField] private List<RankEntry> ranks = new List<RankEntry>
        {
            new RankEntry { displayName = "견습" },
            new RankEntry { displayName = "초급" },
            new RankEntry { displayName = "숙련" },
            new RankEntry { displayName = "장인" },
            new RankEntry { displayName = "명장" }
        };

        public string GetName(BlacksmithRank rank)
        {
            RankEntry entry = Get(rank);
            return entry != null && !string.IsNullOrEmpty(entry.displayName) ? entry.displayName : rank.ToString();
        }

        /// <summary>target 등급에 오르는 데 필요한 도감 해금 수. 미정이거나 없으면 -1.</summary>
        public int GetRequiredCodex(BlacksmithRank target)
        {
            RankEntry entry = Get(target);
            return entry != null ? entry.requiredCodex : -1;
        }

        /// <summary>target 등급에 오르는 데 필요한 master 숙련도 수. 미정이거나 없으면 -1.</summary>
        public int GetRequiredMasters(BlacksmithRank target)
        {
            RankEntry entry = Get(target);
            return entry != null ? entry.requiredMasters : -1;
        }

        /// <summary>rank 등급에 오를 때 보여 주는 혜택 문구. 없으면 빈 문자열.</summary>
        public string GetBenefit(BlacksmithRank rank)
        {
            RankEntry entry = Get(rank);
            return entry != null && entry.benefitText != null ? entry.benefitText : string.Empty;
        }

        /// <summary>rank 등급의 판매 가격 배율. 에셋에 없거나 0 이하면 1(추가 없음).</summary>
        public float GetSellPriceMultiplier(BlacksmithRank rank)
        {
            RankEntry entry = Get(rank);
            return entry != null && entry.sellPriceMultiplier > 0f ? entry.sellPriceMultiplier : 1f;
        }

        /// <summary>판매 단가에 등급 배율을 적용한다. 반올림은 가격 규칙(PriceRounding, 0.5는 올림)을 따른다.</summary>
        public int ApplySellMultiplier(int unitPrice, BlacksmithRank rank)
        {
            float multiplier = GetSellPriceMultiplier(rank);
            if (multiplier == 1f) return unitPrice;
            return PriceRounding.Round((decimal)unitPrice * (decimal)multiplier);
        }

        private RankEntry Get(BlacksmithRank rank)
        {
            int index = (int)rank;
            return ranks != null && index >= 0 && index < ranks.Count ? ranks[index] : null;
        }

        /// <summary>설정 에셋을 불러온다. 없으면 null(등급 표시·승급 없이 동작).</summary>
        public static BlacksmithRankSettings Load()
        {
            return Resources.Load<BlacksmithRankSettings>(ResourcePath);
        }
    }
}
