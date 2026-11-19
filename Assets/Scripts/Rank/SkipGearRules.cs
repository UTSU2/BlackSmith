using UnityEngine;

namespace BlackSmith.Rank
{
    /// <summary>스킵 장비 규칙(순수 계산).</summary>
    public class SkipGearRules
    {
        public const int MaxLevel = 5;

        private readonly SkipGearSettings settings;

        public SkipGearRules(SkipGearSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>레벨(1~5)의 스킵 단계 고정 점수. 범위 밖 레벨은 1~5로 맞춘다.</summary>
        public int GetScore(int level)
        {
            int index = Mathf.Clamp(level, 1, MaxLevel) - 1;
            return index < settings.Scores.Count ? settings.Scores[index] : 0;
        }

        /// <summary>level → level+1 강화 비용. 최대 레벨이거나 비용이 미정(-1)이면 -1.</summary>
        public int GetUpgradeCost(int level)
        {
            if (level < 1 || level >= MaxLevel) return -1;
            int index = level - 1;
            if (index >= settings.UpgradeCosts.Count) return -1;
            int cost = settings.UpgradeCosts[index];
            return cost < 0 ? -1 : cost;
        }

        public BlacksmithRank GetUnlockRank(SkipGearKind kind)
        {
            return kind == SkipGearKind.Heat ? settings.HeatUnlockRank : settings.HeatTreatUnlockRank;
        }
    }
}
