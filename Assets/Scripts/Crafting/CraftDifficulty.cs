namespace BlackSmith.Crafting
{
    /// <summary>
    /// 미니게임 난이도(1~10). 기본값은 레시피에 정의하고, 상위 등급 재료마다 0.1단계씩 더한 뒤 소수점은 올린다.
    /// 4개 미니게임에 같은 값을 쓰고 플레이어에게는 표시하지 않는다.
    /// 단계별 미니게임 수치(속도, 크기 등)는 아직 미정이라 각 미니게임이 Progress로 임시 보간한다.
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

        /// <param name="baseDifficulty">레시피에 정의된 기본 난이도.</param>
        /// <param name="bonusTenths">상위 등급 재료 가중을 0.1단계 단위로 센 값 (3이면 +0.3단계).</param>
        public static int Calculate(int baseDifficulty, int bonusTenths)
        {
            // 올림은 정수 연산으로 한다. (0.1 x 10 같은 값이 부동소수점 오차로 한 단계 더 올라가는 것을 피한다)
            int raised = bonusTenths <= 0 ? 0 : (bonusTenths + 9) / 10;
            int level = baseDifficulty + raised;
            if (level < MinLevel) return MinLevel;
            return level > MaxLevel ? MaxLevel : level;
        }
    }
}
