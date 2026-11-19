using System.Collections.Generic;
using BlackSmith.Items;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 투입한 재료와 정확히 일치하는 레시피를 찾는다. (docs/V2.md §3 "정확 일치 매칭")
    /// 일치 기준은 재료 종류 + 수량이다. 같은 재료의 다른 등급 아이템은 없으므로 등급은 보지 않는다.
    /// 레시피에 없는 재료가 섞이거나 수량이 하나라도 다르면 일치하지 않는다.
    /// 일치하는 레시피가 없으면 null이며, 이때는 알 수 없는 제작품이 만들어진다(CraftingSession).
    /// 재료 구성이 같은 레시피가 둘 이상이면 먼저 등록된 것을 쓴다(RecipeDatabase가 불러올 때 경고한다).
    /// </summary>
    public static class RecipeMatcher
    {
        public static CraftRecipeData Match(IReadOnlyList<CraftInput> inputs)
        {
            return Match(inputs, RecipeDatabase.All);
        }

        public static CraftRecipeData Match(IReadOnlyList<CraftInput> inputs, IReadOnlyList<CraftRecipeData> recipes)
        {
            if (inputs == null || recipes == null) return null;

            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (CraftInput input in inputs)
            {
                if (input.Count <= 0 || string.IsNullOrEmpty(input.ItemId)) continue;
                counts.TryGetValue(input.ItemId, out int current);
                counts[input.ItemId] = current + input.Count;
            }
            if (counts.Count == 0) return null;

            foreach (CraftRecipeData recipe in recipes)
            {
                if (recipe == null || recipe.Result == null) continue;
                if (IsExactMatch(recipe, counts)) return recipe;
            }
            return null;
        }

        /// <summary>레시피 재료(합산)가 투입량과 종류·수량 모두 같은가.</summary>
        public static bool IsExactMatch(CraftRecipeData recipe, Dictionary<string, int> counts)
        {
            List<KeyValuePair<MineralItemData, int>> required = CraftScoring.SumRecipeIngredients(recipe);
            if (required.Count == 0 || required.Count != counts.Count) return false;

            foreach (KeyValuePair<MineralItemData, int> pair in required)
            {
                if (!counts.TryGetValue(pair.Key.Id, out int have) || have != pair.Value) return false;
            }
            return true;
        }

        /// <summary>두 레시피의 재료 구성(합산)이 같은가. 같으면 매칭이 모호해진다.</summary>
        public static bool HasSameIngredients(CraftRecipeData a, CraftRecipeData b)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (KeyValuePair<MineralItemData, int> pair in CraftScoring.SumRecipeIngredients(a))
            {
                counts[pair.Key.Id] = pair.Value;
            }
            return counts.Count > 0 && IsExactMatch(b, counts);
        }
    }
}
