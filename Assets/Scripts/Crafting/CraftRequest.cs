using System.Collections.Generic;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 제작 시작 시점에 확정되는 값 묶음. 재료는 시작할 때 모두 소모되므로
    /// 완성 시점에 쓸 값(재료 합산 가격, 어드밴티지)을 여기에 미리 담아 둔다. 런타임 데이터다.
    /// </summary>
    public class CraftRequest
    {
        public CraftRecipeData Recipe { get; }

        /// <summary>미니게임 난이도(1~10). 레시피 기본값 + 상위 재료 가중(올림).</summary>
        public int Difficulty { get; }

        /// <summary>품질 점수 어드밴티지를 0.1점 단위로 센 값. 3이면 +0.3점.</summary>
        public int AdvantageTenths { get; }

        /// <summary>레시피에 적힌 재료 양의 구매가 합. 완성품 판매가의 기준이 된다.</summary>
        public int IngredientValue { get; }

        public IReadOnlyList<CraftInput> Inputs { get; }

        public CraftRequest(CraftRecipeData recipe, int difficulty, int advantageTenths, int ingredientValue,
            IReadOnlyList<CraftInput> inputs)
        {
            Recipe = recipe;
            Difficulty = difficulty;
            AdvantageTenths = advantageTenths;
            IngredientValue = ingredientValue;
            Inputs = inputs;
        }
    }
}
