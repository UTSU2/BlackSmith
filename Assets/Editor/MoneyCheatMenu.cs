using UnityEditor;
using UnityEngine;
using BlackSmith.Crafting;
using BlackSmith.Data;
using BlackSmith.Proficiency;
using BlackSmith.Rank;

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
            // 제작 없이 해금 수만 바뀌므로 등급 표시(도감 a/N)를 직접 다시 그리게 한다.
            RankController rank = Object.FindFirstObjectByType<RankController>();
            if (rank != null) rank.Refresh();
            Debug.Log($"[Cheat] 도감 해금 +{added} (현재 {player.UnlockedRecipeCount}/{RecipeDatabase.Count}). 도감 창을 다시 열면 반영됩니다.");
        }

        [MenuItem(Root + "Unlock All Recipes", true)]
        private static bool ValidateUnlockAllRecipes() => Application.isPlaying;

        /// <summary>
        /// 다음 승급 조건(도감 해금 수 + master 수)을 필요한 개수만큼만 맞춘다. 그러면 승급 퀘스트가 자동으로 시작된다.
        /// 도감 번호가 낮은 레시피부터 해금하고, 그 앞쪽 레시피를 master로 만든다(이미 넘었으면 건드리지 않는다).
        /// 필요 개수가 미정(-1)이면 아무것도 하지 않는다.
        /// </summary>
        [MenuItem(Root + "Meet Next Rank Conditions")]
        private static void MeetNextRankConditions()
        {
            PlayerData player = GameSession.Current;
            RankController rank = Object.FindFirstObjectByType<RankController>();
            if (rank == null) { Debug.LogWarning("[Cheat] RankController가 없습니다(Game 씬에서 실행하세요)."); return; }

            RankProgress progress = rank.GetProgress();
            if (!progress.HasNext) { Debug.Log("[Cheat] 이미 최고 등급이거나 다음 등급이 없습니다."); return; }
            if (progress.RequiredCodex < 0 || progress.RequiredMasters < 0)
            {
                Debug.LogWarning($"[Cheat] {progress.Next} 승급 필요 개수가 미정(-1)이라 조건을 맞출 수 없습니다.");
                return;
            }

            System.Collections.Generic.List<CraftRecipeData> ordered = RecipeCodex.GetOrdered();
            int unlocked = 0;
            for (int i = 0; i < ordered.Count && player.UnlockedRecipeCount < progress.RequiredCodex; i++)
            {
                if (player.UnlockRecipe(ordered[i].Id)) unlocked++;
            }

            ProficiencySettings settings = ProficiencySettings.Load();
            if (settings == null) { Debug.LogWarning("[Cheat] 숙련도 설정이 없어 master를 만들 수 없습니다."); return; }
            ProficiencyRules rules = new ProficiencyRules(settings);
            int masters = RankCounter.CountMasters(player, rules);
            for (int i = 0; i < ordered.Count && masters < progress.RequiredMasters; i++)
            {
                CraftRecipeData recipe = ordered[i];
                int max = rules.GetMaxPoints(recipe.Result.DefaultGrade);
                if (rules.Evaluate(player.GetProficiencyPoints(recipe.Id), recipe.Result.DefaultGrade).IsMaster) continue;
                player.AddProficiencyPoints(recipe.Id, max, max);
                masters++;
            }

            rank.Refresh();
            RankProgress after = rank.GetProgress();
            Debug.Log($"[Cheat] {progress.Next} 승급 조건: 도감 {after.Codex}/{after.RequiredCodex} (+{unlocked}), master {after.Masters}/{after.RequiredMasters}, 충족={after.IsMet}. 퀘스트 상태: {rank.GetQuestState()}");
        }

        [MenuItem(Root + "Meet Next Rank Conditions", true)]
        private static bool ValidateMeetNextRankConditions() => Application.isPlaying;

        /// <summary>진행 중인 승급 퀘스트 레시피의 재료를 3세트 지급한다(정확히 일치해야 제작되므로 재료 종류·개수 그대로).</summary>
        [MenuItem(Root + "Give Quest Ingredients (x3)")]
        private static void GiveQuestIngredients()
        {
            RankController rank = Object.FindFirstObjectByType<RankController>();
            if (rank == null) { Debug.LogWarning("[Cheat] RankController가 없습니다(Game 씬에서 실행하세요)."); return; }
            if (!rank.TryGetActiveQuest(out RankQuestSettings.QuestEntry quest))
            {
                Debug.Log($"[Cheat] 진행 중인 승급 퀘스트가 없습니다(상태: {rank.GetQuestState()}). 먼저 'Meet Next Rank Conditions'를 실행하세요.");
                return;
            }

            CraftRecipeData target = null;
            foreach (CraftRecipeData recipe in RecipeDatabase.All)
            {
                if (recipe != null && recipe.Id == quest.recipeId) target = recipe;
            }
            if (target == null) { Debug.LogWarning($"[Cheat] 퀘스트 레시피 '{quest.recipeId}'를 찾을 수 없습니다."); return; }

            PlayerData player = GameSession.Current;
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            foreach (CraftIngredient ingredient in target.Ingredients)
            {
                if (ingredient == null || ingredient.Item == null) continue;
                int count = ingredient.Count * 3;
                player.AddItem(ingredient.Item.Id, count, ingredient.Item.DefaultGrade);
                text.Append($"{ingredient.Item.DisplayName}×{count} ");
            }
            Debug.Log($"[Cheat] 퀘스트 '{target.Result.DisplayName}'(최소 {quest.minQuality}) 재료 3세트 지급: {text}");
        }

        [MenuItem(Root + "Give Quest Ingredients (x3)", true)]
        private static bool ValidateGiveQuestIngredients() => Application.isPlaying;

        private static void Add(int amount)
        {
            GameSession.Current.AddMoney(amount);
            Debug.Log($"[Cheat] 금액 +{amount} (현재 {GameSession.Current.Money})");
        }
    }
}
