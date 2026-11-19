using System;
using System.Collections.Generic;
using UnityEngine;

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
