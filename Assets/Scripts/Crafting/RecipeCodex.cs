using System.Collections.Generic;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 도감에 보일 레시피 순서(순수 C#). 도감 번호(CodexNumber) 오름차순, 같으면 id 글자순이다.
    /// 해금 여부는 PlayerData가 레시피 id로 저장하므로 번호가 바뀌어도 해금은 그대로다. (docs/V2.md §3)
    /// </summary>
    public static class RecipeCodex
    {
        public static List<CraftRecipeData> GetOrdered()
        {
            return GetOrdered(RecipeDatabase.All);
        }

        public static List<CraftRecipeData> GetOrdered(IReadOnlyList<CraftRecipeData> recipes)
        {
            List<CraftRecipeData> ordered = new List<CraftRecipeData>();
            if (recipes == null) return ordered;

            foreach (CraftRecipeData recipe in recipes)
            {
                if (recipe != null) ordered.Add(recipe);
            }
            ordered.Sort(Compare);
            return ordered;
        }

        private static int Compare(CraftRecipeData a, CraftRecipeData b)
        {
            int byNumber = a.CodexNumber.CompareTo(b.CodexNumber);
            return byNumber != 0 ? byNumber : string.CompareOrdinal(a.Id, b.Id);
        }
    }
}
