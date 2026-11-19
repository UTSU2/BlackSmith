using BlackSmith.Crafting;
using BlackSmith.Data;
using BlackSmith.Proficiency;

namespace BlackSmith.Rank
{
    /// <summary>등급 조건에 쓰는 개수를 센다(순수 로직). 도감 해금 수는 PlayerData.UnlockedRecipeCount다.</summary>
    public static class RankCounter
    {
        /// <summary>
        /// master 숙련도에 도달한 레시피 수. 레시피의 결과 아이템 등급으로 단계를 계산하고,
        /// 레시피를 찾지 못한 기록은 건너뛴다. rules가 null이면 0.
        /// </summary>
        public static int CountMasters(PlayerData player, ProficiencyRules rules)
        {
            if (player == null || rules == null) return 0;

            int count = 0;
            foreach (ProficiencyEntry entry in player.Proficiencies)
            {
                if (entry == null || string.IsNullOrEmpty(entry.RecipeId)) continue;
                if (!RecipeDatabase.TryGet(entry.RecipeId, out CraftRecipeData recipe) || recipe.Result == null) continue;
                if (rules.Evaluate(entry.Points, recipe.Result.DefaultGrade).IsMaster) count++;
            }
            return count;
        }
    }
}
