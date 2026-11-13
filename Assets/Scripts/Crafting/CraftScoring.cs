using System.Collections.Generic;
using BlackSmith.Items;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 제작 규칙 계산(순수 C#): 품질 점수, 재료 합산 가격. 규칙은 docs/GameDesign.md §4~5.
    /// 재료 등급은 난이도·품질에 영향을 주지 않는다. 점수는 부동소수점 오차를 피하려고 decimal로 다룬다.
    /// </summary>
    public static class CraftScoring
    {
        /// <summary>단계 하나를 100% 수행했을 때의 점수.</summary>
        public const int StageMaxScore = 10;

        /// <summary>점수 합계 상한 (단계 4개 x 10점).</summary>
        public const int MaxScore = 40;

        /// <summary>
        /// 레시피에 적힌 재료 양의 구매가 합. 실제 투입량이 아니라 레시피에 적힌 양 기준이고,
        /// 재료 한 개의 가격은 그 광물 자신의 등급 기준 구매가(BuyPrice)다.
        /// </summary>
        public static int CalculateIngredientValue(CraftRecipeData recipe)
        {
            long total = 0;
            foreach (KeyValuePair<MineralItemData, int> required in SumRecipeIngredients(recipe))
            {
                total += (long)required.Key.BuyPrice * required.Value;
            }
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        /// <summary>같은 레시피 재료가 두 번 적혀 있어도 합산해서 돌려준다. 순서는 처음 나온 순서다.</summary>
        public static List<KeyValuePair<MineralItemData, int>> SumRecipeIngredients(CraftRecipeData recipe)
        {
            List<KeyValuePair<MineralItemData, int>> list = new List<KeyValuePair<MineralItemData, int>>();
            foreach (CraftIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient == null || ingredient.Item == null) continue;

                int index = list.FindIndex(pair => pair.Key == ingredient.Item);
                if (index >= 0) list[index] = new KeyValuePair<MineralItemData, int>(ingredient.Item, list[index].Value + ingredient.Count);
                else list.Add(new KeyValuePair<MineralItemData, int>(ingredient.Item, ingredient.Count));
            }
            return list;
        }

        /// <summary>품질 점수 합계(0~40). 단계 점수 = 완성도(0~1) x 10, 건너뛴 단계는 0점. 재료 등급 보정은 없다.</summary>
        public static decimal CalculateScore(IReadOnlyList<CraftStageResult> stages)
        {
            decimal score = 0m;
            if (stages != null)
            {
                foreach (CraftStageResult stage in stages)
                {
                    if (stage == null || stage.Skipped) continue;
                    decimal completeness = decimal.Round((decimal)stage.Completeness, 4);
                    if (completeness < 0m) completeness = 0m;
                    if (completeness > 1m) completeness = 1m;
                    score += completeness * StageMaxScore;
                }
            }

            return score > MaxScore ? MaxScore : score;
        }

        public static ItemQuality EvaluateQuality(IReadOnlyList<CraftStageResult> stages)
        {
            return ItemQualityInfo.FromScore(CalculateScore(stages));
        }

        /// <summary>매칭된 레시피로 제작 요청(난이도, 재료 합산 가격)을 만든다. 난이도는 레시피 기본값 그대로다.</summary>
        public static CraftRequest CreateRequest(CraftRecipeData recipe, IReadOnlyList<CraftInput> inputs)
        {
            int difficulty = CraftDifficulty.Calculate(recipe.BaseDifficulty);
            int value = CalculateIngredientValue(recipe);
            return new CraftRequest(recipe, difficulty, value, inputs);
        }
    }
}
