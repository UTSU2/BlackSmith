using System.Collections.Generic;
using BlackSmith.Items;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 제작 시작 시점에 확정되는 값 묶음. 재료는 시작할 때 모두 소모되므로
    /// 완성 시점에 쓸 값(결과 아이템, 단계, 난이도, 재료 합산 가격)을 여기에 미리 담아 둔다. 런타임 데이터다.
    /// 레시피와 일치하지 않는 조합이면 Recipe가 null이고 결과는 알 수 없는 제작품이다(UnknownCraftSettings).
    /// </summary>
    public class CraftRequest
    {
        /// <summary>일치한 레시피. 알 수 없는 제작이면 null.</summary>
        public CraftRecipeData Recipe { get; }

        /// <summary>만들어질 제작품.</summary>
        public CraftedItemData Product { get; }

        /// <summary>거칠 단계 순서.</summary>
        public IReadOnlyList<CraftStage> Stages { get; }

        /// <summary>미니게임 난이도(1~10).</summary>
        public int Difficulty { get; }

        /// <summary>레시피에 적힌 재료 양의 구매가 합. 완성품 판매가의 기준이 된다. 알 수 없는 제작이면 0.</summary>
        public int IngredientValue { get; }

        public IReadOnlyList<CraftInput> Inputs { get; }

        /// <summary>레시피와 일치하지 않는 조합으로 만든 제작인가. 숙련도·도감 해금이 없다.</summary>
        public bool IsUnknown => Recipe == null;

        public CraftRequest(CraftRecipeData recipe, CraftedItemData product, IReadOnlyList<CraftStage> stages,
            int difficulty, int ingredientValue, IReadOnlyList<CraftInput> inputs)
        {
            Recipe = recipe;
            Product = product;
            Stages = stages ?? new List<CraftStage>();
            Difficulty = difficulty;
            IngredientValue = ingredientValue;
            Inputs = inputs;
        }
    }
}
