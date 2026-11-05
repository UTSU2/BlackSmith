using System.Collections.Generic;

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

        public CraftStageResult(CraftStage stage, float completeness, bool skipped)
        {
            Stage = stage;
            Completeness = completeness;
            Skipped = skipped;
        }
    }

    /// <summary>
    /// 제작 한 번의 결과. 정적 데이터(레시피)와 분리된 런타임 데이터다.
    /// 완성도 → 등급 계산과 인벤토리 지급은 미정이라 아직 하지 않는다.
    /// </summary>
    public class CraftResult
    {
        private readonly List<CraftStageResult> stages = new List<CraftStageResult>();

        public CraftRecipeData Recipe { get; }
        public int Difficulty { get; }
        public IReadOnlyList<CraftStageResult> Stages => stages;

        public CraftResult(CraftRecipeData recipe, int difficulty)
        {
            Recipe = recipe;
            Difficulty = difficulty;
        }

        public void AddStage(CraftStageResult stage)
        {
            stages.Add(stage);
        }
    }
}
