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

        [Header("미니게임 조작 안내 (튜토리얼 제작 중 왼쪽 상단. 비우면 그 단계는 안내 없음)")]
        [SerializeField, TextArea] private string heatGuide = "가열 단계 : 마우스를 누르고 있으면 막대가 올라갑니다. 목표 위치에 맞춰 유지하세요.";
        [SerializeField, TextArea] private string forgeGuide = "단조 단계 : 줄어드는 원이 안쪽 원에 겹칠 때 클릭하세요.";
        [SerializeField, TextArea] private string heatTreatGuide = "열처리 단계 : 움직이는 표시가 목표 구간에 들어가면 클릭하세요.";
        [SerializeField, TextArea] private string polishGuide = "연마 단계 : 흠집을 누른 채 문질러 지우세요.";

        public CraftRecipeData Recipe => recipe;
        public IReadOnlyList<TutorialStepData> Steps => steps;
        public string CompletionMessage => completionMessage;

        /// <summary>제작 단계의 조작 안내 문구. 없으면 빈 문자열.</summary>
        public string GetStageGuide(CraftStage stage)
        {
            switch (stage)
            {
                case CraftStage.Heat: return heatGuide ?? "";
                case CraftStage.Forge: return forgeGuide ?? "";
                case CraftStage.HeatTreat: return heatTreatGuide ?? "";
                case CraftStage.Polish: return polishGuide ?? "";
                default: return "";
            }
        }

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
