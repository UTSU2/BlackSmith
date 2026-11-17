using System;
using UnityEngine;

namespace BlackSmith.Data
{
    /// <summary>레시피 하나의 누적 숙련도(런타임 상태, 저장됨). 레벨은 ProficiencyRules가 계산한다.</summary>
    [Serializable]
    public class ProficiencyEntry
    {
        [SerializeField] private string recipeId;
        [SerializeField] private int points;

        public string RecipeId => recipeId;
        public int Points => points;

        public ProficiencyEntry(string recipeId, int points)
        {
            this.recipeId = recipeId;
            this.points = points;
        }

        public void SetPoints(int value)
        {
            points = value < 0 ? 0 : value;
        }
    }
}
