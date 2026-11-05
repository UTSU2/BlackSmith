using System.Collections.Generic;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 투입한 재료 조합으로 레시피를 찾는다. 등급은 보지 않고 재료 종류와 수량만 본다.
    /// 레시피의 재료 종류 집합과 수량이 투입한 것과 정확히 같아야 일치한다.
    /// 같은 조합의 레시피가 둘 이상이면 먼저 등록된 것이 선택된다. (Recipe Creator가 경고한다)
    /// </summary>
    public static class RecipeMatcher
    {
        /// <summary>재료 ID별 수량으로 레시피를 찾는다. 일치하는 것이 없으면 null.</summary>
        public static CraftRecipeData Match(IReadOnlyDictionary<string, int> countsByItemId)
        {
            if (countsByItemId == null) return null;

            int kinds = 0;
            foreach (KeyValuePair<string, int> pair in countsByItemId)
            {
                if (pair.Value > 0) kinds++;
            }
            if (kinds == 0) return null;

            foreach (CraftRecipeData recipe in RecipeDatabase.All)
            {
                if (IsMatch(recipe, countsByItemId, kinds)) return recipe;
            }
            return null;
        }

        private static bool IsMatch(CraftRecipeData recipe, IReadOnlyDictionary<string, int> counts, int kinds)
        {
            // 같은 재료가 레시피에 두 번 적혀 있어도 합산해서 비교한다.
            Dictionary<string, int> required = new Dictionary<string, int>();
            foreach (CraftIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient == null || ingredient.Item == null) return false;
                string id = ingredient.Item.Id;
                required.TryGetValue(id, out int current);
                required[id] = current + ingredient.Count;
            }

            if (required.Count != kinds) return false;
            foreach (KeyValuePair<string, int> pair in required)
            {
                if (!counts.TryGetValue(pair.Key, out int have) || have != pair.Value) return false;
            }
            return true;
        }
    }
}
