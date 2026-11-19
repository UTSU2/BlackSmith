using System;
using System.Collections.Generic;
using UnityEngine;
using BlackSmith.Items;

namespace BlackSmith.Rank
{
    /// <summary>
    /// 승급 퀘스트 설정(정적 데이터): 등급별로 어떤 레시피를 어느 품질 이상으로 만들어야 하는지.
    /// 내용은 미정이며 사용자가 에셋에 채운다. 레시피 id가 비어 있거나 최소 품질이 None이면 그 등급은 승급할 수 없다.
    /// </summary>
    [CreateAssetMenu(fileName = "RankQuestSettings", menuName = "BlackSmith/Rank Quest Settings")]
    public class RankQuestSettings : ScriptableObject
    {
        public const string ResourcePath = "Rank/RankQuestSettings";

        [Serializable]
        public class QuestEntry
        {
            [Tooltip("이 퀘스트를 클리어하면 오르는 등급.")]
            public BlacksmithRank targetRank;
            [Tooltip("HUD에 보이는 퀘스트 설명. 비어 있으면 레시피와 품질로 임시 표기한다.")]
            public string description;
            [Tooltip("제작해야 하는 레시피 id. 비어 있으면 미정(승급 불가).")]
            public string recipeId;
            [Tooltip("최종 품질(품질 보너스 적용 후)이 이 값 이상이어야 한다. None이면 미정(승급 불가).")]
            public ItemQuality minQuality = ItemQuality.None;
        }

        [Tooltip("등급별 퀘스트. 비어 있는 항목은 미정이며 그 등급으로는 승급하지 않는다.")]
        [SerializeField] private List<QuestEntry> entries = new List<QuestEntry>
        {
            new QuestEntry { targetRank = BlacksmithRank.Novice },
            new QuestEntry { targetRank = BlacksmithRank.Skilled },
            new QuestEntry { targetRank = BlacksmithRank.Artisan },
            new QuestEntry { targetRank = BlacksmithRank.Grandmaster }
        };

        /// <summary>target 등급의 퀘스트. 항목이 없거나 내용이 비어 있으면 false.</summary>
        public bool TryGet(BlacksmithRank target, out QuestEntry entry)
        {
            entry = null;
            if (entries == null) return false;
            foreach (QuestEntry candidate in entries)
            {
                if (candidate == null || candidate.targetRank != target) continue;
                if (string.IsNullOrEmpty(candidate.recipeId) || candidate.minQuality == ItemQuality.None) return false;
                entry = candidate;
                return true;
            }
            return false;
        }

        /// <summary>설정 에셋을 불러온다. 없으면 null(퀘스트 없이 동작하므로 승급할 수 없다).</summary>
        public static RankQuestSettings Load()
        {
            return Resources.Load<RankQuestSettings>(ResourcePath);
        }
    }
}
