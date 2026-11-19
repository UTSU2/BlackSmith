using System.Collections.Generic;
using UnityEngine;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 레시피 데이터베이스. ItemDatabase와 같은 방식이다.
    /// Assets/Resources/Recipes/ 아래(하위 폴더 포함)의 모든 CraftRecipeData 에셋을 게임 시작 시 자동으로 읽어 등록한다.
    /// 별도 오브젝트나 목록 관리는 필요 없다.
    /// 재료 조합으로 레시피를 찾는 매칭은 RecipeMatcher가 맡는다.
    /// </summary>
    public static class RecipeDatabase
    {
        private const string ResourcesPath = "Recipes";

        private static readonly Dictionary<string, CraftRecipeData> byId = new Dictionary<string, CraftRecipeData>();
        private static readonly List<CraftRecipeData> all = new List<CraftRecipeData>();
        private static bool loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Reload();
        }

        /// <summary>등록된 모든 레시피.</summary>
        public static IReadOnlyList<CraftRecipeData> All
        {
            get { EnsureLoaded(); return all; }
        }

        public static int Count
        {
            get { EnsureLoaded(); return all.Count; }
        }

        /// <summary>Resources에서 레시피를 다시 읽는다. (게임 시작 때 자동 호출됨)</summary>
        public static void Reload()
        {
            byId.Clear();
            all.Clear();

            CraftRecipeData[] recipes = Resources.LoadAll<CraftRecipeData>(ResourcesPath);
            foreach (CraftRecipeData recipe in recipes)
            {
                if (string.IsNullOrWhiteSpace(recipe.Id))
                {
                    Debug.LogWarning($"[RecipeDatabase] ID가 비어 있어 등록하지 않았습니다: {recipe.name}", recipe);
                    continue;
                }

                if (recipe.Result == null)
                {
                    Debug.LogWarning($"[RecipeDatabase] 결과물이 지정되지 않아 등록하지 않았습니다: '{recipe.Id}' ({recipe.name})", recipe);
                    continue;
                }

                if (byId.TryGetValue(recipe.Id, out CraftRecipeData existing))
                {
                    Debug.LogError($"[RecipeDatabase] 중복 ID '{recipe.Id}': '{existing.name}'이(가) 이미 등록되어 있어 '{recipe.name}'은(는) 무시했습니다.", recipe);
                    continue;
                }

                byId.Add(recipe.Id, recipe);
                all.Add(recipe);
            }

            loaded = true;
            WarnSameIngredients();
        }

        // 정확 일치 매칭에서 재료 구성이 같은 레시피는 먼저 등록된 것만 쓰이므로 불러올 때 한 번 알린다.
        private static void WarnSameIngredients()
        {
            for (int i = 0; i < all.Count; i++)
            {
                for (int j = i + 1; j < all.Count; j++)
                {
                    if (!RecipeMatcher.HasSameIngredients(all[i], all[j])) continue;
                    Debug.LogWarning($"[RecipeDatabase] 재료 구성이 같은 레시피: '{all[i].Id}', '{all[j].Id}'. 먼저 등록된 '{all[i].Id}'가 쓰입니다.", all[j]);
                }
            }
        }

        /// <summary>ID로 레시피를 찾는다. 없으면 경고를 남기고 null.</summary>
        public static CraftRecipeData Get(string id)
        {
            if (TryGet(id, out CraftRecipeData recipe)) return recipe;
            Debug.LogWarning($"[RecipeDatabase] 존재하지 않는 레시피 ID: '{id}'");
            return null;
        }

        public static bool TryGet(string id, out CraftRecipeData recipe)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id))
            {
                recipe = null;
                return false;
            }
            return byId.TryGetValue(id, out recipe);
        }

        private static void EnsureLoaded()
        {
            if (!loaded) Reload();
        }
    }
}
