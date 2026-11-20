using System.Collections.Generic;
using BlackSmith.Items;
using BlackSmith.Proficiency;

namespace BlackSmith.Crafting
{
    /// <summary>제작 단계 하나의 결과. 런타임 데이터다.</summary>
    public class CraftStageResult
    {
        public CraftStage Stage { get; }

        /// <summary>완성도 0~1. 건너뛴 단계는 의미 없다.</summary>
        public float Completeness { get; }

        /// <summary>아직 구현되지 않아 건너뛴 단계인가.</summary>
        public bool Skipped { get; }

        /// <summary>스킵 장비로 건너뛴 단계인가. 이때는 미니게임 없이 FixedScore가 그대로 점수가 된다(Skipped와는 다르다).</summary>
        public bool IsFixed { get; }

        /// <summary>스킵 장비가 준 단계 점수(0~10). IsFixed일 때만 의미 있다.</summary>
        public int FixedScore { get; }

        public CraftStageResult(CraftStage stage, float completeness, bool skipped)
        {
            Stage = stage;
            Completeness = completeness;
            Skipped = skipped;
        }

        /// <summary>스킵 장비로 건너뛴 단계의 결과(고정 점수).</summary>
        public CraftStageResult(CraftStage stage, int fixedScore)
        {
            Stage = stage;
            IsFixed = true;
            FixedScore = fixedScore;
        }
    }

    /// <summary>
    /// 제작 한 번의 결과. 정적 데이터(레시피)와 분리된 런타임 데이터다.
    /// 단계 점수 → 품질 계산은 CraftScoring이, 인벤토리 지급은 CraftingController가 맡는다.
    /// </summary>
    public class CraftResult
    {
        private readonly List<CraftStageResult> stages = new List<CraftStageResult>();

        public CraftRequest Request { get; }
        /// <summary>일치한 레시피. 알 수 없는 제작이면 null.</summary>
        public CraftRecipeData Recipe => Request.Recipe;
        /// <summary>레시피와 일치하지 않는 조합으로 만든 알 수 없는 제작품인가.</summary>
        public bool IsUnknown => Request.IsUnknown;
        public int Difficulty => Request.Difficulty;
        public IReadOnlyList<CraftStageResult> Stages => stages;

        public CraftResult(CraftRequest request)
        {
            Request = request;
        }

        public void AddStage(CraftStageResult stage)
        {
            stages.Add(stage);
        }

        // ---- 제작이 끝난 뒤 CraftingController가 채운다 (CraftFinished 전에) ----

        public CraftedItemData Product { get; private set; }
        public ItemGrade Grade { get; private set; }
        public ItemQuality Quality { get; private set; }

        /// <summary>보너스 적용 전 총점(0~40).</summary>
        public decimal RawScore { get; private set; }

        /// <summary>보너스 적용 후 총점. 품질은 이 값으로 정했다.</summary>
        public decimal FinalScore { get; private set; }

        /// <summary>이번 제작에 적용한 레벨 보너스(제작 전 숙련도 기준). 상한으로 실제로 덜 더해졌어도 레벨 값이다.</summary>
        public decimal Bonus { get; private set; }

        /// <summary>이번 제작으로 늘어난 숙련도.</summary>
        public int ProficiencyGain { get; private set; }

        /// <summary>제작 전/후 숙련도. 숙련도 설정이 없으면 null.</summary>
        public ProficiencyState ProficiencyBefore { get; private set; }
        public ProficiencyState ProficiencyAfter { get; private set; }

        public void SetOutcome(CraftedItemData product, ItemGrade grade, ItemQuality quality, decimal rawScore, decimal finalScore, decimal bonus)
        {
            Product = product;
            Grade = grade;
            Quality = quality;
            RawScore = rawScore;
            FinalScore = finalScore;
            Bonus = bonus;
        }

        /// <summary>이번 제작으로 레시피가 도감에 처음 해금됐는가. 알 수 없는 제작은 항상 false.</summary>
        public bool NewlyUnlocked { get; private set; }

        public void SetUnlocked(bool newlyUnlocked)
        {
            NewlyUnlocked = newlyUnlocked;
        }

        public void SetProficiency(ProficiencyState before, ProficiencyState after, int gain)
        {
            ProficiencyBefore = before;
            ProficiencyAfter = after;
            ProficiencyGain = gain;
        }
    }
}
