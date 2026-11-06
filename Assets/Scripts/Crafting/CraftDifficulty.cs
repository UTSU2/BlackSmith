using System.Collections.Generic;
using BlackSmith.Items;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 미니게임 난이도(1~10). 재료 등급 점수의 평균(사용한 유닛의 점수 합 ÷ 유닛 수, 수량 무관)이
    /// 임계값을 넘을 때마다 한 단계씩 오른다.
    /// 등급별 점수와 임계값이 아직 미정이라 지금은 항상 최소 난이도를 돌려주는 자리표시자다.
    /// </summary>
    public static class CraftDifficulty
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 10;

        /// <summary>난이도 1이면 0, 10이면 1인 보간 계수. 미니게임이 난이도별 값을 Lerp할 때 쓴다.</summary>
        public static float Progress(int difficulty)
        {
            return UnityEngine.Mathf.Clamp01((difficulty - MinLevel) / (float)(MaxLevel - MinLevel));
        }

        /// <param name="unitGrades">사용한 재료를 유닛 하나당 등급 하나로 펼친 목록.</param>
        public static int Calculate(IReadOnlyList<ItemGrade> unitGrades)
        {
            // TODO: 등급별 점수와 임계값이 정해지면 평균 점수 → 난이도 계산을 구현한다. (미정)
            return MinLevel;
        }
    }
}
