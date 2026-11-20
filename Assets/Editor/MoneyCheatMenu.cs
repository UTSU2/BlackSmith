using UnityEditor;
using UnityEngine;
using BlackSmith.Crafting;
using BlackSmith.Data;

namespace BlackSmith.EditorTools
{
    /// <summary>
    /// 에디터 전용 테스트 치트. 플레이 모드에서 보유 금액을 지급하거나 도감 전체를 해금한다.
    /// 금액은 테스트용 값이며 게임 밸런스와 무관하다. Editor 폴더라 빌드에 포함되지 않는다.
    /// </summary>
    public static class MoneyCheatMenu
    {
        private const string Root = "Tools/BlackSmith/Cheat/";

        [MenuItem(Root + "Add Money +1000")]
        private static void Add1000() => Add(1000);

        [MenuItem(Root + "Add Money +10000")]
        private static void Add10000() => Add(10000);

        [MenuItem(Root + "Add Money +1000", true)]
        private static bool Validate1000() => Application.isPlaying;

        [MenuItem(Root + "Add Money +10000", true)]
        private static bool Validate10000() => Application.isPlaying;

        [MenuItem(Root + "Unlock All Recipes")]
        private static void UnlockAllRecipes()
        {
            PlayerData player = GameSession.Current;
            int added = 0;
            foreach (CraftRecipeData recipe in RecipeDatabase.All)
            {
                if (recipe != null && player.UnlockRecipe(recipe.Id)) added++;
            }
            Debug.Log($"[Cheat] 도감 해금 +{added} (현재 {player.UnlockedRecipeCount}/{RecipeDatabase.Count}). 도감 창을 다시 열면 반영됩니다.");
        }

        [MenuItem(Root + "Unlock All Recipes", true)]
        private static bool ValidateUnlockAllRecipes() => Application.isPlaying;

        private static void Add(int amount)
        {
            GameSession.Current.AddMoney(amount);
            Debug.Log($"[Cheat] 금액 +{amount} (현재 {GameSession.Current.Money})");
        }
    }
}
