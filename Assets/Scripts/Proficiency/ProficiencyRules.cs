using System.Collections.Generic;
using BlackSmith.Items;

namespace BlackSmith.Proficiency
{
    /// <summary>
    /// 숙련도 계산(순수 C#). 규칙은 docs/V2.md §2.
    /// 숙련도는 레시피별 누적 수치 하나로 저장하고, 레벨은 필요 수치표의 누적 합으로 계산한다(초과분 이월이 따로 필요 없다).
    /// </summary>
    public class ProficiencyRules
    {
        /// <summary>보너스 적용 전 만점 (미니게임 4개 x 10점, CraftScoring.MaxScore와 같다).</summary>
        public const int MaxScore = 40;

        private readonly ProficiencySettings settings;
        private readonly Dictionary<ItemGrade, int[]> thresholdCache = new Dictionary<ItemGrade, int[]>();

        public ProficiencyRules(ProficiencySettings settings)
        {
            this.settings = settings;
        }

        private int StageCount => settings.StageNames.Count;
        private int LevelCount => StageCount * settings.LevelsPerStage;

        /// <summary>
        /// 레벨별 필요 수치(단계 순서대로, 단계마다 LevelsPerStage개). 레벨 등급은 레시피 결과물의 등급이다.
        /// 반올림하지 않은 값으로 이어 계산하고, 각 값만 half-up 반올림한다.
        /// </summary>
        public IReadOnlyList<int> GetThresholds(ItemGrade grade)
        {
            if (thresholdCache.TryGetValue(grade, out int[] cached)) return cached;

            int[] thresholds = new int[LevelCount];
            decimal value = settings.BaseRequirement + settings.RequirementPerGrade * (int)grade;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (i > 0)
                {
                    bool newStage = i % settings.LevelsPerStage == 0;
                    value *= newStage ? StageMultiplier(i / settings.LevelsPerStage - 1) : settings.LevelMultiplier;
                }
                thresholds[i] = PriceRounding.Round(value);
            }

            thresholdCache[grade] = thresholds;
            return thresholds;
        }

        /// <summary>Master까지 필요한 누적 수치.</summary>
        public int GetMaxPoints(ItemGrade grade)
        {
            long total = 0;
            foreach (int threshold in GetThresholds(grade)) total += threshold;
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        public ProficiencyState Evaluate(int points, ItemGrade grade)
        {
            IReadOnlyList<int> thresholds = GetThresholds(grade);
            int max = GetMaxPoints(grade);
            if (points < 0) points = 0;
            if (points >= max)
            {
                return new ProficiencyState(max, StageCount, 0, true, 0, 0, settings.MasterBonus, settings.MasterName);
            }

            int remaining = points;
            int index = 0;
            while (remaining >= thresholds[index])
            {
                remaining -= thresholds[index];
                index++;
            }

            int stage = index / settings.LevelsPerStage;
            int level = index % settings.LevelsPerStage + 1;
            decimal bonus = settings.BonusPerLevel * (index + 1);
            string label = $"{settings.StageNames[stage]} Lv.{level}";
            return new ProficiencyState(points, stage, level, false, remaining, thresholds[index], bonus, label);
        }

        /// <summary>보너스 적용 전 총점으로 획득량을 정한다. 버림한 정수이고, 기준 미만이면 0, 만점이면 만점 획득량.</summary>
        public int CalculateGain(decimal rawScore)
        {
            int floored = (int)decimal.Floor(rawScore);
            if (floored >= MaxScore) return settings.PerfectScoreGain;
            if (floored < settings.MinScoreToGain) return 0;
            return floored;
        }

        /// <summary>
        /// 총점에 보너스를 더한다. 보너스 적용 전 총점이 만점이면 그대로 두고,
        /// 아니면 더한 값이 상한(39) 이상일 때 상한으로 맞춘다. 최상품은 직접 만점일 때만 나온다.
        /// </summary>
        public decimal ApplyBonus(decimal rawScore, decimal bonus)
        {
            if (rawScore >= MaxScore) return rawScore;
            if (bonus <= 0m) return rawScore;
            decimal total = rawScore + bonus;
            return total >= settings.BonusScoreCap ? settings.BonusScoreCap : total;
        }

        private decimal StageMultiplier(int transition)
        {
            IReadOnlyList<float> multipliers = settings.StageMultipliers;
            if (transition < 0 || transition >= multipliers.Count) return settings.LevelMultiplier;
            return (decimal)multipliers[transition];
        }
    }
}
