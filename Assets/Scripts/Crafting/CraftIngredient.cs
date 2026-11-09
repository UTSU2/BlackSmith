using System;
using UnityEngine;
using BlackSmith.Items;

namespace BlackSmith.Crafting
{
    /// <summary>레시피에 필요한 재료 한 종류와 수량. 정적 데이터이며 CraftRecipeData 안에 직렬화된다.</summary>
    [Serializable]
    public class CraftIngredient
    {
        [SerializeField] private MineralItemData item;
        [SerializeField, Min(1)] private int count = 1;

        public MineralItemData Item => item;
        public int Count => count;
    }
}
