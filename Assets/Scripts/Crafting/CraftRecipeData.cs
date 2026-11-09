using System.Collections.Generic;
using UnityEngine;
using BlackSmith.Items;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 제작 레시피. 레시피 하나가 ScriptableObject 에셋 하나다.
    /// "어떤 재료 조합이 → 어떤 장비가 되고 → 어떤 단계를 거치는가"를 정의만 하며 로직은 없다.
    /// 해금 개념은 없다: 에셋 폴더(Assets/Resources/Recipes/)에 있으면 모두 개방된 레시피다.
    /// 에셋은 직접 만들지 말고 Tools > BlackSmith > Recipe Creator 창을 사용한다.
    /// 주의: Recipe Creator 창이 아래 private 필드 이름(id, result, ingredients, stages)으로 값을 기록하므로
    /// 필드 이름을 바꾸면 창도 같이 수정해야 한다.
    /// </summary>
    [CreateAssetMenu(fileName = "new_recipe", menuName = "BlackSmith/Crafting/Recipe")]
    public class CraftRecipeData : ScriptableObject
    {
        [Tooltip("고유 ID. 영소문자, 숫자, 밑줄만 사용 (예: iron_sword). 다른 레시피와 겹치면 안 된다.")]
        [SerializeField] private string id;

        [Tooltip("이 레시피로 만들어지는 장비.")]
        [SerializeField] private CraftedItemData result;

        [Tooltip("필요한 재료와 수량. 같은 재료 조합을 쓰는 레시피가 둘 이상이면 매칭이 모호해진다.")]
        [SerializeField] private List<CraftIngredient> ingredients = new List<CraftIngredient>();

        [Tooltip("거칠 제작 단계를 순서대로. 값(판정 등)은 단계별 설정에서 정한다.")]
        [SerializeField] private List<CraftStage> stages = new List<CraftStage>();

        [Tooltip("미니게임 기본 난이도(1~10). 레시피 등급보다 높은 재료를 쓰면 여기에 가중이 더해진다.")]
        [SerializeField, Range(1, 10)] private int baseDifficulty = 1;

        public string Id => id;
        public int BaseDifficulty => baseDifficulty;
        public CraftedItemData Result => result;
        public IReadOnlyList<CraftIngredient> Ingredients => ingredients;
        public IReadOnlyList<CraftStage> Stages => stages;
    }
}
