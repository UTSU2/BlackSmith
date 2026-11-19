using System.Collections.Generic;
using UnityEngine;

namespace BlackSmith.Proficiency
{
    /// <summary>
    /// 숙련도 수치 설정(정적 데이터). 규칙은 docs/V2.md §2, 계산은 ProficiencyRules가 한다.
    /// 배율·보너스는 float로 저장하지만 계산할 때 decimal로 바꿔 오차를 피한다.
    /// </summary>
    [CreateAssetMenu(fileName = "ProficiencySettings", menuName = "BlackSmith/Proficiency Settings")]
    public class ProficiencySettings : ScriptableObject
    {
        public const string ResourcePath = "Proficiency/ProficiencySettings";

        [Header("레벨별 필요 수치")]
        [Tooltip("Common 등급 첫 단계 Lv1의 필요 수치.")]
        [SerializeField] private int baseRequirement = 100;
        [Tooltip("등급이 하나 오를 때마다 Lv1 시작 수치에 더하는 값.")]
        [SerializeField] private int requirementPerGrade = 50;
        [Tooltip("같은 단계 안에서 레벨이 오를 때 곱하는 배율.")]
        [SerializeField] private float levelMultiplier = 1.2f;
        [Tooltip("단계가 넘어갈 때(Lv9 → 다음 단계 Lv1) 곱하는 배율. 개수 = 단계 수 - 1.")]
        [SerializeField] private List<float> stageMultipliers = new List<float> { 1.5f, 1.5f };
        [Tooltip("단계 이름. 순서대로 진행한다.")]
        [SerializeField] private List<string> stageNames = new List<string> { "초급", "중급", "고급" };
        [SerializeField] private int levelsPerStage = 9;
        [SerializeField] private string masterName = "Master";

        [Header("품질 보너스")]
        [SerializeField] private float bonusPerLevel = 0.1f;
        [SerializeField] private float masterBonus = 3f;
        [Tooltip("보너스를 더한 총점의 상한. 최상품(40)은 보너스 없이 직접 40점을 얻어야 한다.")]
        [SerializeField] private int bonusScoreCap = 39;

        [Header("획득량")]
        [Tooltip("보너스 적용 전 총점(버림)이 이 값 미만이면 쌓이지 않는다.")]
        [SerializeField] private int minScoreToGain = 20;
        [Tooltip("보너스 적용 전 총점이 만점(40)일 때의 획득량.")]
        [SerializeField] private int perfectScoreGain = 60;

        public int BaseRequirement => baseRequirement;
        public int RequirementPerGrade => requirementPerGrade;
        public decimal LevelMultiplier => (decimal)levelMultiplier;
        public IReadOnlyList<float> StageMultipliers => stageMultipliers;
        public IReadOnlyList<string> StageNames => stageNames;
        public int LevelsPerStage => levelsPerStage;
        public string MasterName => masterName;
        public decimal BonusPerLevel => (decimal)bonusPerLevel;
        public decimal MasterBonus => (decimal)masterBonus;
        public int BonusScoreCap => bonusScoreCap;
        public int MinScoreToGain => minScoreToGain;
        public int PerfectScoreGain => perfectScoreGain;

        /// <summary>설정 에셋을 불러온다. 없으면 null(숙련도 없이 동작).</summary>
        public static ProficiencySettings Load()
        {
            return Resources.Load<ProficiencySettings>(ResourcePath);
        }
    }
}
