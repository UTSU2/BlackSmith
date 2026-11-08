using System.Collections.Generic;
using BlackSmith.Items;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 제작 규칙 계산(순수 C#): 난이도 가중, 품질 점수, 재료 합산 가격. 규칙은 docs/GameDesign.md §4~5.
    /// 0.1 단위 값은 부동소수점 오차를 피하려고 "10분의 1 단위 정수(tenths)"와 decimal로 다룬다.
    /// </summary>
    public static class CraftScoring
    {
        /// <summary>단계 하나를 100% 수행했을 때의 점수.</summary>
        public const int StageMaxScore = 10;

        /// <summary>점수 합계 상한 (단계 4개 x 10점).</summary>
        public const int MaxScore = 40;

        /// <summary>
        /// 매칭된 레시피 등급보다 높은 재료의 가중을 0.1 단위로 센다.
        /// 재료 (아이템, 등급) 한 줄마다 (재료 등급 − 레시피 등급)단계 x 1. 개수는 따지지 않는다.
        /// 난이도에는 이 값 x 0.1단계, 품질 점수에는 이 값 x 0.1점이 더해진다.
        /// </summary>
        public static int GetGradeBonusTenths(ItemGrade recipeGrade, IReadOnlyList<CraftInput> inputs)
        {
            int tenths = 0;
            if (inputs == null) return tenths;

            foreach (CraftInput input in inputs)
            {
                if (input.Count <= 0) continue;
                int steps = (int)input.Grade - (int)recipeGrade;
                if (steps > 0) tenths += steps;
            }
            return tenths;
        }

        /// <summary>
        /// 레시피에 적힌 재료 양의 구매가 합 (반올림).
        /// 실제 투입량이 아니라 레시피에 적힌 양 기준이다. 재료 한 개의 가격은 투입한 그 재료들의 등급별 구매가 평균이고,
        /// 그 재료를 투입하지 않았다면 레시피 등급 기준 구매가를 쓴다.
        /// </summary>
        public static int CalculateIngredientValue(CraftRecipeData recipe, IReadOnlyList<CraftInput> inputs)
        {
            ItemGrade recipeGrade = recipe.Result.DefaultGrade;
            decimal total = 0m;

            foreach (KeyValuePair<MineralItemData, int> required in SumRecipeIngredients(recipe))
            {
                decimal unitPrice = AverageInputPrice(required.Key, inputs, out bool found);
                if (!found) unitPrice = required.Key.GetBuyPrice(recipeGrade);
                total += unitPrice * required.Value;
            }
            return PriceRounding.Round(total);
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

        private static decimal AverageInputPrice(MineralItemData item, IReadOnlyList<CraftInput> inputs, out bool found)
        {
            decimal sum = 0m;
            int units = 0;
            foreach (CraftInput input in inputs)
            {
                if (input.ItemId != item.Id || input.Count <= 0) continue;
                sum += (decimal)item.GetBuyPrice(input.Grade) * input.Count;
                units += input.Count;
            }
            found = units > 0;
            return found ? sum / units : 0m;
        }

        /// <summary>
        /// 품질 점수 합계(0~40). 단계 점수 = 완성도(0~1) x 10, 건너뛴 단계는 0점.
        /// 마지막에 어드밴티지(0.1점 단위)를 더하고 40점을 넘지 않게 자른다.
        /// </summary>
        public static decimal CalculateScore(IReadOnlyList<CraftStageResult> stages, int advantageTenths)
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

            score += advantageTenths / 10m;
            return score > MaxScore ? MaxScore : score;
        }

        public static ItemQuality EvaluateQuality(IReadOnlyList<CraftStageResult> stages, int advantageTenths)
        {
            return ItemQualityInfo.FromScore(CalculateScore(stages, advantageTenths));
        }

        /// <summary>투입한 재료로 제작 요청(난이도, 어드밴티지, 재료 합산 가격)을 만든다.</summary>
        public static CraftRequest CreateRequest(CraftRecipeData recipe, IReadOnlyList<CraftInput> inputs)
        {
            int bonusTenths = GetGradeBonusTenths(recipe.Result.DefaultGrade, inputs);
            int difficulty = CraftDifficulty.Calculate(recipe.BaseDifficulty, bonusTenths);
            int value = CalculateIngredientValue(recipe, inputs);
            return new CraftRequest(recipe, difficulty, bonusTenths, value, inputs);
        }
    }
}
