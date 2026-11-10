using System.Collections.Generic;
using BlackSmith.Items;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 투입한 재료로 가장 가까운 레시피를 찾는다. 레시피 북은 플레이어에게 보이지 않는다. (docs/GameDesign.md §5 "레시피 매칭")
    /// 매칭은 재료 수량만 본다. 재료나 레시피의 등급은 영향을 주지 않는다.
    /// 1. 제외: 레시피가 요구하는 어떤 재료든 투입량보다 많이 요구하면 제외한다.
    /// 2. 거리: 레시피의 재료마다 5 − |수량 차이| (최소 0)를 더한다. 가장 높은 합이 가장 가까운 레시피다.
    ///    레시피에 없는 재료를 투입한 것은 점수에 들어가지 않는다 (0점).
    /// 3. 동점이면 레시피 재료 구매가 합이 더 높은 것, 그래도 같으면 먼저 등록된 것.
    /// 모든 광물을 쓰는 레시피가 있다는 콘텐츠 규칙 덕분에 후보가 비는 일은 없어야 한다. 비면 null을 돌려준다.
    /// </summary>
    public static class RecipeMatcher
    {
        public const int MaxIngredientScore = 5;

        public static CraftRecipeData Match(IReadOnlyList<CraftInput> inputs)
        {
            return Match(inputs, RecipeDatabase.All);
        }

        public static CraftRecipeData Match(IReadOnlyList<CraftInput> inputs, IReadOnlyList<CraftRecipeData> recipes)
        {
            if (inputs == null || recipes == null) return null;

            Dictionary<string, int> counts = new Dictionary<string, int>();
            bool hasAny = false;
            foreach (CraftInput input in inputs)
            {
                if (input.Count <= 0) continue;
                hasAny = true;
                counts.TryGetValue(input.ItemId, out int current);
                counts[input.ItemId] = current + input.Count;
            }
            if (!hasAny) return null;

            CraftRecipeData best = null;
            int bestScore = -1;
            int bestPrice = -1;
            foreach (CraftRecipeData recipe in recipes)
            {
                if (recipe == null || recipe.Result == null) continue;

                if (!TryScore(recipe, counts, out int score)) continue;

                int price = GetRecipePrice(recipe);
                if (score > bestScore || (score == bestScore && price > bestPrice))
                {
                    best = recipe;
                    bestScore = score;
                    bestPrice = price;
                }
            }
            return best;
        }

        /// <summary>거리 점수. 요구량이 투입량보다 많은 재료가 하나라도 있거나 재료가 없는 레시피면 false.</summary>
        private static bool TryScore(CraftRecipeData recipe, Dictionary<string, int> counts, out int score)
        {
            score = 0;
            List<KeyValuePair<MineralItemData, int>> required = CraftScoring.SumRecipeIngredients(recipe);
            if (required.Count == 0) return false;

            foreach (KeyValuePair<MineralItemData, int> pair in required)
            {
                counts.TryGetValue(pair.Key.Id, out int have);
                if (have < pair.Value) return false;

                int difference = have - pair.Value;
                score += difference >= MaxIngredientScore ? 0 : MaxIngredientScore - difference;
            }
            return true;
        }

        /// <summary>동점 처리용 레시피 가격: 레시피에 적힌 재료를 각 광물 자신의 구매가로 합산한 값.</summary>
        private static int GetRecipePrice(CraftRecipeData recipe)
        {
            return CraftScoring.CalculateIngredientValue(recipe);
        }
    }
}
