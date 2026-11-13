using System.Collections.Generic;
using UnityEngine;
using BlackSmith.Crafting;
using BlackSmith.Items;

namespace BlackSmith.Tutorial
{
    /// <summary>튜토리얼 전체 설정(레시피, 단계 목록, 완료 문구).</summary>
    [CreateAssetMenu(fileName = "TutorialConfig", menuName = "BlackSmith/Tutorial Config")]
    public class TutorialConfig : ScriptableObject
    {
        public const string ResourcePath = "Tutorial/TutorialConfig";

        [SerializeField] private CraftRecipeData recipe;
        [SerializeField] private List<TutorialStepData> steps = new List<TutorialStepData>();
        [SerializeField, TextArea] private string completionMessage;

        public CraftRecipeData Recipe => recipe;
        public IReadOnlyList<TutorialStepData> Steps => steps;
        public string CompletionMessage => completionMessage;

        /// <summary>설정 에셋을 불러온다. 없으면 null.</summary>
        public static TutorialConfig Load()
        {
            return Resources.Load<TutorialConfig>(ResourcePath);
        }

        /// <summary>시작 지급 금액: 레시피 재료의 구매가 x 수량 합.</summary>
        public int CalculateStartMoney()
        {
            if (recipe == null) return 0;
            int total = 0;
            IReadOnlyList<CraftIngredient> ingredients = recipe.Ingredients;
            for (int i = 0; i < ingredients.Count; i++)
            {
                CraftIngredient ing = ingredients[i];
                if (ing == null || ing.Item == null) continue;
                total += ing.Item.BuyPrice * ing.Count;
            }
            return total;
        }

        public bool IsBuyAllowed(ItemData item)
        {
            if (item == null || recipe == null) return false;
            IReadOnlyList<CraftIngredient> ingredients = recipe.Ingredients;
            for (int i = 0; i < ingredients.Count; i++)
            {
                CraftIngredient ing = ingredients[i];
                if (ing != null && ing.Item != null && ing.Item.Id == item.Id) return true;
            }
            return false;
        }

        public bool IsSellAllowed(ItemData item)
        {
            if (item == null || recipe == null || recipe.Result == null) return false;
            return recipe.Result.Id == item.Id;
        }
    }
}
